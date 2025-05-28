using WebOptimus.Data;
using WebOptimus.Models;
using WebOptimus.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;

    public AuditService(ApplicationDbContext db)
    {
        _db = db;
    }
    public int CalculateAge(string yearOfBirth)
    {
        if (int.TryParse(yearOfBirth, out var birthYear))
        {
            return DateTime.Now.Year - birthYear;
        }
        return 0;
    }
    public int CalculateAgeAtDate(string yearOfBirth, DateTime referenceDate)
    {
        if (int.TryParse(yearOfBirth, out var birthYear))
        {
            return referenceDate.Year - birthYear;
        }
        return 0;
    }

    public async Task RecordAuditAsync(string action, string category, string description, CancellationToken ct)
    {
        Audit addAudit = new()
        {
            UserID = null,
            Email = "backgroundservice@umojawetu.com",
            ActionType = action,
            ErrorType = category,
            ActionDesc = description,
            DateCreated = DateTime.UtcNow
        };

        _db.Audits.Add(addAudit);
        await _db.SaveChangesAsync(ct);
    }
}
