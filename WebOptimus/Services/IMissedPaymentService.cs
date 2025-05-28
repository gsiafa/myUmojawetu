using System.Threading;
using System.Threading.Tasks;

namespace WebOptimus.Services
{
    public interface IMissedPaymentService
    {
        Task ProcessMissedPaymentsEmail(CancellationToken stoppingToken);
    }
}
