namespace WebOptimus.Services
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using WebOptimus.Data;
    using Microsoft.EntityFrameworkCore;
    using WebOptimus.Models;

    public partial class ScheduledMissedPayments : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScheduledMissedPayments> _logger;
        private PeriodicTimer _timer;

        public ScheduledMissedPayments(IServiceProvider serviceProvider, ILogger<ScheduledMissedPayments> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        }

        // The method to trigger manually
        public async Task ManualTriggerMissedPaymentsEmail(CancellationToken cancellationToken)
        {
            try
            {
                using IServiceScope scope = _serviceProvider.CreateScope();
                var leaseContractService = scope.ServiceProvider.GetRequiredService<IMissedPaymentService>();
                await leaseContractService.ProcessMissedPaymentsEmail(cancellationToken);

              
            }
            catch (Exception ex)
            {
             
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var settings = GetSchedulingSettings();

                if (settings == null)
                {
                  
                    return;
                }

                if (settings.RunEveryDay)
                {
                    _timer = new PeriodicTimer(TimeSpan.FromDays(1));
                }
                else
                {
                    _timer = new PeriodicTimer(TimeSpan.FromMinutes(settings.IntervalValue));
                }

                if (await _timer.WaitForNextTickAsync(stoppingToken))
                {
                    if (!settings.RunEveryDay || DateTime.Now.Day == settings.IntervalValue)
                    {
                        try
                        {
                            using IServiceScope scope = _serviceProvider.CreateScope();
                            var leaseContractService = scope.ServiceProvider.GetRequiredService<IMissedPaymentService>();
                            await leaseContractService.ProcessMissedPaymentsEmail(stoppingToken);

                        
                        }
                        catch (Exception ex)
                        {
                           
                        }
                    }
                }
            }
        }

        public override void Dispose()
        {
            _timer.Dispose();
            base.Dispose();
        }

        private MissedPaymentsEmailSettings GetSchedulingSettings()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var sqlQuery = "SELECT TOP 1 RunEveryDay, IntervalValue FROM MissedPaymentsEmailSettings ORDER BY Id DESC";
                using var command = dbContext.Database.GetDbConnection().CreateCommand();
                command.CommandText = sqlQuery;
                command.CommandType = System.Data.CommandType.Text;

                if (dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                {
                    dbContext.Database.GetDbConnection().Open();
                }

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return new MissedPaymentsEmailSettings
                    {
                        RunEveryDay = reader.GetBoolean(reader.GetOrdinal("RunEveryDay")),
                        IntervalValue = reader.GetInt32(reader.GetOrdinal("IntervalValue"))
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        

    }
}
