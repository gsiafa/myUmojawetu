namespace WebOptimus.Services.StoreProcedure
{
    public interface IStoredProcedureEmailService
    {
        Task RunProcedureAndSendEmailAsync(string procedureName, string emailSubject, string toEmail, CancellationToken ct = default);
        Task RunAllActiveProceduresAsync(CancellationToken ct = default);

    }

}
