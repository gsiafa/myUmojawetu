using Azure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;
using WebOptimus.Data;
using WebOptimus.Models;
using WebOptimus.Models.ViewModel;
using WebOptimus.Services;
using WebOptimus.Helpers;
using System.Text.Json.Serialization;
using DocumentFormat.OpenXml.Bibliography;

public class CauseManagementService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CauseManagementService> _logger;
    private readonly IPostmarkClient _postmark;
    private readonly IConfiguration _configuration;


    public CauseManagementService(IServiceProvider serviceProvider, IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<CauseManagementService> logger, IPostmarkClient postmark)
    {
        _serviceProvider = serviceProvider;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _postmark = postmark;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeZoneInfo ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Fetch scheduled execution time from DB
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

            // Determine today's target UK time (12:00 AM or from config)
            var targetTimeLocalUnspecified = DateTime.SpecifyKind(nowUk.Date, DateTimeKind.Unspecified)
                                             .Add(scheduledTask.ExecutionTime);

            var targetTimeUtc = TimeZoneInfo.ConvertTimeToUtc(targetTimeLocalUnspecified, ukTimeZone);

            // If today's time has passed, move to tomorrow's
            if (nowUtc >= targetTimeUtc)
            {
                var tomorrowUnspecified = DateTime.SpecifyKind(nowUk.Date.AddDays(1), DateTimeKind.Unspecified)
                                          .Add(scheduledTask.ExecutionTime);
                targetTimeUtc = TimeZoneInfo.ConvertTimeToUtc(tomorrowUnspecified, ukTimeZone);
            }

            var delay = targetTimeUtc - nowUtc;
            var targetTimeUk = TimeZoneInfo.ConvertTimeFromUtc(targetTimeUtc, ukTimeZone); // For audit log

            using (var auditScope = _scopeFactory.CreateScope())
            {
                var auditService = auditScope.ServiceProvider.GetRequiredService<IAuditService>();
                await auditService.RecordAuditAsync("Cause Management Scheduled", "Cause Processing",
                    $"Next execution scheduled in {delay.TotalMinutes:F0} minutes (at {targetTimeUk:HH:mm}).", stoppingToken);
            }

            await Task.Delay(delay, stoppingToken);

            try
            {
                await ManageCauses(stoppingToken);
            }
            catch (Exception ex)
            {
                using (var exceptionScope = _scopeFactory.CreateScope())
                {
                    var auditService = exceptionScope.ServiceProvider.GetRequiredService<IAuditService>();
                    await auditService.RecordAuditAsync("Cause Management Failed", "Cause Processing",
                        $"Cause management process failed: {ex.Message}", stoppingToken);
                }
            }
        }

    }

    private async Task ManageCauses(CancellationToken stoppingToken)
    { 
        TimeZoneInfo ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        var todayUk = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ukTimeZone).Date;

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        // Fetch all causes
        var causesFromDb = await dbContext.Cause
    .ToListAsync(stoppingToken);


        if (!causesFromDb.Any())
        {
            await auditService.RecordAuditAsync("No Causes Found", "Cause Processing",
                "No active causes found for processing.", stoppingToken);
            return;
        }

        foreach (var cause in causesFromDb)
        {
            var causeEndUk = cause.EndDate?.Date ?? DateTime.MinValue;

            //  Process only active causes
            if (cause.IsActive == true && cause.StartDate <= todayUk && (!cause.EndDate.HasValue || todayUk <= cause.EndDate))
            {
                // Handle active causes (reminders, processing, etc.)
                var daysSinceCreation = (todayUk - cause.DateCreated.Date).Days;

                if (daysSinceCreation == 3 || daysSinceCreation == 5)
                {
                    await SendReminderEmail(cause, daysSinceCreation, stoppingToken);
                }

                if (daysSinceCreation == 7)
                {
                    await ProcessFinalDayActions(cause, stoppingToken);
                }
            }
            if (cause.IsActive != true && todayUk >= causeEndUk.AddDays(7))
            {
                var deactivated = await DeactivateUnpaidUsers(cause.CauseCampaignpRef, stoppingToken);
                if (deactivated.Any())
                {
                    await SendDeactivateUnpaidUsers(cause.CauseCampaignpRef, deactivated, stoppingToken);

                    await auditService.RecordAuditAsync("User Deactivation", "Cause Processing",
                        $"Deactivated unpaid users for cause {cause.CauseCampaignpRef} 7 days after end date.", stoppingToken);
                }

            }
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }


    private async Task SendReminderEmail(Cause cause, int day, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var missedPayments = await GetMissedPaymentsForCause(cause.CauseCampaignpRef, ct);
        if (!missedPayments.Any()) return;

        string emailSubject = (day == 7)
            ? "⚠️ URGENT: Final Payment Reminder – Membership at Risk"
            : $"Reminder: Ongoing Cause - {cause.CauseCampaignpRef}";
        string emailTemplate = (day == 7)
            ? @"EmailTemplate/FinalReminder.html"
            : @"EmailTemplate/CauseReminder.html";
        //string emailTemplatePath = @"EmailTemplate/test.html";
        string emailBody;

        using (StreamReader reader = File.OpenText(emailTemplate))
        {
            emailBody = await reader.ReadToEndAsync();
        }

        emailBody = emailBody.Replace("{{causeName}}", cause.CauseCampaignpRef)
                             .Replace("{{causeTarget}}", cause.TargetAmount.ToString("C"))
                             .Replace("{{daysLeft}}", (7 - day).ToString())
                             .Replace("{{ref}}", cause.CauseCampaignpRef +"day: "+ day);

        //  Prepare batch emails
        var emailMessages = missedPayments.Select(missedPayment => new PostmarkMessage
        {
            To = missedPayment.Email,
            Subject = emailSubject,
            HtmlBody = emailBody,
            MessageStream = "broadcast",
            From = "info@umojawetu.com"
        }).ToList();

        //  Split into batches of 500
        const int BATCH_SIZE = 500;
        var emailBatches = emailMessages
            .Select((email, index) => new { email, index })
            .GroupBy(x => x.index / BATCH_SIZE)
            .Select(group => group.Select(x => x.email).ToList())
            .ToList();

        List<string> failedEmails = new();
        List<string> successEmails = new();

        using var httpClient = new HttpClient { BaseAddress = new Uri("https://api.postmarkapp.com") };
        httpClient.DefaultRequestHeaders.Add("X-Postmark-Server-Token", _configuration["Postmark:ApiKey"]);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        foreach (var batch in emailBatches)
        {
            var jsonPayload = JsonSerializer.Serialize(batch, jsonOptions);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("/email/batch", content, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                await auditService.RecordAuditAsync("Email API Failure", "Cause Processing",
                    $"Batch sending failed. Response: {responseContent}", ct);
                continue;
            }

            var postmarkResponse = JsonSerializer.Deserialize<List<PostmarkResponseMessage>>(responseContent);
            if (postmarkResponse == null)
            {
                await auditService.RecordAuditAsync("Email API Failure", "Cause Processing",
                    $"Unexpected response format from Postmark: {responseContent}", ct);
                continue;
            }

            failedEmails.AddRange(postmarkResponse.Where(m => m.ErrorCode != 0).Select(m => m.To));
            successEmails.AddRange(postmarkResponse.Where(m => m.ErrorCode == 0).Select(m => m.To));
        }

        //  Retry sending failed emails one-by-one
        foreach (var failedEmail in failedEmails.ToList())
        {
            var retryMessage = new PostmarkMessage
            {
                From = "info@umojawetu.com",
                To = failedEmail,
                Subject = emailSubject,
                HtmlBody = emailBody,
                MessageStream = "broadcast"
            };

            var retryPayload = JsonSerializer.Serialize(new List<PostmarkMessage> { retryMessage }, jsonOptions);
            var retryContent = new StringContent(retryPayload, Encoding.UTF8, "application/json");
            var retryResponse = await httpClient.PostAsync("/email/batch", retryContent, ct);

            if (retryResponse.IsSuccessStatusCode)
            {
                successEmails.Add(failedEmail);
                failedEmails.Remove(failedEmail); //  Remove from failed list
            }
            else
            {
                var retryResponseContent = await retryResponse.Content.ReadAsStringAsync(ct);
                await auditService.RecordAuditAsync("Email Retry Failure", "Cause Processing",
                    $"Retry failed for {failedEmail}. Response: {retryResponseContent}", ct);
            }
        }

        //  Log successful emails
        if (successEmails.Any())
        {
            await auditService.RecordAuditAsync("Email Success", "Cause Processing",
                $"Successfully sent reminder emails to: {string.Join(", ", successEmails)}", ct);
        }

        //  Log failed emails
        if (failedEmails.Any())
        {
            await auditService.RecordAuditAsync("Email Failure", "Cause Processing",
                $"Failed to send reminder emails to: {string.Join(", ", failedEmails)}", ct);
        }

        //  Final summary log
        await auditService.RecordAuditAsync("Cause Reminder Sent", "Cause Processing",
            $"Sent {emailMessages.Count} reminder emails for cause {cause.CauseCampaignpRef} on day {day}. " +
            $"Successful: {successEmails.Count}, Failed: {failedEmails.Count}", ct);
    }

    private async Task ProcessFinalDayActions(Cause cause, CancellationToken ct)
    {
        TimeZoneInfo ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"); // UK Time Zone
        var nowUtc = DateTime.UtcNow;
        var nowUk = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, ukTimeZone); // Convert to UK Time

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var totalAmountRaised = await dbContext.PaymentSessions
            .Where(ps => ps.CauseCampaignpRef == cause.CauseCampaignpRef && ps.IsPaid == true)
            .SumAsync(ps => ps.TotalAmount - ps.TransactionFees, ct);

        if (totalAmountRaised < cause.TargetAmount)
        {
            var causeEndUk = cause.EndDate.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(cause.EndDate.Value, ukTimeZone)
                : nowUk; // Convert EndDate to UK Time if it exists

            cause.EndDate = causeEndUk.AddDays(5); // Extend by 5 days in UK Time

            await SendExtensionEmail(cause, ct);
            await auditService.RecordAuditAsync("Cause Extended", "Cause Processing",
                $"Extended cause {cause.CauseCampaignpRef} by 5 days due to unmet target.", ct);
        }
        else
        {
            cause.IsActive = false;
            await SendClosureEmail(cause, ct);
            await auditService.RecordAuditAsync("Cause Closed", "Cause Processing",
                $"Closed cause {cause.CauseCampaignpRef} as the target was met.", ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task<List<string>> DeactivateUnpaidUsers(string causeRef, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
        var deactivatedRegNumbers = new List<string>();
        var cause = await dbContext.Cause.FirstOrDefaultAsync(c => c.CauseCampaignpRef == causeRef, ct);

        if (cause == null)
        {
            await auditService.RecordAuditAsync("Deactivation Skipped", "Cause Processing",
                $"Cause not found for ref: {causeRef}", ct);
            return deactivatedRegNumbers;
        }

        var underAgeLimit = cause.UnderAge > 0 ? cause.UnderAge : 18;
        var startDate = cause.StartDate ?? cause.DateCreated;
        var missedPayments = await GetMissedPaymentsForCause(causeRef, ct);

        foreach (var missed in missedPayments)
        {
            if (string.IsNullOrEmpty(missed.RegNumber))
            {
                await auditService.RecordAuditAsync("User Deactivation Skipped", "Cause Processing",
                    $"Skipped deactivation: Missing RegNumber for cause {causeRef}", ct);
                continue;
            }

            var user = await dbContext.Users
                .FirstOrDefaultAsync(a => a.PersonRegNumber == missed.RegNumber && a.IsActive == true, ct);

            var dependent = await dbContext.Dependants
                .FirstOrDefaultAsync(a => a.PersonRegNumber == missed.RegNumber && a.IsActive == true, ct);

            // ✅ Skip if under the age limit at campaign start
            if (dependent != null &&
                auditService.CalculateAgeAtDate(dependent.PersonYearOfBirth, startDate) < underAgeLimit)
            {
                await auditService.RecordAuditAsync("Deactivation Skipped", "Cause Processing",
                    $"Skipped {dependent.PersonRegNumber} ({dependent.PersonName}) — under age limit ({underAgeLimit}) at campaign start.", ct);
                continue;
            }

            if (dependent != null)
            {
                dependent.IsActive = false;
                dependent.DeactivationReason = "Missed Payment";
              dependent.DeactivationDate = DateTime.UtcNow;
            }

            if (user != null)
            {
                user.IsActive = false;
                user.DeactivationReason = "Missed Payment";
                user.DeactivationDate = DateTime.UtcNow;
            }

            deactivatedRegNumbers.Add(missed.RegNumber);

            var deactivationNote = new NoteHistory
            {
                NoteTypeId = 1,
                PersonRegNumber = dependent?.PersonRegNumber ?? missed.RegNumber,
                Description = $"User {dependent?.PersonName ?? "Unknown"} (Reg#: {missed.RegNumber}) was deactivated due to missed payment for Cause Ref {causeRef}.",
                CreatedBy = "System",
                CreatedByName = "Automated Process",
                DateCreated = DateTime.UtcNow
            };

            await dbContext.NoteHistory.AddAsync(deactivationNote, ct);
        }

        await dbContext.SaveChangesAsync(ct);
        return deactivatedRegNumbers;
    }

    private async Task SendDeactivateUnpaidUsers(string causeRef, List<string> deactivatedRegNumbers, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
        var cause = await dbContext.Cause.FirstOrDefaultAsync(c => c.CauseCampaignpRef == causeRef, ct);

        List<string> recipientEmails = new();
        List<string> failedEmails = new();
        List<string> successEmails = new();

        foreach (var regNumber in deactivatedRegNumbers.Distinct())
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(a => a.PersonRegNumber == regNumber, ct);
            var dependent = await dbContext.Dependants.FirstOrDefaultAsync(a => a.PersonRegNumber == regNumber, ct);

            if (!string.IsNullOrWhiteSpace(user?.Email))
                recipientEmails.Add(user.Email);

            if (!string.IsNullOrWhiteSpace(dependent?.Email))
                recipientEmails.Add(dependent.Email);
        }

        if (!recipientEmails.Any())
            return;

        const string templatePath = @"EmailTemplate/AccountTermination.html";
        string htmlBody;
        using (StreamReader reader = File.OpenText(templatePath))
        {
            htmlBody = await reader.ReadToEndAsync(ct);
        }

        var emailMessages = recipientEmails.Distinct().Select(email => new PostmarkMessage
        {
            From = "info@umojawetu.com",
            To = email,
            Subject = "Your Account Has Been Cancelled",
            HtmlBody = htmlBody
                .Replace("{{causeName}}", cause?.CauseCampaignpRef ?? "Unknown Cause")
                .Replace("{{ref}}", $"{cause?.CauseCampaignpRef} account term: ")
                .Replace("{{newEndDate}}", cause?.EndDate?.ToString("dd/MM/yyyy") ?? "N/A"),
            MessageStream = "broadcast",
            TrackOpens = true,
            TrackLinks = "None"
        }).ToList();

        const int BATCH_SIZE = 500;
        var emailBatches = emailMessages
            .Select((email, index) => new { email, index })
            .GroupBy(x => x.index / BATCH_SIZE)
            .Select(group => group.Select(x => x.email).ToList())
            .ToList();

        using var httpClient = new HttpClient { BaseAddress = new Uri("https://api.postmarkapp.com") };
        var postmarkApiKey = _configuration["Postmark:ApiKey"];
        httpClient.DefaultRequestHeaders.Add("X-Postmark-Server-Token", postmarkApiKey);

        foreach (var batch in emailBatches)
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var jsonPayload = JsonSerializer.Serialize(batch, jsonOptions);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("/email/batch", content, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                await auditService.RecordAuditAsync("Email API Failure", "User Termination",
                    $"Batch sending failed for terminated accounts. Response: {responseContent}", ct);
                continue;
            }

            var postmarkResponse = JsonSerializer.Deserialize<List<PostmarkResponseMessage>>(responseContent);
            if (postmarkResponse == null)
            {
                await auditService.RecordAuditAsync("Email API Failure", "User Termination",
                    $"Unexpected response format from Postmark: {responseContent}", ct);
                continue;
            }

            failedEmails.AddRange(postmarkResponse.Where(m => m.ErrorCode != 0).Select(m => m.To));
            successEmails.AddRange(postmarkResponse.Where(m => m.ErrorCode == 0).Select(m => m.To));
        }

        if (successEmails.Any())
        {
            await auditService.RecordAuditAsync("Email Success", "User Termination",
                $"Successfully sent termination emails to: {string.Join(", ", successEmails)}", ct);
        }

        if (failedEmails.Any())
        {
            await auditService.RecordAuditAsync("Email Failure", "User Termination",
                $"Failed to send termination emails to: {string.Join(", ", failedEmails)}", ct);
        }
    }

    private async Task<List<AdminMissedPaymentViewModel>> GetMissedPaymentsForCause(string causeRef, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var payments = await dbContext.Payment
            .Where(p => p.CauseCampaignpRef == causeRef)
            .ToListAsync(ct);

        var dependents = await dbContext.Dependants
            .Where(d => d.IsActive == true || d.IsActive == null)
            .ToListAsync(ct);

        var cause = await dbContext.Cause
            .FirstOrDefaultAsync(c => c.CauseCampaignpRef == causeRef, ct);

        if (cause == null) return new List<AdminMissedPaymentViewModel>();

        var minimumAge = cause.UnderAge > 0 ? cause.UnderAge : 18;
        var referenceDate = cause.StartDate ?? cause.DateCreated;

        return dependents
            .Where(d =>
                !payments.Any(p => p.personRegNumber == d.PersonRegNumber) &&
                CalculateAgeAtYear(d.PersonYearOfBirth.ToString(), referenceDate) >= minimumAge &&
                d.DateCreated <= cause.IsClosedDate)
            .Select(d => new AdminMissedPaymentViewModel
            {
                DependentId = d.Id,
                DependentName = d.PersonName,
                UserId = d.UserId,
                Email = d.Email,
                PhoneNumber = d.Telephone,
                YearOfBirth = d.PersonYearOfBirth,
                RegNumber = d.PersonRegNumber,
                CauseCampaignpRef = causeRef,
                Amount = cause.FullMemberAmount,
                IsClosedDate = cause.IsClosedDate
            })
            .ToList();
    }

    private int CalculateAgeAtYear(string personYearOfBirth, DateTime startDate)
    {
        if (int.TryParse(personYearOfBirth, out var birthYear))
        {
            return startDate.Year - birthYear;
        }
        return 0; // Return 0 if parsing fails
    }
  
    //const string templatePath = @"EmailTemplate/CauseClosure.html";
    private async Task SendExtensionEmail(Cause cause, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var users = await userService.GetAllActiveUsersAsync(ct);

        if (!users.Any()) return; // Exit if no users found
        const string templatePath = @"EmailTemplate/CauseExtended.html";
        //const string templatePath = @"EmailTemplate/test.html";
        string htmlBody;
        using (StreamReader reader = File.OpenText(templatePath))
        {
            htmlBody = await reader.ReadToEndAsync();
        }

        htmlBody = htmlBody.Replace("{{causeName}}", cause.Description)
                            .Replace("{{ref}}", cause.CauseCampaignpRef + "extension: ")
                           .Replace("{{newEndDate}}", cause.EndDate?.ToString("dd/MM/yyyy"));

        var emailMessages = users.Select(user => new PostmarkMessage
        {
            From = "info@umojawetu.com",
            To = user.Email,
            Subject = $"Cause Extended - {cause.CauseCampaignpRef}",
            HtmlBody = htmlBody,
            MessageStream = "broadcast"
        }).ToList();

        await SendBatchEmailsAsync(emailMessages, "Cause Extension", $"Extension emails for cause {cause.CauseCampaignpRef}", ct);
    }

    private async Task SendClosureEmail(Cause cause, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var users = await userService.GetAllActiveUsersAsync(ct);

        if (!users.Any()) return; // Exit if no users found

        const string templatePath = @"EmailTemplate/causeClosure.html";
        string htmlBody;
        using (StreamReader reader = File.OpenText(templatePath))
        {
            htmlBody = await reader.ReadToEndAsync();
        }

        htmlBody = htmlBody.Replace("{{causeName}}", cause.Description)
               .Replace("{{ref}}", cause.CauseCampaignpRef + "closure: ")
                           .Replace("{{causeTarget}}", cause.TargetAmount.ToString("C"));

        var emailMessages = users.Select(user => new PostmarkMessage
        {
            From = "info@umojawetu.com",
            To = user.Email,
            Subject = $"Final Reminder: Cause Closure - {cause.CauseCampaignpRef}",
            HtmlBody = htmlBody,
            MessageStream = "broadcast"
        }).ToList();

        await SendBatchEmailsAsync(emailMessages, "Cause Closure", $"Closure emails for cause {cause.CauseCampaignpRef}", ct);
    }
    private async Task SendBatchEmailsAsync(List<PostmarkMessage> emailMessages, string auditCategory, string auditDescription, CancellationToken ct)
    {
        const int BATCH_SIZE = 500;
        var emailBatches = emailMessages
            .Select((email, index) => new { email, index })
            .GroupBy(x => x.index / BATCH_SIZE)
            .Select(group => group.Select(x => x.email).ToList())
            .ToList();

        List<string> failedEmails = new();
        List<string> successEmails = new();

        var postmarkApiKey = _configuration["Postmark:ApiKey"];
        if (string.IsNullOrWhiteSpace(postmarkApiKey))
        {
            throw new Exception("Postmark API Key is missing in configuration.");
        }

        using var httpClient = new HttpClient { BaseAddress = new Uri("https://api.postmarkapp.com") };
        httpClient.DefaultRequestHeaders.Add("X-Postmark-Server-Token", postmarkApiKey);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        using var auditScope = _scopeFactory.CreateScope();
        var auditService = auditScope.ServiceProvider.GetRequiredService<IAuditService>();

        foreach (var batch in emailBatches)
        {
            var jsonPayload = JsonSerializer.Serialize(batch, jsonOptions);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("/email/batch", content, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);
         
            if (!response.IsSuccessStatusCode)
            {
                await auditService.RecordAuditAsync("Email API Failure", auditCategory,
                    $"{auditCategory} - Batch sending failed. Response: {responseContent}", ct);
                continue;
            }

            var postmarkResponse = JsonSerializer.Deserialize<List<PostmarkResponseMessage>>(responseContent);

            if (postmarkResponse == null)
            {
                await auditService.RecordAuditAsync("Email API Failure", auditCategory,
                    $"{auditCategory} - Unexpected response format from Postmark: {responseContent}", ct);
                continue;
            }

            failedEmails.AddRange(postmarkResponse.Where(m => m.ErrorCode != 0).Select(m => m.To));
            successEmails.AddRange(postmarkResponse.Where(m => m.ErrorCode == 0).Select(m => m.To));
        }

        //  Retry sending failed emails in smaller batches
        if (failedEmails.Count > 0)
        {
            foreach (var failedEmail in failedEmails)
            {
                var retryMessage = emailMessages.FirstOrDefault(m => m.To == failedEmail);
                if (retryMessage == null) continue;

                var retryPayload = JsonSerializer.Serialize(new { Messages = new List<PostmarkMessage> { retryMessage } }, jsonOptions);
                var retryContent = new StringContent(retryPayload, Encoding.UTF8, "application/json");
                var retryResponse = await httpClient.PostAsync("/email/batch", retryContent, ct);

                if (retryResponse.IsSuccessStatusCode)
                {
                    successEmails.Add(failedEmail);
                }
                else
                {
                    var retryResponseContent = await retryResponse.Content.ReadAsStringAsync(ct);
                    await auditService.RecordAuditAsync("Email Retry Failure", auditCategory,
                        $"{auditCategory} - Retry failed for {failedEmail}. Response: {retryResponseContent}", ct);
                }
            }
        }

        //  Log results
        if (successEmails.Any())
        {
            await auditService.RecordAuditAsync("Email Success", auditCategory,
                $"Successfully sent {auditDescription} to: {string.Join(", ", successEmails)}", ct);
        }

        if (failedEmails.Any())
        {
            await auditService.RecordAuditAsync("Email Failure", auditCategory,
                $"Failed to send {auditDescription} to: {string.Join(", ", failedEmails)}", ct);
        }

        await auditService.RecordAuditAsync($"{auditCategory} Emails Sent", auditCategory,
            $"Sent {emailMessages.Count} {auditDescription}. Successful: {successEmails.Count}, Failed: {failedEmails.Count}", ct);
    }
 

}
