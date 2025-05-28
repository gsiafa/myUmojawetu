using Microsoft.EntityFrameworkCore;
using WebOptimus.Data;
using WebOptimus.Models;
using WebOptimus.Services;


public class DonationClosureService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DonationClosureService> _logger;
    private readonly IPostmarkClient _postmark;

    public DonationClosureService(IServiceProvider serviceProvider, IServiceScopeFactory scopeFactory, ILogger<DonationClosureService> logger, IPostmarkClient postmark)
    {
        _serviceProvider = serviceProvider;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _postmark = postmark;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeZoneInfo ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            //  Fetch scheduled execution time from DB
            var scheduledTask = await dbContext.ScheduledTask
                .Where(t => t.IsActive)
                .FirstOrDefaultAsync(stoppingToken);

            if (scheduledTask == null)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait before retrying
                continue;
            }

            var nowUtc = DateTime.UtcNow;
            var nowUk = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, ukTimeZone);

            //  Get target execution time based on configuration
            DateTime targetTimeUk = nowUk.Date.Add(scheduledTask.ExecutionTime);

            if (nowUk >= targetTimeUk)
            {
                // If scheduled time has passed today, schedule for tomorrow
                targetTimeUk = targetTimeUk.AddDays(1);
            }

            var delay = targetTimeUk - nowUk;

            using (var auditScope = _scopeFactory.CreateScope())
            {
                var auditService = auditScope.ServiceProvider.GetRequiredService<IAuditService>();
                await auditService.RecordAuditAsync("Donation Closure Scheduled", "Donation Processing",
                    $"Next donation check scheduled in {delay.TotalMinutes:F0} minutes (at {targetTimeUk:HH:mm}).", stoppingToken);
            }

            //  Wait until the scheduled execution time
            await Task.Delay(delay, stoppingToken);

            try
            {
                await CloseExpiredDonations(stoppingToken);
            }
            catch (Exception ex)
            {
                using (var exceptionScope = _scopeFactory.CreateScope())
                {
                    var auditService = exceptionScope.ServiceProvider.GetRequiredService<IAuditService>();
                    await auditService.RecordAuditAsync("Donation Closure Failed", "Donation Processing",
                        $"Donation closure process failed: {ex.Message}", stoppingToken);
                }
            }
        }
    }


    private async Task CloseExpiredDonations(CancellationToken stoppingToken)
    {
        TimeZoneInfo ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        var nowUtc = DateTime.UtcNow;
        var nowUk = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, ukTimeZone);
        var todayUk = nowUk.Date;

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        // Fetch scheduled execution time from DB
        var scheduledTask = await dbContext.ScheduledTask
            .Where(t => t.IsActive)
            .FirstOrDefaultAsync(stoppingToken);

        if (scheduledTask == null)
        {
            await auditService.RecordAuditAsync("Donation Closure Skipped", "Donation Processing",
                "No active scheduled execution time found in DB.", stoppingToken);
            return;
        }

        //  Get target execution time based on configuration
        DateTime targetTimeUk = todayUk.Add(scheduledTask.ExecutionTime);

        if (nowUk >= targetTimeUk)
        {
            // If execution time has passed today, schedule for tomorrow
            targetTimeUk = targetTimeUk.AddDays(1);
        }

        var delay = targetTimeUk - nowUk;
        await auditService.RecordAuditAsync("Donation Closure Scheduled", "Donation Processing",
            $"Next donation closure check scheduled in {delay.TotalMinutes:F0} minutes (at {targetTimeUk:HH:mm}).", stoppingToken);

        //  Wait until the scheduled execution time
        await Task.Delay(delay, stoppingToken);

        var expiredDonations = await dbContext.DonationForNonDeathRelated
            .Where(d => d.IsActive && d.ClosedDate.HasValue &&
                        TimeZoneInfo.ConvertTimeFromUtc(d.ClosedDate.Value, ukTimeZone).Date <= todayUk)
            .ToListAsync(stoppingToken);

        if (!expiredDonations.Any())
        {
            await auditService.RecordAuditAsync("No Donations Closed", "Donation Processing",
                "No active donations were found to close.", stoppingToken);
            return;
        }

        foreach (var donation in expiredDonations)
        {
            donation.IsActive = false;
            donation.Status = "Closed";
            await auditService.RecordAuditAsync("Donation Closed", "Donation Processing",
                $"Closed donation ID {donation.Id} - {donation.Summary}", stoppingToken);
        }

        await dbContext.SaveChangesAsync(stoppingToken);
        await SendClosureEmail(expiredDonations, stoppingToken);
    }

    private async Task SendClosureEmail(List<DonationForNonDeathRelated> donations, CancellationToken ct)
    {
        var adminEmails = new List<string> { "info@umojawetu.com", "seakou2@yahoo.com" };

        const string templatePath = @"EmailTemplate/DonationClosureNotification.html";
        string htmlBody;
        using (StreamReader reader = File.OpenText(templatePath))
        {
            htmlBody = await reader.ReadToEndAsync();
        }

        string donationListHtml = "";
        foreach (var donation in donations)
        {
            donationListHtml += $"<li>{donation.Summary} (Closed Date: {donation.ClosedDate?.ToString("dd/MM/yyyy")})</li>";
        }

        htmlBody = htmlBody.Replace("{{donationList}}", donationListHtml)
                           .Replace("{{date}}", DateTime.UtcNow.ToString("dd/MM/yyyy"));

        foreach (var email in adminEmails)
        {
            var message = new PostmarkMessage
            {
                To = email,
                Subject = "Donation Campaigns Closed",
                HtmlBody = htmlBody,
                MessageStream = "broadcast",
                From = "info@umojawetu.com"
            };

            await _postmark.SendMessageAsync(message, ct);
        }
    }
}
