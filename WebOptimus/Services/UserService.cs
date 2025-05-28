using Microsoft.EntityFrameworkCore;
using WebOptimus.Data;
using WebOptimus.Models;
using WebOptimus.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _db;

    public UserService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Dependant>> GetAllActiveUsersAsync(CancellationToken ct)
    {
        return await _db.Dependants
            .Where(u => u.IsActive == true) 
            .ToListAsync(ct);
    }
}
