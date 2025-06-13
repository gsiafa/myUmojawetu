using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebOptimus.Services.StoreProcedure
{
    public class DailyProcedureRunner : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CauseManagementService> _logger;
        private readonly IPostmarkClient _postmark;
        private readonly IConfiguration _configuration;
       
        public DailyProcedureRunner(IServiceProvider serviceProvider, IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<CauseManagementService> logger, IPostmarkClient postmark)
        {
            _serviceProvider = serviceProvider;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _postmark = postmark;
            _configuration = configuration;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var procEmailService = scope.ServiceProvider.GetRequiredService<IStoredProcedureEmailService>();

                    await procEmailService.RunAllActiveProceduresAsync(stoppingToken);

                    // Calculate next run time (5-minute interval)
                    var delay = TimeSpan.FromMinutes(5);
                    var nextRunUtc = DateTime.UtcNow.Add(delay);
                    var nextRunUk = TimeZoneInfo.ConvertTimeFromUtc(nextRunUtc, TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"));

                    _logger.LogInformation("Background procedure executed at {now}. Next run scheduled for {nextRunUk}.",
                        DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                        nextRunUk.ToString("yyyy-MM-dd HH:mm:ss"));

                    await auditService.RecordAuditAsync(
                        "Daily Procedure Runner Scheduled",
                        "Processing",
                        $"Background ran at {DateTime.UtcNow:HH:mm:ss}. Next run at {nextRunUk:HH:mm:ss}.",
                        stoppingToken
                    );

                    await Task.Delay(delay, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in DailyProcedureRunner: {Message}", ex.Message);

                    await auditService.RecordAuditAsync(
                        "Cause Management Scheduled",
                        "Cause Processing Error",
                        $"Exception occurred: {ex.Message}",
                        stoppingToken
                    );

                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // retry sooner on error
                }
            }
        }

    }
}
