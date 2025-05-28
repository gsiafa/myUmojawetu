
using Microsoft.EntityFrameworkCore;
using WebOptimus.Data;


namespace WebOptimus.Services
{
    public partial class MissedPaymentService : IMissedPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MissedPaymentService> _logger;

        public MissedPaymentService(ApplicationDbContext db, ILogger<MissedPaymentService> logger)
        {
            _context = db;
            _logger = logger;
        }
        public async Task ProcessMissedPaymentsEmail(CancellationToken stoppingToken)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            var count = await _context.Payment.CountAsync(stoppingToken);
       

            await strategy.ExecuteAsync(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync(stoppingToken))
                {
                    try
                    {
                        string calculationSql = @"
                        DECLARE @CalculationDate DATETIME2 = GETDATE();

                        INSERT INTO LeaseContractMonthlyCalculations (
                        [LessorId], [CompanyNumber], [ChangeInBookValue], [ChangeInBookValue_Rate], 
                        [CurrentBookValue], [CalculationDate], [ContractNumber], [NumberOfLessors], [NumberOfAgreements]
                         )
                        SELECT
                        LC.LessorId,
                        LC.CompanyNumber,
                        (SUM(LC.CurrentBookValue) - ISNULL(MAX(HIST.PreviousBookValue), 0)) AS ChangeInBookValue,
                        CASE 
                            WHEN ISNULL(MAX(HIST.PreviousBookValue), 0) = 0 THEN 0
                            ELSE ((SUM(LC.CurrentBookValue) - ISNULL(MAX(HIST.PreviousBookValue), 0)) / MAX(HIST.PreviousBookValue)) * 100
                        END AS ChangeInBookValue_Rate,
                        SUM(LC.CurrentBookValue) AS CurrentBookValue,
                        @CalculationDate,
                        MAX(LC.ContractNumber) AS ContractNumber,
                        COUNT(DISTINCT LC.LessorId) AS NumberOfLessors,
                        COUNT(DISTINCT LC.ContractNumber) AS NumberOfAgreements
                        FROM 
                        LeaseContract LC
                        LEFT JOIN 
                        (SELECT CompanyNumber, SUM(CurrentBookValue) AS PreviousBookValue
                         FROM LeaseContract_Historic
                         GROUP BY CompanyNumber) HIST
                        ON LC.CompanyNumber = HIST.CompanyNumber
                        GROUP BY 
                        LC.CompanyNumber, 
                        LC.LessorId;
                        ";

                        await _context.Database.ExecuteSqlRawAsync(calculationSql, stoppingToken);
                        string batchSql = @"
                        DELETE FROM LeaseContract_Historic;

                        INSERT INTO LeaseContract_Historic (
                        LeaseContractId, ContractNumber, CompanyNumber, CompanyName, 
                        CurrentBookValue, ContractStartDate, ContractEndDate, LessorId, StampDate
                        )
                        SELECT 
                        LeaseContractId, ContractNumber, CompanyNumber, CompanyName, 
                        CurrentBookValue, ContractStartDate, ContractEndDate, LessorId, GETDATE()
                        FROM 
                        LeaseContract;
                        ";

                        await _context.Database.ExecuteSqlRawAsync(batchSql, stoppingToken);
                        await transaction.CommitAsync(stoppingToken);
                    
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(stoppingToken);                   
                        throw;
                    }
                }
            });
        }

    }
}
