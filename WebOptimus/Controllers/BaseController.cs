namespace WebOptimus.Controllers
{

  
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using WebOptimus.Data;
    using WebOptimus.Helpers;
    using WebOptimus.Models;

    public class BaseController : Controller
    {
        protected readonly UserManager<User> _userManager;
        protected readonly ApplicationDbContext _db;
        protected readonly RequestIpHelper _requestIpHelper;

        public BaseController(UserManager<User> userManager, ApplicationDbContext db, RequestIpHelper requestIpHelper)
        {
            _userManager = userManager;
            _db = db;
            _requestIpHelper = requestIpHelper;
        }

        protected async Task RecordAuditAsync(string actionType, string description)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            var currentUser = string.IsNullOrEmpty(email) ? null : await _userManager.FindByEmailAsync(email);
            string myIP = _requestIpHelper.GetRequestIp();
            await RecordAuditAsync(currentUser, myIP, actionType, description);
            return;
        }
        protected async Task LogPageNavigation(string pageName, string actionName, string controllerName)
        {
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");
            if (currentUserEmail != null)
            {
                var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
                var log = new PageNavigationLog
                {
                    UserId = Guid.Parse(_userManager.GetUserId(User)),
                    Email = currentUserEmail,
                    PageName = pageName,
                    ActionName = actionName,
                    ControllerName = controllerName,
                    Timestamp = DateTime.UtcNow
                };

                _db.PageNavigationLogs.Add(log);
                await _db.SaveChangesAsync();
            }
        }
        protected async Task RecordAuditAsync(User? currentUser, string myIP, string actionType, string description)
        {
            Audit addAudit = new()
            {
                UserID = currentUser?.UserId,
                Email = currentUser!= null ? currentUser.Email : string.Empty,
                ActionType = actionType,
                ActionDesc = description,
                DateCreated = DateTime.UtcNow,
                IPAddress = myIP
            };
            _db.Audits.Add(addAudit);
            await _db.SaveChangesAsync();
            return;
        }

        protected async Task RecordAuditAsync(Guid? userId, string email, string myIP, string actionType, string description)
        {
            Audit addAudit = new()
            {
                UserID = userId,
                Email = email,
                ActionType = actionType,
                ActionDesc = description,
                DateCreated = DateTime.UtcNow,
                IPAddress = myIP
            };
            _db.Audits.Add(addAudit);
            await _db.SaveChangesAsync();
            return;
        }

        protected void RecordAudit(Guid? userId, string email, string myIP, string actionType, string errorType, string description)
        {
            Audit addAudit = new()
            {
                UserID = userId,
                Email = email,
                ActionType = actionType,
                ActionDesc = description,
                ErrorType = errorType,
                DateCreated = DateTime.UtcNow,
                IPAddress = myIP
            };
            _db.Audits.Add(addAudit);
            _db.SaveChanges();
            return;
        }

        protected async Task RecordAuditAsync(Guid? userId, string email, string myIP, string actionType, string errorType, string description)
        {
            Audit addAudit = new()
            {
                UserID = userId,
                Email = email,
                ActionType = actionType,
                ActionDesc = description,
                ErrorType = errorType,
                DateCreated = DateTime.UtcNow,
                IPAddress = myIP
            };
            _db.Audits.Add(addAudit);
            await _db.SaveChangesAsync();
            return;
        }

        protected async Task RecordAuditAsync(User? currentUser, string myIP, string actionType, string description, CancellationToken ct)
        {
            Audit addAudit = new()
            {
                UserID = currentUser?.UserId,
                Email = currentUser != null ? currentUser.Email : string.Empty,
                ActionType = actionType,
                ActionDesc = description,
                DateCreated = DateTime.UtcNow,
                IPAddress = myIP
            };
            _db.Audits.Add(addAudit);
            await _db.SaveChangesAsync(ct);
            return;
        }

        protected async Task RecordAuditAsync(string actionType, string description, CancellationToken ct)
        {
            var currentUser = await _userManager.FindByEmailAsync(HttpContext.Session.GetString("loginEmail"));
            string myIP = _requestIpHelper.GetRequestIp();
            await RecordAuditAsync(currentUser, myIP, actionType, description, ct);
            return;
        }

        protected async Task RecordAuditAsync(User? currentUser, string myIP, string actionType, string errorType, string description)
        {
            Audit addAudit = new()
            {
                UserID = currentUser?.UserId,
                Email = currentUser != null ? currentUser.Email : string.Empty,
                ActionType = actionType,
                ErrorType = errorType,
                ActionDesc = description,
                DateCreated = DateTime.UtcNow,
                IPAddress = myIP
            };
            _db.Audits.Add(addAudit);
            await _db.SaveChangesAsync();
            return;
        }

        protected async Task RecordAuditAsync(string actionType, string errorType, string description)
        {
            var currentUser = await _userManager.FindByEmailAsync(HttpContext.Session.GetString("loginEmail"));
            string myIP = _requestIpHelper.GetRequestIp();
            await RecordAuditAsync(currentUser, myIP, actionType, errorType, description);
            return;
        }

        protected async Task RecordAuditAsync(string actionType, string errorType, string description, CancellationToken ct)
        {
            var currentUser = await _userManager.FindByEmailAsync(HttpContext.Session.GetString("loginEmail"));
            string myIP = _requestIpHelper.GetRequestIp();
            await RecordAuditAsync(currentUser, myIP, actionType, errorType, description, ct);
            return;
        }

        protected async Task RecordAuditAsync(User? currentUser, string myIP, string actionType, string errorType, string description, CancellationToken ct)
        {
            Audit addAudit = new()
            {
                UserID = currentUser?.UserId,
                Email = currentUser != null ? currentUser.Email : string.Empty,
                ActionType = actionType,
                ErrorType = errorType,
                ActionDesc = description,
                DateCreated = DateTime.UtcNow,
                IPAddress = myIP
            };
            _db.Audits.Add(addAudit);
            await _db.SaveChangesAsync(ct);
            return;
        }

        protected async Task RecordAuditAsync(Guid? userId, string email, string myIP, string actionType, string description, CancellationToken? ct = null)
        {
            Audit addAudit = new()
            {
                UserID = userId,
                Email = email,
                ActionType = actionType,
                ActionDesc = description,
                DateCreated = DateTime.UtcNow,
                IPAddress = myIP
            };

            if (ct.HasValue)
            {
                await _db.Audits.AddAsync(addAudit, ct.Value);
                await _db.SaveChangesAsync(ct.Value);
            }
            else
            {
                await _db.Audits.AddAsync(addAudit);
                await _db.SaveChangesAsync();
            }
            return;
        }

        protected async Task RecordAuditAsync(Guid? userId, string email, string myIP, string actionType, string errorType, string description, CancellationToken? ct = null)
        {
            Audit addAudit = new()
            {
                UserID = userId,
                Email = email,
                ActionType = actionType,
                ErrorType = errorType,
                ActionDesc = description,
                DateCreated = DateTime.UtcNow,
                IPAddress = myIP
            };

            if (ct.HasValue)
            {
                await _db.Audits.AddAsync(addAudit, ct.Value);
                await _db.SaveChangesAsync(ct.Value);
            }
            else
            {
                await _db.Audits.AddAsync(addAudit);
                await _db.SaveChangesAsync();
            }
            return;
        }

      
    }
}
