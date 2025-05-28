using System.Threading;
using System.Threading.Tasks;
using WebOptimus.Models;

namespace WebOptimus.Services
{
    public interface IUserService
    {
        Task<List<Dependant>> GetAllActiveUsersAsync(CancellationToken ct);
    }
}
