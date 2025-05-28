using System.Threading;
using System.Threading.Tasks;

namespace WebOptimus.Services
{
    public interface IAuditService
    {
        public int CalculateAge(string yearOfBirth);

        int CalculateAgeAtDate(string yearOfBirth, DateTime referenceDate);
        Task RecordAuditAsync(string action, string category, string description, CancellationToken ct);
    }
}
