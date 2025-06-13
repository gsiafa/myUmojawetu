using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using WebOptimus.Models;

namespace WebOptimus.Services.StoreProcedure
{
    public class StoredProcedureEmailService : IStoredProcedureEmailService
    {
        private readonly IConfiguration _config;
        private readonly IPostmarkClient _postmarkClient;
        private readonly string _connectionString;

        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CauseManagementService> _logger;


        public StoredProcedureEmailService(
         IConfiguration config,
         IPostmarkClient postmarkClient,
         IServiceScopeFactory scopeFactory,
         ILogger<CauseManagementService> logger)
        {
            _config = config;
            _postmarkClient = postmarkClient;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection");
        }

        public async Task RunProcedureAndSendEmailAsync(string procedureName, string emailSubject, string toEmail, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

            var results = new List<Dictionary<string, object>>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(procedureName, conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            await conn.OpenAsync(ct);
            using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                results.Add(row);
            }

            if (results.Any())
            {
                var htmlBody = BuildHtmlTable(results);
                var fullBody = $"<p>Results from <strong>{procedureName}</strong>:</p>{htmlBody}";

                var message = new PostmarkMessage
                {
                    From = "info@umojawetu.com",
                    To = toEmail,
                    Subject = emailSubject,
                    HtmlBody = fullBody,
                    MessageStream = "outbound"
                };

                try
                {
                    await _postmarkClient.SendMessageAsync(message, ct);

                    await auditService.RecordAuditAsync(
                       "RunProcedureAndSendEmailAsync",
                       "Run Procedure Success",
                       $"Procedure {procedureName} returned {results.Count} rows and email was sent.",
                       ct
                    );

                }
                catch (Exception ex)
                {
                    await auditService.RecordAuditAsync(
                      "RunProcedureAndSendEmailAsync",
                      "Run Procedure And SendEmailAsync Error",
                      $"Failed to send email for procedure {procedureName}: {ex.Message}",
                      ct
                  );
                   
                }

            }
        }

        public async Task RunAllActiveProceduresAsync(CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            using var conn = new SqlConnection(_connectionString);
            var procedures = new List<ScheduledStoredProcedure>();

            using var cmd = new SqlCommand("SELECT Id, ProcedureName, EmailSubject, ToEmail, IsActive, LastRunDate, CreatedOn, ScheduledTime, Frequency, DayOfMonth, IsRunNow \r\nFROM ScheduledStoredProcedure \r\nWHERE IsActive = 1\r\n", conn);
            await conn.OpenAsync(ct);
            using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                procedures.Add(new ScheduledStoredProcedure
                {
                    Id = reader.GetInt32(0),
                    ProcedureName = reader.GetString(1),
                    EmailSubject = reader.GetString(2),
                    ToEmail = reader.GetString(3),
                    IsActive = reader.GetBoolean(4),
                    LastRunDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    CreatedOn = reader.GetDateTime(6),
                    ScheduledTime = reader.GetTimeSpan(7),
                    Frequency = (ProcedureFrequency)reader.GetInt32(8),
                    DayOfMonth = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    IsRunNow = reader.GetBoolean(10)
                });
            }

            var ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ukTimeZone);

            await auditService.RecordAuditAsync("RunAllActiveProceduresAsync", "Time now is ", $"Procedure {now} UK TIME", ct);
            foreach (var proc in procedures)
            {
                var lastRun = proc.LastRunDate;
                var scheduledTimeToday = now.Date + proc.ScheduledTime;

                bool shouldRun = false;

                if (proc.IsRunNow)
                {
                    shouldRun = true;
                }
                else
                {
                    switch (proc.Frequency)
                    {
                        case ProcedureFrequency.Hourly:
                            shouldRun = !lastRun.HasValue || (now - lastRun.Value).TotalHours >= 1;
                            break;

                        case ProcedureFrequency.Daily:
                            shouldRun = now >= scheduledTimeToday && (!lastRun.HasValue || lastRun.Value.Date != now.Date);
                            break;

                        case ProcedureFrequency.Monthly:
                            shouldRun = now.Day == (proc.DayOfMonth ?? 1)
                                        && now >= scheduledTimeToday
                                        && (!lastRun.HasValue || lastRun.Value.Month != now.Month);
                            break;
                    }
                }

                if (shouldRun)
                {
                    await RunProcedureAndSendEmailAsync(proc.ProcedureName, proc.EmailSubject, proc.ToEmail, ct);
                    
                    await UpdateLastRunDateAsync(proc.Id, now, ct);

                    // Reset the IsRunNow flag if it was a manual trigger
                    if (proc.IsRunNow)
                    {
                        await ResetRunNowFlagAsync(proc.Id, ct);
                        await auditService.RecordAuditAsync("Manual Trigger", "RunNow flag", $"Procedure {proc.ProcedureName} triggered manually.", ct);
                    }
                }
                var nextRun = GetNextRunTime(proc, now);

                await auditService.RecordAuditAsync(
                    "Procedure Schedule",
                    "Next Run Time",
                    $"Procedure: {proc.ProcedureName}, Next run: {(nextRun?.ToString("yyyy-MM-dd HH:mm") ?? "unknown")}",
                    ct
                );

            }


        }
        private async Task ResetRunNowFlagAsync(int procedureId, CancellationToken ct = default)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("UPDATE ScheduledStoredProcedure SET IsRunNow = 0 WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", procedureId);
            await conn.OpenAsync(ct);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        private async Task UpdateLastRunDateAsync(int procedureId, DateTime runDateTime, CancellationToken ct = default)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("UPDATE ScheduledStoredProcedure SET LastRunDate = @RunDate WHERE Id = @Id", conn);

            cmd.Parameters.AddWithValue("@RunDate", runDateTime);
            cmd.Parameters.AddWithValue("@Id", procedureId);

            await conn.OpenAsync(ct);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        private DateTime? GetNextRunTime(ScheduledStoredProcedure proc, DateTime now)
        {
            var baseTime = now.Date + proc.ScheduledTime;

            return proc.Frequency switch
            {
                ProcedureFrequency.Hourly => now.AddHours(1),
                ProcedureFrequency.Daily => baseTime > now ? baseTime : baseTime.AddDays(1),
                ProcedureFrequency.Monthly =>
                    new DateTime(
                        now.Year,
                        now.Month == 12 ? 1 : now.Month + 1,
                        proc.DayOfMonth ?? 1,
                        proc.ScheduledTime.Hours,
                        proc.ScheduledTime.Minutes,
                        0),
                _ => null
            };
        }

        private string BuildHtmlTable(List<Dictionary<string, object>> rows)
        {
            if (rows == null || rows.Count == 0)
                return "<p>No results returned.</p>";

            var sb = new StringBuilder();
            var headers = rows[0].Keys;

            sb.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse; width: 100%;'>");
            sb.Append("<thead><tr>");
            foreach (var header in headers)
            {
                sb.Append($"<th style='background-color: #f2f2f2;'>{header}</th>");
            }
            sb.Append("</tr></thead><tbody>");
            
           
            foreach (var row in rows)
            {
                sb.Append("<tr>");
                foreach (var cell in row.Values)
                {
                    sb.Append($"<td>{cell}</td>");
                }
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");
            return sb.ToString();
        }
    }
}
