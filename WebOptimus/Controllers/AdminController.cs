using AutoMapper;
using WebOptimus.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebOptimus.Models;
using WebOptimus.Helpers;
using WebOptimus.Data;
using Microsoft.EntityFrameworkCore;
using WebOptimus.Models.ViewModel;
using WebOptimus.StaticVariables;
using WebOptimus.Configuration;
using WebOptimus.Models.ViewModel.Admin;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Web;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using DependentChangeLog = WebOptimus.Models.DependentChangeLog;
namespace WebOptimus.Controllers
{
    public class AdminController : BaseController
    {
        private readonly IMapper _mapper;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<User> _userStore;
        private readonly IDataProtector protector;
        private readonly IPasswordValidator<User> passwordValidator;
        private readonly IUserValidator<User> userValidator;
        private readonly IPasswordHasher<User> passwordHasher;
        private readonly RequestIpHelper ipHelper;
        private readonly HttpClient httpClient;
        private readonly IPostmarkClient _postmark;
        private readonly IAuditService _auditService;
        public AdminController(IMapper mapper, UserManager<User> userManager,
           SignInManager<User> signInManager, RoleManager<IdentityRole> roleManager, IUserStore<User> userStore,
            RequestIpHelper ipHelper,
          HttpClient httpClient,
             IPostmarkClient postmark,
                IAuditService auditService,
           IPasswordHasher<User> passwordHash,
           IUserValidator<User> userValid,
           IPasswordValidator<User> passwordVal,
           ApplicationDbContext db) :
           base(userManager, db, ipHelper)
        {
            _mapper = mapper;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _userStore = userStore;
            passwordValidator = passwordVal;
            userValidator = userValid;
            passwordHasher = passwordHash;
            this.ipHelper = ipHelper;
            this.httpClient = httpClient;
            _postmark = postmark;
            _auditService = auditService;
        }

        [HttpGet]
        [Authorize(Roles = "General Admin,Local Admin,Regional Admin")]
       
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                if (email == null)
                {
                    TempData["Error"] = "Session does not contain loginEmail.";
                    return RedirectToAction("Logout", "Account");
                }

                var userRole = HttpContext.Session.GetString("adminuser");
                var currentUser = await _userManager.FindByEmailAsync(email);
                if (currentUser == null)
                {
                    TempData["Error"] = $"User not found: {email}";
                    return RedirectToAction("Logout", "Account");
                }

                var allDependents = await _db.Dependants
                    .Where(d => d.IsActive == true || d.IsActive == null)
                    .ToListAsync(ct);

                var reportedDeadQuery = _db.ReportedDeath
                    .Where(d => d.Status != DeathStatus.Approved)
                    .AsQueryable();

                var regions = await _db.Region.ToListAsync(ct);
                var minimumage = await _db.Settings
                .Where(s => s.IsActive == true && s.MinimumAge > 0)
                .Select(s => s.MinimumAge)
                .FirstOrDefaultAsync();

                if (minimumage == 0)
                {
                    minimumage = 18;
                }

                // Filter by role
                if (userRole == RoleList.RegionalAdmin)
                {
                    allDependents = allDependents.Where(d => d.RegionId == currentUser.RegionId).ToList();
                    reportedDeadQuery = reportedDeadQuery.Where(d => d.RegionId == currentUser.RegionId);
                }
                else if (userRole == RoleList.LocalAdmin)
                {
                    allDependents = allDependents.Where(d => d.CityId == currentUser.CityId).ToList();
                    reportedDeadQuery = reportedDeadQuery.Where(d => d.CityId == currentUser.CityId);
                }

                var reportedDead = await reportedDeadQuery.ToListAsync(ct);

                // Remove dependents with null DOB when calculating under/over
                var validDependents = allDependents.Where(d => d.PersonYearOfBirth != null).ToList();

                var Under18Count = validDependents.Count(d => _auditService.CalculateAge(d.PersonYearOfBirth) < minimumage);
                var Over18Count = validDependents.Count(d => _auditService.CalculateAge(d.PersonYearOfBirth) >= minimumage);
                var totalDependents = allDependents.Count;

                // Set ViewBag values
                ViewBag.totalusers = totalDependents;
                ViewBag.UnderAge = Under18Count;
                ViewBag.OverAge = Over18Count;
                ViewBag.confirmedDeath = reportedDead.Count();

                // Chart data per region
                ViewBag.RegionNames = JsonSerializer.Serialize(regions.Select(r => r.Name).ToArray());

                ViewBag.RegionCountsAll = JsonSerializer.Serialize(
                    regions.Select(r => allDependents.Count(d => d.RegionId == r.Id)).ToArray()
                );

                ViewBag.RegionCountsUnder18 = JsonSerializer.Serialize(
                    regions.Select(r => allDependents.Count(d =>
                        d.RegionId == r.Id &&
                        d.PersonYearOfBirth != null &&
                        _auditService.CalculateAge(d.PersonYearOfBirth) < minimumage
                    )).ToArray()
                );

                ViewBag.RegionCountsOver18 = JsonSerializer.Serialize(
                    regions.Select(r => allDependents.Count(d =>
                        d.RegionId == r.Id &&
                        d.PersonYearOfBirth != null &&
                        _auditService.CalculateAge(d.PersonYearOfBirth) >= minimumage
                    )).ToArray()
                );

                ViewBag.minimumAge = minimumage;
                ViewBag.userRole = userRole;

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading the dashboard.";
                return RedirectToAction("Logout", "Account");
            }
        }


        [HttpGet]
          [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> AllDependents(CancellationToken ct)
        {
            var dependents = await _db.Dependants.ToListAsync(ct);
            var dependentsViewModel = dependents.Select(d => new DependentViewModel
            {
                DependentName = d.PersonName,
                PersonRegNumber = d.PersonRegNumber,
                YearOfBirth = d.PersonYearOfBirth,
                Age = CalculateAge(d.PersonYearOfBirth)
            }).ToList();

            return View(dependentsViewModel);
        }
        
        private int CalculateAge(string yearOfBirth)
        {
            if (int.TryParse(yearOfBirth, out var birthYear))
            {
                return DateTime.Now.Year - birthYear;
            }
            return 0;
        }

        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> ChangeLogs(CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _userManager.FindByEmailAsync(email);

            if (email == null)
            {
                TempData[SD.Error] = "Session Expires - Please login.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var changeLogs = await _db.ChangeLogs
            .OrderByDescending(c => c.ChangeDate)
            .Select(c => new ChangeLogsVM
            {
                Id = c.Id,
                UserId = c.UserId,
                DependentId = c.DependentId,
                FieldChanged = c.FieldChanged,
                OldValue = c.OldValue,
                NewValue = c.NewValue,
                ChangeDate = c.ChangeDate,
                ChangedBy = c.ChangedBy
            })
            .ToListAsync();

                if (changeLogs == null || !changeLogs.Any())
                {
                    TempData["Error"] = "No change logs found.";
                }
                else
                {
                    TempData["Success"] = $"Found {changeLogs.Count} change logs.";
                }

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Navigation", "user navigated to change logs page", ct);

                return View(changeLogs);
            }
            catch (Exception ex)
            {
                // Log the error (you could use a logging framework here)
                TempData["Error"] = "An error occurred while loading the change logs.";
                return RedirectToAction("Index", "Admin");
            }





        }

        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> DeathChangeLogs(CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _userManager.FindByEmailAsync(email);

            if (email == null)
            {
                TempData[SD.Error] = "Session Expires - Please login.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var changeLogs = await _db.ReportedDeathChangeLog
            .OrderByDescending(c => c.ChangeDate)
            .Select(c => new DeathChangeLogsVM
            {
                Id = c.Id,
                UserId = c.UserId,
                ReportedDeathId = c.ReportedDeathId,
                FieldName = c.FieldName,
                OldValue = c.OldValue,
                NewValue = c.NewValue,
                ChangeDate = c.ChangeDate,
                ChangedBy = c.ChangedBy
            })
            .ToListAsync();

                if (changeLogs == null || !changeLogs.Any())
                {
                    TempData["Error"] = "No change logs found.";
                }
                else
                {
                    TempData["Success"] = $"Found {changeLogs.Count} change logs.";
                }

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Navigation", "user navigated to reported death change logs page", ct);

                return View(changeLogs);
            }
            catch (Exception ex)
            {
                // Log the error (you could use a logging framework here)
                TempData["Error"] = "An error occurred while loading the change logs.";
                return RedirectToAction("Index", "Admin");
            }

        }


        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> MemberChangeLogs(CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _userManager.FindByEmailAsync(email);

            if (email == null)
            {
                TempData[SD.Error] = "Session Expires - Please login.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var changeLogs = await _db.UserProfileChangeLog
            .OrderByDescending(c => c.ChangeDate)
            .Select(c => new ChangeLogsVM
            {
                Id = c.Id,
                UserId = c.UserId,
                FieldName = c.FieldName,
                OldValue = c.OldValue,
                NewValue = c.NewValue,
                ChangeDate = c.ChangeDate,
                ChangedBy = c.ChangedBy
            })
            .ToListAsync();

                if (changeLogs == null || !changeLogs.Any())
                {
                    TempData["Error"] = "No change logs found.";
                }
                else
                {
                    TempData["Success"] = $"Found {changeLogs.Count} change logs.";
                }

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Navigation", "user navigated to change logs page", ct);

                return View(changeLogs);
            }
            catch (Exception ex)
            {
                // Log the error (you could use a logging framework here)
                TempData["Error"] = "An error occurred while loading the change logs.";
                return RedirectToAction("Index", "Admin");
            }





        }



        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> NextOfKinLogs(CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _userManager.FindByEmailAsync(email);

            if (email == null)
            {
                TempData[SD.Error] = "Session Expires - Please login.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var changeLogs = await _db.NextOfKinChangeLogs
            .OrderByDescending(c => c.ChangeDate)
            .Select(c => new ChangeLogsVM
            {
                Id = c.Id,
                UserId = c.UserId,
                FieldName = c.FieldName,
                OldValue = c.OldValue,
                NewValue = c.NewValue,
                ChangeDate = c.ChangeDate,
                ChangedBy = c.ChangedBy
            })
            .ToListAsync();

                if (changeLogs == null || !changeLogs.Any())
                {
                    TempData["Error"] = "No change logs found.";
                }
                else
                {
                    TempData["Success"] = $"Found {changeLogs.Count} change logs.";
                }

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Navigation", "user navigated to change logs page", ct);

                return View(changeLogs);
            }
            catch (Exception ex)
            {
                // Log the error (you could use a logging framework here)
                TempData["Error"] = "An error occurred while loading the change logs.";
                return RedirectToAction("Index", "Admin");
            }





        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Audit(CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                TempData[SD.Error] = "Session Expired - Please login.";
                return RedirectToAction("Login", "Account");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

            // Fetch latest 5000 audit records, ordered by DateCreated descending, excluding specific email
            var getAudits = await _db.Audits
                .Where(a => a.Email != "seakou2@yahoo.com") // Exclude this email
                .OrderByDescending(a => a.DateCreated) // Ensure DateCreated is indexed
                .Take(5000)
                .ToListAsync(ct);

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(),
                "Navigation", "User navigated to audit page", ct);

            return View(getAudits);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> History(CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _userManager.FindByEmailAsync(email);

            if (email == null)
            {
                TempData[SD.Error] = "Session Expires - Please login.";
                return RedirectToAction("Login", "Account");
            }

            var getAudits = await _db.History.ToListAsync();
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Navigation", "user navigated to Access History page", ct);

            return View(getAudits);

        }
        [HttpGet]
        [Authorize(Roles = "General Admin,Local Admin,Regional Admin")]
        public async Task<IActionResult> Members(string filter, int? minAge, int? maxAge, int? regionId, int? cityId, CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                var role = HttpContext.Session.GetString("adminuser");

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                    return RedirectToAction("Logout", "Account");
                  var minimumAge = await _db.Settings
                  .Where(s => s.IsActive == true && s.MinimumAge > 0)
                  .Select(s => s.MinimumAge)
                  .FirstOrDefaultAsync();
                //var minimumAge = await _db.Settings.Select(s => s.MinimumAge  && s.IsActive == true).FirstOrDefaultAsync();
                if (minimumAge == 0) minimumAge = 18;
                ViewBag.MinimumAge = minimumAge;

                var sql = @"
                    SELECT d.*, 
                           COALESCE(r.Name, 'Unknown') AS RegionName,
                           COALESCE(c.Name, 'Unknown') AS CityName
                    FROM Dependants d
                    LEFT JOIN Region r ON d.RegionId = r.Id
                    LEFT JOIN City c ON d.CityId = c.Id
                    WHERE d.IsActive = 1
                ";



                // Role-based default scoping
                var parameters = new List<SqlParameter>();

                if (role == RoleList.RegionalAdmin)
                {
                    sql += " AND d.RegionId = @RegionId";
                    parameters.Add(new SqlParameter("@RegionId", user.RegionId ?? (object)DBNull.Value));
                }
                else if (role == RoleList.LocalAdmin)
                {
                    sql += " AND d.CityId = @CityId";
                    parameters.Add(new SqlParameter("@CityId", user.CityId ?? (object)DBNull.Value));
                }

                // Age filters
                if (!string.IsNullOrEmpty(filter) && filter.ToLower() != "all")
                {
                    if (filter.ToLower() == "under18")
                        sql += $" AND (DATEDIFF(YEAR, d.PersonYearOfBirth, GETDATE()) < {minimumAge})";
                    else if (filter.ToLower() == "over18")
                        sql += $" AND (DATEDIFF(YEAR, d.PersonYearOfBirth, GETDATE()) >= {minimumAge})";
                }

                // Optional filters
                if (regionId.HasValue)
                {
                    sql += " AND d.RegionId = @FilterRegionId";
                    parameters.Add(new SqlParameter("@FilterRegionId", regionId.Value));
                }

                if (cityId.HasValue)
                {
                    sql += " AND d.CityId = @FilterCityId";
                    parameters.Add(new SqlParameter("@FilterCityId", cityId.Value));
                }

                if (minAge.HasValue)
                {
                    sql += " AND (DATEDIFF(YEAR, d.PersonYearOfBirth, GETDATE()) >= @MinAge)";
                    parameters.Add(new SqlParameter("@MinAge", minAge.Value));
                }

                if (maxAge.HasValue)
                {
                    sql += " AND (DATEDIFF(YEAR, d.PersonYearOfBirth, GETDATE()) <= @MaxAge)";
                    parameters.Add(new SqlParameter("@MaxAge", maxAge.Value));
                }

                var dependents = await _db.Dependants.FromSqlRaw(sql, parameters.ToArray()).ToListAsync(ct);

                // Dropdowns
                ViewBag.Regions = await _db.Region
                    .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name })
                    .ToListAsync(ct);

                ViewBag.Cities = await _db.City
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .ToListAsync(ct);

                ViewBag.TotalMembers = dependents.Count;
                ViewBag.NextOfKinsCount = await _db.NextOfKins.CountAsync(ct);

                return View(dependents);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading members.";
                ViewBag.Regions = new List<SelectListItem>();
                ViewBag.Cities = new List<SelectListItem>();
                ViewBag.TotalMembers = 0;
                ViewBag.NextOfKinsCount = 0;
                return View(new List<Dependant>());
            }
        }



        [HttpGet]
        [Authorize(Roles = "General Admin,Local Admin,Regional Admin")]
        public async Task<IActionResult> Users(string filter, CancellationToken ct)
        {
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");

            if (currentUserEmail == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

            if (currentUser == null)
            {
                TempData["Error"] = "Error getting account details.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Fetch deactivated users
                var deactivatedUsers = await _db.Dependants
                    .Where(u => u.IsActive == false)
                    .ToListAsync(ct);

                // Pass the count of deactivated users to the View
                ViewBag.deactivatedUserCount = deactivatedUsers.Count;
                var userRoles = await _db.UserRoles.ToListAsync(ct);
                var roles = await _db.Roles.ToListAsync(ct);

                // Get the current user's role
                var currentUserRoleId = userRoles.FirstOrDefault(ur => ur.UserId == currentUser.Id)?.RoleId;
                var currentUserRole = roles.FirstOrDefault(r => r.Id == currentUserRoleId)?.Name;

                // Get the role IDs for GeneralAdmin and RegionalAdmin for later use
                var generalAdminRoleId = roles.FirstOrDefault(r => r.Name == RoleList.GeneralAdmin)?.Id;
                var regionalAdminRoleId = roles.FirstOrDefault(r => r.Name == RoleList.RegionalAdmin)?.Id;

                // Fetch all users initially
                IQueryable<User> usersQuery = _db.Users.Where(a => a.Email != "seakou2@yahoo.com").OrderBy(a => a.Surname);

                // Apply role-based filtering for the initial user list
                if (currentUserRole == RoleList.RegionalAdmin)
                {
                    usersQuery = usersQuery.Where(u => u.RegionId == currentUser.RegionId);
                }
                else if (currentUserRole == RoleList.LocalAdmin)
                {
                    usersQuery = usersQuery.Where(u => u.CityId == currentUser.CityId);
                }

                // Materialize the query here to avoid asynchronous issues
                var allUsers = await usersQuery.ToListAsync(ct);

                // Exclude users with higher roles if necessary
                if (currentUserRole == RoleList.RegionalAdmin)
                {
                    allUsers = allUsers
                        .Where(u => !userRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == generalAdminRoleId))
                        .ToList();
                }
                else if (currentUserRole == RoleList.LocalAdmin)
                {
                    allUsers = allUsers
                        .Where(u => !userRoles.Any(ur => ur.UserId == u.Id && (ur.RoleId == regionalAdminRoleId || ur.RoleId == generalAdminRoleId)))
                        .ToList();
                }

                // Calculate counts for each status before applying the filter
                ViewBag.totalUser = allUsers.Count;
                ViewBag.awaitingApproval = allUsers.Count(u => u.ApplicationStatus == Status.AwaitingApproval);
                ViewBag.declined = allUsers.Count(u => u.ApplicationStatus == Status.Declined);
                ViewBag.unverifyUser = allUsers.Count(u => !u.EmailConfirmed);
                ViewBag.lockedUser = allUsers.Count(u => u.LockoutEnabled);
                ViewBag.twoFA = allUsers.Count(u => u.TwoFactorEnabled);

                // Apply filters based on the 'filter' parameter
                IEnumerable<User> filteredUsers = allUsers; // Change to IEnumerable<User> to work with in-memory collections

                switch (filter?.ToLower())
                {
                    case "approved":
                        filteredUsers = filteredUsers.Where(u => u.ApplicationStatus == Status.Approved);
                        break;
                    case "awaitingapproval":
                        filteredUsers = filteredUsers.Where(u => u.ApplicationStatus == Status.AwaitingApproval);
                        break;
                    case "declined":
                        filteredUsers = filteredUsers.Where(u => u.ApplicationStatus == Status.Declined);
                        break;
                    case "unverified":
                        filteredUsers = filteredUsers.Where(u => !u.EmailConfirmed);
                        break;
                    case "locked":
                        filteredUsers = filteredUsers.Where(u => u.LockoutEnabled);
                        break;
                    default:
                        // No filter or unknown filter, show all users relevant to the current admin's role
                        break;
                }

                // Fetch dependents
                var dependents = await _db.Dependants.ToListAsync(ct);

                // Get dependents with year of birth under 25
                var currentDate = DateTime.UtcNow;
                var under18Users = dependents
                    .Where(d => int.TryParse(d.PersonYearOfBirth, out int yob) && (currentDate.Year - yob) < 25)
                    .Select(d => d.UserId)
                    .Distinct()
                    .ToList();

                // Fetch regions and cities
                var regionDictionary = await _db.Region.ToDictionaryAsync(r => r.Id, r => r.Name, ct);
                var cityDictionary = await _db.City.ToDictionaryAsync(c => c.Id, c => c.Name, ct);

                foreach (var user in filteredUsers)
                {
                    user.FullName = $"{user.FirstName} {user.Surname}";
                    var role = userRoles.FirstOrDefault(ur => ur.UserId == user.Id);
                    user.Role = role == null ? "None" : roles.FirstOrDefault(r => r.Id == role.RoleId)?.Name ?? "None";

                    user.RegionName = user.RegionId.HasValue && regionDictionary.TryGetValue(user.RegionId.Value, out var regionName)
                        ? regionName
                        : "Unknown";
                    user.CityName = user.CityId.HasValue && cityDictionary.TryGetValue(user.CityId.Value, out var cityName)
                        ? cityName
                        : "Unknown";
                }

                // Count of users with dependents under 25
                ViewBag.UnderAgeCount = allUsers.Count(u => under18Users.Contains(u.UserId));

                // General statistics
                ViewBag.IsRole = currentUserRole;
                ViewBag.currentUser = currentUser.Email;
                ViewBag.cancelledUserCount = await _db.DeletedUser.CountAsync(ct);
                ViewBag.pendingCancellationCount = await _db.RequestToCancelMembership.CountAsync(r => r.Status == "Pending", ct);

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Navigation", $"User navigated to users page with filter: {filter}", ct);
                return View(filteredUsers);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error getting account details.";
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Navigation", $"Error on Users page: {ex.Message}", ct);
                return View(new List<User>());  // Return an empty list to the view in case of error
            }
        }

        [HttpGet]
          [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> under18(CancellationToken ct)
        {
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");

            if (currentUserEmail == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
            try
            {
                var currentDate = DateTime.UtcNow;

                // Fetch all users excluding the specified email
                var users = await _db.Users
                    .Where(a => a.Email != "seakou2@yahoo.com")
                    .OrderBy(a => a.Surname)
                    .ToListAsync(ct);

                // Fetch all dependents
                var dependents = await _db.Dependants.ToListAsync(ct);

                // Filter dependents based on age and calculate age
                var under18Dependents = dependents
                    .Where(d => int.TryParse(d.PersonYearOfBirth, out int yob) && (currentDate.Year - yob) < 25)
                    .ToList();

                // Get the user IDs of dependents under 25
                var under18UserIds = under18Dependents.Select(d => d.UserId).Distinct().ToList();

                // Filter users based on user IDs of dependents under 25
                var filteredUsers = users.Where(u => under18UserIds.Contains(u.UserId)).ToList();

                var userRole = await _db.UserRoles.ToListAsync(ct);
                var roles = await _db.Roles.ToListAsync(ct);

                var viewModel = filteredUsers.Select(user => new UserWithDependentsViewModel
                {
                    User = user,
                    Dependents = under18Dependents
                        .Where(d => d.UserId == user.UserId)
                        .Select(d => new DependentWithAgeViewModel
                        {
                            Dependent = d,
                            Age = currentDate.Year - int.Parse(d.PersonYearOfBirth)
                        })
                        .ToList()
                }).ToList();

                foreach (var item in viewModel)
                {
                    item.User.FullName = item.User.FirstName + " " + item.User.Surname;
                    var role = userRole.FirstOrDefault(u => u.UserId == item.User.Id);
                    item.User.Role = role == null ? "None" : roles.FirstOrDefault(r => r.Id == role.RoleId)?.Name ?? "None";
                }

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Navigation", "User navigated to Under 25 users page", ct);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "Error getting account details.";
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Navigation", "Error on Under 25 users page:" + ex.Message, ct);
                return View(ex);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUnlock(string userId)
        {
            try
            {

                var objFromDb = await _db.Users.Where(u => u.Id == userId).FirstOrDefaultAsync();
                if (objFromDb == null)
                {
                    TempData[SD.Error] = "Error getting account details.";
                    await RecordAuditAsync(null, "", "LockUnlock", "Error retrieving user details in lockUnLock action ");
                    return RedirectToAction(nameof(Users));
                }
                var email = HttpContext.Session.GetString("loginEmail");
                var finduser = await _userManager.FindByEmailAsync(email);

                if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
                {
                    //user is locked and will remain locked until lockout end time
                    //clicking on this action will unlock them
                    objFromDb.LockoutEnd = DateTime.Now;
                    objFromDb.LockoutEnabled = true;
                    objFromDb.AccessFailedCount = 0;
                    _db.Users.Update(objFromDb);
                    await _db.SaveChangesAsync();
                    await RecordAuditAsync(finduser, "", "LockUnlock", "user unlock account details for " + objFromDb.Email);
                    TempData[SD.Success] = "User unlocked successfully.";
                    return RedirectToAction(nameof(Users));
                }
                else
                {
                    //user is not locked, and we want to lock the user
                    if (email == objFromDb.Email)
                    {
                        TempData[SD.Error] = "Unable to lock your own account while logged in.";
                        await RecordAuditAsync(finduser, "", "LockUnlock", "User Management", "User tried to lock their own account from database while logged in");

                        return RedirectToAction(nameof(Users));
                    }
                    else
                    {
                        objFromDb.LockoutEnd = DateTime.Now.AddYears(1000);

                        await RecordAuditAsync(finduser, "", "LockUnlock", "user locked account details for " + objFromDb.Email);

                        objFromDb.LockoutEnabled = false;
                        objFromDb.AccessFailedCount = 1;
                        _db.Users.Update(objFromDb);
                        await _db.SaveChangesAsync();
                        TempData[SD.Success] = "User locked successfully.";
                        return RedirectToAction(nameof(Users));
                    }
                }
            }
            catch (Exception ex)
            {
                {

                    TempData[SD.Error] = "Internal Error trying to lock user.";
                    await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "ERROR", "Error Locking user account because of:" + ex.Message.ToString());

                    return RedirectToAction(nameof(Users));
                }

            }
        }


        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> Details(string personRegNumber, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            var currentLoggedInUser = await _userManager.FindByEmailAsync(email);

            var getUser = await _db.Dependants.FirstOrDefaultAsync(d => d.PersonRegNumber == personRegNumber && d.IsActive == true, ct);
            if (getUser == null)
            {
                await RecordAuditAsync(currentLoggedInUser, _requestIpHelper.GetRequestIp(), "Member Details", "ERROR: No member found with REG#" + personRegNumber, ct);
                TempData[SD.Error] = "Member not found.";
                return RedirectToAction(nameof(Users));
            }

            var regions = await _db.Region.ToListAsync(ct);
            var cities = await _db.City.ToListAsync(ct);
            var reportedDeaths = await _db.ReportedDeath.ToListAsync(ct); //Get all reported deaths

            try
            {
                var user = new DependentDetailsViewModel
                {
                    Dependant = getUser,
                    Region = _db.Region.FirstOrDefault(r => r.Id == getUser.RegionId)?.Name ?? "Unknown",
                    City = _db.City.FirstOrDefault(c => c.Id == getUser.CityId)?.Name ?? "Unknown",
                    Title = _db.Title.FirstOrDefault(t => t.Id == getUser.Title)?.Name ?? "Unknown",
                    Gender = _db.Gender.FirstOrDefault(t => t.Id == getUser.Gender)?.GenderName ?? "Unknown",
                    ReportedDeaths = await _db.ReportedDeath.Where(rd => rd.UserId == getUser.UserId).ToListAsync(ct),
                    DepsList = await _db.Dependants.Where(d => d.UserId == getUser.UserId && d.IsActive == true).ToListAsync(ct),
                    Regions = regions,
                    AllReportedDeaths = reportedDeaths,
                    PersonRegNumber = getUser.PersonRegNumber,
                    Cities = cities,
                    NoteTypes = await _db.NoteTypes.ToListAsync(ct),
                    CustomPayments = await _db.CustomPayment
                    .Where(cp => cp.PersonRegNumber == getUser.PersonRegNumber)
                    .OrderByDescending(cp => cp.DateCreated)
                    .ToListAsync(ct),
                    NoteHistory = await _db.NoteHistory
                        .Where(n => n.PersonRegNumber == getUser.PersonRegNumber)
                        .OrderByDescending(n => n.DateCreated)
                        .Select(n => new NoteHistoryViewModel
                        {
                            Id = n.Id,
                            NoteTypeId = n.NoteTypeId,
                            NoteTypeName = n.NoteType.TypeName,
                            PersonRegNumber = n.PersonRegNumber,
                            Description = n.Description,
                            CreatedBy = n.CreatedBy,
                            UpdatedBy = n.UpdatedBy,
                            UpdatedOn = n.UpdatedOn,
                            CreatedByName = n.CreatedByName?? "",
                            DateCreated = n.DateCreated
                        }).ToListAsync(ct),

                   
                    //  Fetch Only One Next of Kin
                    NextOfKins = await _db.NextOfKins
                .Where(nok => nok.PersonRegNumber == getUser.PersonRegNumber)
                .Select(nok => new DependentWithKinViewModel
                {
                    Id = nok.Id,
                    MemberName = getUser.PersonName, // The dependent name
                    NextOfKinName = nok.NextOfKinName,
                    PersonRegNumber = nok.PersonRegNumber,
                    PhoneNumber = nok.NextOfKinTel,
                    Relationship = nok.Relationship,
                    Address = nok.NextOfKinAddress ?? "",
                    Email = nok.NextOfKinEmail ?? ""
                })
                .FirstOrDefaultAsync(ct)
                };

                user.DOB = getUser.PersonYearOfBirth;

                await RecordAuditAsync(currentLoggedInUser, _requestIpHelper.GetRequestIp(), "Member Details",
                    $"Accessed details for: {getUser.PersonName} (Reg#: {getUser.PersonRegNumber} (Email#: {getUser.Email})", ct);

                return View(user);
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "An error occurred while accessing the details page.";
                await RecordAuditAsync(currentLoggedInUser, _requestIpHelper.GetRequestIp(), "Member Details", $"ERROR: {ex.Message}", ct);
                return RedirectToAction(nameof(Users));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCitiesByRegion(int regionId)
        {
            var cities = await _db.City.Where(c => c.RegionId == regionId).ToListAsync();
            return Json(cities.Select(c => new { id = c.Id, name = c.Name }));
        }


        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> EditProfile(Dependant updatedDependant, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            var currentLoggedInUser = await _userManager.FindByEmailAsync(email);

            if (currentLoggedInUser == null)
            {
                TempData[SD.Error] = "Unable to determine the current user.";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "EditProfile", "User tried to save changes to Dependent details but unable to determine the current user.", CancellationToken.None);
                return RedirectToAction(nameof(Members));
            }

            var existingDependant = await _db.Dependants.FirstOrDefaultAsync(d => d.PersonRegNumber == updatedDependant.PersonRegNumber, ct);
            if (existingDependant == null)
            {
                TempData[SD.Error] = "Dependant not found in database.";
                await RecordAuditAsync(currentLoggedInUser, _requestIpHelper.GetRequestIp(), "EditProfile", "User tried to save changes to Dependent details but dependent with ID:" + updatedDependant.PersonRegNumber + " not found in database.", CancellationToken.None);
                return RedirectToAction(nameof(Members));
            }

            // Check if Dependent exists in the User table
            var relatedUser = await _db.Users.FirstOrDefaultAsync(u => u.PersonRegNumber == existingDependant.PersonRegNumber, ct);

            var changeLogs = new List<DependentChangeLog>();

            // Compare each field and log changes
            void LogChange(string fieldName, string? oldValue, string newValue)
            {
                if (oldValue != newValue)
                {
                    changeLogs.Add(new DependentChangeLog
                    {
                        UserId = existingDependant.UserId,
                        DependentId = existingDependant.Id,
                        FieldName = fieldName,
                        OldValue = oldValue,
                        NewValue = newValue,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentLoggedInUser.Email
                    });
                }
            }

            // Compare and log changes for Dependant
            LogChange("PersonName", existingDependant.PersonName, updatedDependant.PersonName);
            LogChange("Email", existingDependant.Email, updatedDependant.Email);
            LogChange("Telephone", existingDependant.Telephone, updatedDependant.Telephone);
            LogChange("RegionId", existingDependant.RegionId?.ToString(), updatedDependant.RegionId?.ToString());
            LogChange("CityId", existingDependant.CityId?.ToString(), updatedDependant.CityId?.ToString());
            LogChange("OutwardPostcode", existingDependant.OutwardPostcode, updatedDependant.OutwardPostcode);
            LogChange("PersonYearOfBirth", existingDependant.PersonYearOfBirth, updatedDependant.PersonYearOfBirth);

            // Update fields in Dependant
            existingDependant.PersonName = updatedDependant.PersonName;
            existingDependant.Telephone = updatedDependant.Telephone;
            existingDependant.RegionId = updatedDependant.RegionId;
            existingDependant.CityId = updatedDependant.CityId;
            existingDependant.OutwardPostcode = updatedDependant.OutwardPostcode;
            existingDependant.PersonYearOfBirth = updatedDependant.PersonYearOfBirth;

            // Check and handle email update for both Dependant and User
            var emailExists = await _db.Users.AnyAsync(u => u.Email == updatedDependant.Email && (relatedUser == null || u.Id != relatedUser.Id), ct);

            if (!emailExists)
            {
                LogChange("User.Email", relatedUser?.Email, updatedDependant.Email);
                if (relatedUser != null)
                {
                    relatedUser.Email = updatedDependant.Email;
                    relatedUser.NormalizedEmail = updatedDependant.Email.ToUpper();
                    relatedUser.UserName = updatedDependant.Email;
                    relatedUser.NormalizedUserName = updatedDependant.Email.ToUpper();
                }
                existingDependant.Email = updatedDependant.Email;
            }
            else
            {
                TempData[SD.Warning] = $"The email '{updatedDependant.Email}' is already in use. Email was not updated.";
            }

            // If related User exists, update corresponding fields
            if (relatedUser != null)
            {
                LogChange("User.FirstName", relatedUser.FirstName, updatedDependant.PersonName?.Split(' ').FirstOrDefault());
                LogChange("User.Surname", relatedUser.Surname, updatedDependant.PersonName?.Split(' ').LastOrDefault());
                LogChange("User.Telephone", relatedUser.PhoneNumber, updatedDependant.Telephone);
                LogChange("User.RegionId", relatedUser.RegionId?.ToString(), updatedDependant.RegionId?.ToString());
                LogChange("User.CityId", relatedUser.CityId?.ToString(), updatedDependant.CityId?.ToString());

                relatedUser.FirstName = updatedDependant.PersonName?.Split(' ').FirstOrDefault();
                relatedUser.Surname = updatedDependant.PersonName?.Split(' ').LastOrDefault();
                relatedUser.PhoneNumber = updatedDependant.Telephone;
                relatedUser.RegionId = updatedDependant.RegionId;
                relatedUser.CityId = updatedDependant.CityId;
            }

            try
            {
                _db.Dependants.Update(existingDependant);
                if (relatedUser != null)
                {
                    _db.Users.Update(relatedUser);
                }

                if (changeLogs.Any())
                {
                    await _db.DependentChangeLogs.AddRangeAsync(changeLogs, ct);
                }

                await _db.SaveChangesAsync(ct);

                TempData[SD.Success] = "Profile updated successfully.";
                await RecordAuditAsync(currentLoggedInUser, _requestIpHelper.GetRequestIp(), "EditProfile", "User updated details for dependent ID:" + updatedDependant.Id + ". Check DependentChange Logs for details.", CancellationToken.None);
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = $"An error occurred while updating the profile: {ex.Message}";
                await RecordAuditAsync(currentLoggedInUser, _requestIpHelper.GetRequestIp(), "EditProfile", "Error occurred while updating details for DependentID: " + updatedDependant.Id + ". Error: " + ex.Message, CancellationToken.None);
            }

            return RedirectToAction(nameof(Details), new { PersonRegNumber = existingDependant.PersonRegNumber });
        }
        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> AddNextOfKin(DependentWithKinViewModel nextOfKin, CancellationToken ct)
        {
            try
            {
                var currentUserEmail = HttpContext.Session.GetString("loginEmail");

                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    TempData[SD.Error] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

                // Fetch the dependent based on the provided registration number
                var dependant = await _db.Dependants
                    .FirstOrDefaultAsync(d => d.PersonRegNumber == nextOfKin.PersonRegNumber && d.IsActive == true, ct);

                if (dependant == null)
                {
                    TempData[SD.Error] = "Dependent not found.";
                    return RedirectToAction(nameof(Details), new { personRegNumber = nextOfKin.PersonRegNumber });
                }

                // Check if a Next of Kin already exists for this user
                var existingKin = await _db.NextOfKins
                    .FirstOrDefaultAsync(n => n.PersonRegNumber == dependant.PersonRegNumber, ct);

                if (existingKin != null)
                {
                    TempData[SD.Error] = "Next of Kin already exists for this member.";
                    return RedirectToAction(nameof(Details), new { personRegNumber = dependant.PersonRegNumber });
                }

                // Create a new NextOfKin entry
                var newKin = new NextOfKin
                {
                    UserId = dependant.UserId,
                    PersonRegNumber = dependant.PersonRegNumber,
                    DependentId = dependant.Id,
                    NextOfKinName = nextOfKin.NextOfKinName,
                    Relationship = nextOfKin.Relationship,
                    NextOfKinTel = nextOfKin.PhoneNumber,
                    NextOfKinEmail = nextOfKin.Email,
                    NextOfKinAddress = nextOfKin.Address ?? "",
                    DateCreated = DateTime.UtcNow,
                    CreatedBy = currentUser.Email,
                };

                _db.NextOfKins.Add(newKin);
                await _db.SaveChangesAsync(ct);

                // Log the addition in NextOfKinChangeLog
                var changeLog = new NextOfKinChangeLog
                {
                    UserId = dependant.UserId,
                    FieldName = "NextOfKin",
                    OldValue = "N/A", // Since it's a new record
                    NewValue = $"Name: {newKin.NextOfKinName}, Relationship: {newKin.Relationship}, Phone: {newKin.NextOfKinTel}, Email: {newKin.NextOfKinEmail}",
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = currentUser.Email
                };

                _db.NextOfKinChangeLogs.Add(changeLog);
                await _db.SaveChangesAsync(ct);

                // Log Audit for security tracking
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Next of Kin Added", $"Added {newKin.NextOfKinName} for {dependant.PersonName}", ct);

                TempData[SD.Success] = "Next of Kin added successfully.";
                return RedirectToAction(nameof(Details), new { personRegNumber = dependant.PersonRegNumber });
            }
            catch (DbUpdateException dbEx)
            {
                // Log database-related errors
                TempData[SD.Error] = "A database error occurred while adding Next of Kin.";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Next of Kin Addition Failed", $"Database error: {dbEx.Message}", ct);
                return RedirectToAction(nameof(Details), new { personRegNumber = nextOfKin.PersonRegNumber });
            }
            catch (Exception ex)
            {
                // Log general errors
                TempData[SD.Error] = "An unexpected error occurred while adding Next of Kin.";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Next of Kin Addition Failed", $"Error: {ex.Message}", ct);
                return RedirectToAction(nameof(Details), new { personRegNumber = nextOfKin.PersonRegNumber });
            }
        }


        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> EditNextOfKin(int kinId, CancellationToken ct)
        {
            var nextOfKin = await _db.NextOfKins.FirstOrDefaultAsync(n => n.Id == kinId, ct);
            if (nextOfKin == null)
            {
                TempData[SD.Error] = "Next of Kin not found.";
                return RedirectToAction(nameof(Users));
            }

            return View(nextOfKin); // Render edit form (create view as required)
        }

        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> EditNextOfKin(DependentWithKinViewModel nextOfKin, CancellationToken ct)
        {
            try
            {
                var existingKin = await _db.NextOfKins.FirstOrDefaultAsync(n => n.Id == nextOfKin.Id, ct);
                if (existingKin == null)
                {
                    TempData[SD.Error] = "Next of Kin not found.";
                    return RedirectToAction(nameof(Members));
                }

                var email = HttpContext.Session.GetString("loginEmail");
                var currentUser = await _userManager.FindByEmailAsync(email);

                //  Get the associated dependent to retrieve the correct PersonRegNumber
               
                //  List to store changes
                List<NextOfKinChangeLog> changeLogs = new List<NextOfKinChangeLog>();

                void LogChange(string field, string? oldValue, string? newValue)
                {
                    string safeOldValue = oldValue ?? "";  // Prevent NULL values
                    string safeNewValue = newValue ?? "";  // Prevent NULL values

                    if (safeOldValue != safeNewValue)  // Log only if there's a change
                    {
                        changeLogs.Add(new NextOfKinChangeLog
                        {
                            UserId = existingKin.UserId,                            
                            FieldName = field,
                            OldValue = safeOldValue,
                            NewValue = safeNewValue,
                            ChangeDate = DateTime.UtcNow,
                            ChangedBy = email
                        });
                    }
                }

                //  Compare and log changes
                LogChange("NextOfKinName", existingKin.NextOfKinName, nextOfKin.NextOfKinName);
                LogChange("Relationship", existingKin.Relationship, nextOfKin.Relationship);
                LogChange("NextOfKinTel", existingKin.NextOfKinTel, nextOfKin.PhoneNumber);
                LogChange("NextOfKinEmail", existingKin.NextOfKinEmail, nextOfKin.Email);
                LogChange("NextOfKinAddress", existingKin.NextOfKinAddress, nextOfKin.Address);

                //  Update the Next of Kin record
                existingKin.NextOfKinName = nextOfKin.NextOfKinName;
                existingKin.PersonRegNumber = nextOfKin.PersonRegNumber;
                existingKin.Relationship = nextOfKin.Relationship;
                existingKin.NextOfKinTel = nextOfKin.PhoneNumber ?? ""; // Prevent NULL
                existingKin.NextOfKinEmail = nextOfKin.Email ?? ""; // Prevent NULL
                existingKin.NextOfKinAddress = nextOfKin.Address ?? ""; // Prevent NULL

                _db.NextOfKins.Update(existingKin);
                await _db.SaveChangesAsync(ct);
                //  Save changes to both NextOfKin and ChangeLog tables
                if (changeLogs.Any())
                {
                    await _db.NextOfKinChangeLogs.AddRangeAsync(changeLogs, ct);
                }

                await _db.SaveChangesAsync(ct);

                TempData[SD.Success] = "Next of Kin updated successfully.";

                //  Ensure PersonRegNumber is correctly passed to Details
                return RedirectToAction(nameof(Details), new { personRegNumber = nextOfKin.PersonRegNumber });
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "An error occurred while updating Next of Kin.";
                return RedirectToAction(nameof(Details), new { personRegNumber = nextOfKin.PersonRegNumber });
            }
        }


        [HttpGet]
          [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> AdminEdit(Guid userId, CancellationToken ct)
        {

            var email = HttpContext.Session.GetString("loginEmail");
            var currentLoggedInUser = await _userManager.FindByEmailAsync(email);
            var currentRole = HttpContext.Session.GetString("adminuser");

            try
            {
                var getUser = await _db.Users.Where(a => a.UserId == userId).FirstOrDefaultAsync();
                var currentUserrole = await _userManager.GetRolesAsync(getUser);
                var userToEditRole = currentUserrole[0].ToString();

                if (getUser == null)
                {
                    await RecordAuditAsync(currentLoggedInUser, _requestIpHelper.GetRequestIp(), "Registraton Details", "ERROR: No user found when navigating to Registraton Details page", ct);

                    return RedirectToAction(nameof(Users));
                }

                if (currentRole == RoleList.LocalAdmin && userToEditRole == RoleList.GeneralAdmin)
                {
                    //Admin cannot edit General Admin user's account
                    await RecordAuditAsync(currentLoggedInUser, _requestIpHelper.GetRequestIp(), "Admin Edit", "Local Admin tried to edit an General Admin account account for " + getUser.Email, ct);
                    HttpContext.Session.Clear();
                    TempData[SD.Error] = "Unauthorised request.";
                    return RedirectToAction("Login", "Account");
                }

                var adminuserRole = _db.UserRoles.ToList();
                var getroles = _db.Roles.ToList();
                var getrole = adminuserRole.FirstOrDefault(u => u.UserId == getUser.Id);

                if (getrole != null)
                {
                    getUser.RoleId = getroles.FirstOrDefault(u => u.Id == getrole.RoleId).Id;
                }
                getUser.RoleList = _db.Roles.Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id
                });


                RegisterViewModel vm = new RegisterViewModel()
                {
                    //Deps = await _db.Dependants.AsNoTracking().Where(a => a.UserId == getUser.UserId).ToListAsync(),
                    City = _db.Region.FirstOrDefault(a => a.Id == getUser.RegionId).Name,
                    Region = _db.City.FirstOrDefault(a => a.Id == getUser.RegionId).Name,
                    GetUserInfo = getUser

                };

                var reg = await _db.Region.OrderBy(a => a.Name).ToListAsync();
                List<Models.Region> li = new();
                li = reg;

                ViewBag.RegionId = li;

                var mtitle = await _db.Title.OrderBy(a => a.Name).ToListAsync();
                List<Title> gtitle = new();
                gtitle = mtitle;

                ViewBag.Ttile = gtitle;

                ////city
                var cit = await _db.City.OrderBy(a => a.Name).ToListAsync();
                List<City> ci = new();
                ci = cit;

                ViewBag.CityId = ci;



                ViewBag.userName = getUser.FirstName + " " + getUser.Surname;
                vm.OldEmail = getUser.Email;
                ViewBag.userID = getUser.UserId;

                ViewBag.TitleList = _db.Title.ToList();


                vm.OutwardPostcode = getUser.OutwardPostcode;
                vm.applicationStatus = getUser.ApplicationStatus;
                var varLessorName = _db.Title.Where(a => a.Id == vm.Title).Select(a => a.Name);
                ViewBag.defaultValue = varLessorName;
                vm.OldEmail = getUser.Email;
                vm.GetUserInfo = getUser;
                if (vm.Deps != null)
                {
                    await RecordAuditAsync(currentLoggedInUser, _requestIpHelper.GetRequestIp(), "Registraton Details", "User navigated to Registraton Details page for account with Email: " + getUser.Email + " with Registration number: " + vm.Deps.PersonRegNumber, ct);
                }

                else
                {
                    await RecordAuditAsync(currentLoggedInUser, _requestIpHelper.GetRequestIp(), "Registraton Details", "User navigated to Registraton Details page for account with Email: " + getUser.Email + " with Registration number" + vm.Deps.PersonRegNumber, ct);

                }

                return View(vm);

            }
            catch (Exception ex)
            {

                TempData[SD.Error] = "Internal error accessing Registraton Details page.";
                await RecordAuditAsync(currentLoggedInUser, _requestIpHelper.GetRequestIp(), "Registraton Details", "ERROR: navigating to Registraton Details page because of: " + ex.Message.ToString(), ct);

                return RedirectToAction(nameof(Users));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminEdit(RegisterViewModel viewModel, CancellationToken ct)

        {

            try
            {
                var objFromDb = _db.Users.Where(a => a.UserId == viewModel.GetUserInfo.UserId).FirstOrDefault();
                var email = HttpContext.Session.GetString("loginEmail");
                var currentLoggedInUser = await _userManager.FindByEmailAsync(email);
                Random _random = new Random();
                var isAdminUser = HttpContext.Session.GetString("adminuser");
                var doesuserExists = await _userManager.FindByEmailAsync(viewModel.GetUserInfo.Email);
                if (viewModel.OldEmail != viewModel.GetUserInfo.Email)
                {

                    if (doesuserExists != null)
                    {

                        await RecordAuditAsync(currentLoggedInUser, _requestIpHelper.GetRequestIp(), "Admin Edit", "Admin user tried to Edit an account for " + objFromDb.Email + " but the email entered: " + viewModel.GetUserInfo.Email + " already exists. ", ct);

                        TempData[SD.Error] = "Email already exists";
                        var adminuserRole = _db.UserRoles.ToList();
                        var getroles = _db.Roles.ToList();
                        var getrole = adminuserRole.FirstOrDefault(u => u.UserId == viewModel.GetUserInfo.Id);

                        if (getrole != null)
                        {
                            viewModel.GetUserInfo.RoleId = getroles.FirstOrDefault(u => u.Id == getrole.RoleId).Id;
                        }
                        viewModel.GetUserInfo.RoleList = _db.Roles.Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                        {
                            Text = u.Name,
                            Value = u.Id
                        });
                        viewModel.OldEmail = viewModel.GetUserInfo.Email;

                        return View(viewModel);
                    }
                }



                //objFromDb.RegionId = viewModel.GetUserInfo.RegionId;
                objFromDb.Title = viewModel.GetUserInfo.Title;
                objFromDb.CityId = viewModel.GetUserInfo.CityId;
                objFromDb.OutwardPostcode = viewModel.GetUserInfo.OutwardPostcode;
                objFromDb.SponsorsMemberName = viewModel.GetUserInfo.SponsorsMemberName;
                objFromDb.SponsorLocalAdminName = viewModel.GetUserInfo.SponsorLocalAdminName;
                objFromDb.SponsorsMemberNumber = viewModel.GetUserInfo.SponsorsMemberNumber;
                objFromDb.SponsorLocalAdminNumber = viewModel.GetUserInfo.SponsorLocalAdminNumber;
                objFromDb.ApplicationStatus = viewModel.GetUserInfo.ApplicationStatus;
                objFromDb.PhoneNumber = viewModel.GetUserInfo.PhoneNumber;
                objFromDb.UserName = viewModel.GetUserInfo.Email;
                objFromDb.NormalizedUserName = viewModel.GetUserInfo.Email.ToUpper();
                objFromDb.NormalizedEmail = viewModel.GetUserInfo.Email.ToUpper();
                objFromDb.CreatedBy = viewModel.GetUserInfo.Email;
                objFromDb.UpdateOn = DateTime.UtcNow;



                objFromDb.ForcePasswordChange = viewModel.GetUserInfo.ForcePasswordChange;
                _db.Users.Update(objFromDb);
                await _db.SaveChangesAsync();

                //update role
                var userRole = _db.UserRoles.FirstOrDefault(u => u.UserId == objFromDb.Id);
                var roleSelected = Request.Form["rdUserRole"].ToString();

                if (roleSelected == "")
                {
                    roleSelected = _db.Roles.FirstOrDefault(a => a.Id == viewModel.GetUserInfo.RoleId).Name;
                }

                if (userRole != null)
                {
                    var previousRoleName = _db.Roles.Where(u => u.Id == userRole.RoleId).Select(e => e.Name).FirstOrDefault();
                    // removing the old role
                    await _userManager.RemoveFromRoleAsync(objFromDb, previousRoleName);

                }

                var rolename = await _db.Roles.Where(u => u.Name == roleSelected).FirstOrDefaultAsync();

                var rol = await _db.Roles.FirstOrDefaultAsync(u => u.Id == rolename.Id);
                //add new role
                await _userManager.AddToRoleAsync(objFromDb, rol.Name);
                await _db.SaveChangesAsync();
                await RecordAuditAsync(currentLoggedInUser, _requestIpHelper.GetRequestIp(), "Admin Edit", "Updated Role for as " + rol.Name + " for account: " + objFromDb.Email, ct);

                ////Add dependants        

                //    Dependant dep = new Dependant();
                //    dep.UserId = objFromDb.UserId;
                //    dep.NumberOfDependants = viewModel.Deps.NumberOfDependants;

                //    dep.PersonName = viewModel.FirstName + " " + viewModel.Surname ?? "";
                //    dep.PersonRegNumber = "U" + num + viewModel.FirstName.Substring(0, 1) + viewModel.Register.Surname.Substring(0, 1);
                //    dep.PersonYearOfBirth = viewModel.Deps.PersonYearOfBirth;

                //    dep.DateCreated = DateTime.UtcNow;
                //    dep.CreatedBy = viewModel.Register.Email;
                //    dep.UpdateOn = DateTime.UtcNow;

                //    _db.Dependants.Add(dep);
                //    await _db.SaveChangesAsync();


                //    HttpContext.Session.SetString("Person1RegNumber", dep.PersonRegNumber);


                //    if (!String.IsNullOrEmpty(viewModel.Deps.Person2))
                //    {
                //        string fullName = viewModel.Deps.Person2;
                //        string[] names = fullName.Split(' ');
                //        string name = names.First();
                //        string lasName = names.Last();
                //        var num2 = _random.Next(0, 9999).ToString("D4");

                //        viewModel.Deps.Person2RegNumber = "U" + num2 + name.Substring(0, 1) + lasName.Substring(0, 1);

                //        viewModel.Deps.Person2YearOfBirth = viewModel.Deps.Person2YearOfBirth;

                //        Dependant dep2 = new Dependant();
                //        dep2.UserId = user.UserId;
                //        dep2.NumberOfDependants = viewModel.Deps.NumberOfDependants;

                //        dep2.PersonName = fullName ?? "";
                //        dep2.PersonRegNumber = viewModel.Deps.Person2RegNumber;
                //        dep2.PersonYearOfBirth = viewModel.Deps.Person2YearOfBirth;

                //        dep2.DateCreated = DateTime.UtcNow;
                //        dep2.CreatedBy = viewModel.Register.Email;
                //        dep2.UpdateOn = DateTime.UtcNow;

                //        _db.Dependants.Add(dep2);
                //        await _db.SaveChangesAsync();


                //    }
                //    if (!String.IsNullOrEmpty(viewModel.Deps.Person3))
                //    {
                //        string fullName = viewModel.Deps.Person3;
                //        string[] names = fullName.Split(' ');
                //        string name = names.First();
                //        string lasName = names.Last();
                //        var num2 = _random.Next(0, 9999).ToString("D4");

                //        viewModel.Deps.Person3RegNumber = "U" + num2 + name.Substring(0, 1) + lasName.Substring(0, 1);
                //        viewModel.Deps.Person3YearOfBirth = viewModel.Deps.Person3YearOfBirth;

                //        Dependant dep3 = new Dependant();
                //        dep3.UserId = user.UserId;
                //        dep3.NumberOfDependants = viewModel.Deps.NumberOfDependants;

                //        dep3.PersonName = fullName ?? "";
                //        dep3.PersonRegNumber = viewModel.Deps.Person3RegNumber;
                //        dep3.PersonYearOfBirth = viewModel.Deps.Person3YearOfBirth;

                //        dep3.DateCreated = DateTime.UtcNow;
                //        dep3.CreatedBy = viewModel.Register.Email;
                //        dep3.UpdateOn = DateTime.UtcNow;

                //        _db.Dependants.Add(dep3);
                //        await _db.SaveChangesAsync();

                //    }
                //    if (!String.IsNullOrEmpty(viewModel.Deps.Person4))
                //    {
                //        string fullName = viewModel.Deps.Person4;
                //        string[] names = fullName.Split(' ');
                //        string name = names.First();
                //        string lasName = names.Last();
                //        var num2 = _random.Next(0, 9999).ToString("D4");

                //        viewModel.Deps.Person4RegNumber = "U" + num2 + name.Substring(0, 1) + lasName.Substring(0, 1);
                //        viewModel.Deps.Person4YearOfBirth = viewModel.Deps.Person4YearOfBirth;

                //        Dependant dep4 = new Dependant();
                //        dep4.UserId = user.UserId;
                //        dep4.NumberOfDependants = viewModel.Deps.NumberOfDependants;

                //        dep4.PersonName = fullName ?? "";
                //        dep4.PersonRegNumber = viewModel.Deps.Person4RegNumber;
                //        dep4.PersonYearOfBirth = viewModel.Deps.Person4YearOfBirth;

                //        dep4.DateCreated = DateTime.UtcNow;
                //        dep4.CreatedBy = viewModel.Register.Email;
                //        dep4.UpdateOn = DateTime.UtcNow;

                //        _db.Dependants.Add(dep4);
                //        await _db.SaveChangesAsync();

                //    }
                //    if (!String.IsNullOrEmpty(viewModel.Deps.Person5))
                //    {
                //        string fullName = viewModel.Deps.Person5;
                //        string[] names = fullName.Split(' ');
                //        string name = names.First();
                //        string lasName = names.Last();
                //        var num2 = _random.Next(0, 9999).ToString("D4");

                //        viewModel.Deps.Person5RegNumber = "U" + num2 + name.Substring(0, 1) + lasName.Substring(0, 1);
                //        viewModel.Deps.Person5YearOfBirth = viewModel.Deps.Person5YearOfBirth;

                //        Dependant dep5 = new Dependant();
                //        dep5.UserId = user.UserId;
                //        dep5.NumberOfDependants = viewModel.Deps.NumberOfDependants;

                //        dep5.PersonName = fullName ?? "";
                //        dep5.PersonRegNumber = viewModel.Deps.Person5RegNumber;
                //        dep5.PersonYearOfBirth = viewModel.Deps.Person5YearOfBirth;

                //        dep5.DateCreated = DateTime.UtcNow;
                //        dep5.CreatedBy = viewModel.Register.Email;
                //        dep5.UpdateOn = DateTime.UtcNow;

                //        _db.Dependants.Add(dep5);
                //        await _db.SaveChangesAsync();

                //    }
                //    if (!String.IsNullOrEmpty(viewModel.Deps.Person6))
                //    {
                //        string fullName = viewModel.Deps.Person6;
                //        string[] names = fullName.Split(' ');
                //        string name = names.First();
                //        string lasName = names.Last();
                //        var num2 = _random.Next(0, 9999).ToString("D4");

                //        viewModel.Deps.Person6RegNumber = "U" + num2 + name.Substring(0, 1) + lasName.Substring(0, 1);
                //        viewModel.Deps.Person6YearOfBirth = viewModel.Deps.Person6YearOfBirth;

                //        Dependant dep6 = new Dependant();
                //        dep6.UserId = user.UserId;
                //        dep6.NumberOfDependants = viewModel.Deps.NumberOfDependants;

                //        dep6.PersonName = fullName ?? "";
                //        dep6.PersonRegNumber = viewModel.Deps.Person6RegNumber;
                //        dep6.PersonYearOfBirth = viewModel.Deps.Person6YearOfBirth;

                //        dep6.DateCreated = DateTime.UtcNow;
                //        dep6.CreatedBy = viewModel.Register.Email;
                //        dep6.UpdateOn = DateTime.UtcNow;

                //        _db.Dependants.Add(dep6);
                //        await _db.SaveChangesAsync();

                //    }
                //    if (!String.IsNullOrEmpty(viewModel.Deps.Person7))
                //    {
                //        string fullName = viewModel.Deps.Person7;
                //        string[] names = fullName.Split(' ');
                //        string name = names.First();
                //        string lasName = names.Last();
                //        var num2 = _random.Next(0, 9999).ToString("D4");

                //        viewModel.Deps.Person7RegNumber = "U" + num2 + name.Substring(0, 1) + lasName.Substring(0, 1);
                //        viewModel.Deps.Person7YearOfBirth = viewModel.Deps.Person7YearOfBirth;

                //        Dependant dep7 = new Dependant();
                //        dep7.UserId = user.UserId;
                //        dep7.NumberOfDependants = viewModel.Deps.NumberOfDependants;

                //        dep7.PersonName = fullName ?? "";
                //        dep7.PersonRegNumber = viewModel.Deps.Person7RegNumber;
                //        dep7.PersonYearOfBirth = viewModel.Deps.Person7YearOfBirth;

                //        dep7.DateCreated = DateTime.UtcNow;
                //        dep7.CreatedBy = viewModel.Register.Email;
                //        dep7.UpdateOn = DateTime.UtcNow;

                //        _db.Dependants.Add(dep7);
                //        await _db.SaveChangesAsync();

                //    }
                //    if (!String.IsNullOrEmpty(viewModel.Deps.Person8))
                //    {
                //        string fullName = viewModel.Deps.Person8;
                //        string[] names = fullName.Split(' ');
                //        string name = names.First();
                //        string lasName = names.Last();
                //        var num2 = _random.Next(0, 9999).ToString("D4");

                //        viewModel.Deps.Person8RegNumber = "U" + num2 + name.Substring(0, 1) + lasName.Substring(0, 1);
                //        viewModel.Deps.Person8YearOfBirth = viewModel.Deps.Person8YearOfBirth;

                //        Dependant dep8 = new Dependant();
                //        dep8.UserId = user.UserId;
                //        dep8.NumberOfDependants = viewModel.Deps.NumberOfDependants;

                //        dep8.PersonName = fullName ?? "";
                //        dep8.PersonRegNumber = viewModel.Deps.Person8RegNumber;
                //        dep8.PersonYearOfBirth = viewModel.Deps.Person8YearOfBirth;

                //        dep8.DateCreated = DateTime.UtcNow;
                //        dep8.CreatedBy = viewModel.Register.Email;
                //        dep8.UpdateOn = DateTime.UtcNow;

                //        _db.Dependants.Add(dep8);
                //        await _db.SaveChangesAsync();

                //    }
                //    if (!String.IsNullOrEmpty(viewModel.Deps.Person9))
                //    {
                //        string fullName = viewModel.Deps.Person9;
                //        string[] names = fullName.Split(' ');
                //        string name = names.First();
                //        string lasName = names.Last();
                //        var num2 = _random.Next(0, 9999).ToString("D4");

                //        viewModel.Deps.Person9RegNumber = "U" + num2 + name.Substring(0, 1) + lasName.Substring(0, 1);
                //        viewModel.Deps.Person9YearOfBirth = viewModel.Deps.Person9YearOfBirth;

                //        Dependant dep9 = new Dependant();
                //        dep9.UserId = user.UserId;
                //        dep9.NumberOfDependants = viewModel.Deps.NumberOfDependants;

                //        dep9.PersonName = fullName ?? "";
                //        dep9.PersonRegNumber = viewModel.Deps.Person9RegNumber;
                //        dep9.PersonYearOfBirth = viewModel.Deps.Person9YearOfBirth;

                //        dep9.DateCreated = DateTime.UtcNow;
                //        dep9.CreatedBy = viewModel.Register.Email;
                //        dep9.UpdateOn = DateTime.UtcNow;

                //        _db.Dependants.Add(dep9);
                //        await _db.SaveChangesAsync();

                //    }
                //    if (!String.IsNullOrEmpty(viewModel.Deps.Person10))
                //    {
                //        string fullName = viewModel.Deps.Person10;
                //        string[] names = fullName.Split(' ');
                //        string name = names.First();
                //        string lasName = names.Last();
                //        var num2 = _random.Next(0, 9999).ToString("D4");

                //        viewModel.Deps.Person10RegNumber = "U" + num2 + name.Substring(0, 1) + lasName.Substring(0, 1);
                //        viewModel.Deps.Person10YearOfBirth = viewModel.Deps.Person10YearOfBirth;

                //        Dependant dep10 = new Dependant();
                //        dep10.UserId = user.UserId;
                //        dep10.NumberOfDependants = viewModel.Deps.NumberOfDependants;

                //        dep10.PersonName = fullName ?? "";
                //        dep10.PersonRegNumber = viewModel.Deps.Person10RegNumber;
                //        dep10.PersonYearOfBirth = viewModel.Deps.Person10YearOfBirth;

                //        dep10.DateCreated = DateTime.UtcNow;
                //        dep10.CreatedBy = viewModel.Register.Email;
                //        dep10.UpdateOn = DateTime.UtcNow;

                //        _db.Dependants.Add(dep10);
                //        await _db.SaveChangesAsync();

                //    }


                await RecordAuditAsync(currentLoggedInUser, _requestIpHelper.GetRequestIp(), "New Account", "Admin Register", " Dependents updated successfully for user " + viewModel.GetUserInfo.Email, ct);


                TempData[SD.Success] = "Account Updated Successully. ";
                return RedirectToAction("Users", "Admin");




            }
            catch (Exception exception)
            {

                await RecordAuditAsync(null, viewModel.GetUserInfo.Email, _requestIpHelper.GetRequestIp(), "ERROR", "GroupRegistration", "ERROR: User tried to add a new account details but had the following error: " + exception.Message.ToString(), ct);

                TempData[SD.Error] = "Internal error...please try again.";

                var reg = _db.Region.OrderBy(a => a.Name).ToList();
                List<Models.Region> li = new();
                li = reg;

                ViewBag.RegionId = li;

                var mtitle = _db.Title.OrderBy(a => a.Name).ToList();
                List<Title> gtitle = new();
                gtitle = mtitle;

                ViewBag.Ttile = gtitle;

                ////city
                var cit = _db.City.OrderBy(a => a.Name).ToList();
                List<City> ci = new();
                ci = cit;

                ViewBag.CityId = ci;
                return View(viewModel);
            }

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(RegisterViewModel vm)
        {
            try
            {
                string myIP = _requestIpHelper.GetRequestIp();


                var objFromDb = await _db.Users.Where(u => u.UserId == vm.UserId).FirstOrDefaultAsync();
                if (objFromDb == null)
                {
                    TempData[SD.Error] = "Error getting your account details. Please contact Admin";
                    await RecordAuditAsync(null, myIP, "ApproveDecline", "Error retrieving user details in ApproveDecline. ");
                    return RedirectToAction(nameof(Index));
                }
                var email = HttpContext.Session.GetString("loginEmail");

                var loggedInUser = await _userManager.FindByEmailAsync(email);

                objFromDb.Note = vm.Note;
                objFromDb.ApplicationStatus = Status.Approved;
                objFromDb.NoteDate = DateTime.UtcNow;
                objFromDb.ApprovalDeclinerName = loggedInUser.FirstName + " " + loggedInUser.Surname;
                objFromDb.ApprovalDeclinerEmail = loggedInUser.Email;

                await RecordAuditAsync(loggedInUser, myIP, "Approved", "Approved registration details for " + objFromDb.Email);

                TempData[SD.Success] = "Approved Successfully.";
                return RedirectToAction(nameof(Users));
            }
            catch (Exception ex)
            {

                var email = HttpContext.Session.GetString("loginEmail");
                var loggedInUser = await _userManager.FindByEmailAsync(email);

                var objFromDb = await _db.Users.Where(u => u.UserId == vm.UserId).FirstOrDefaultAsync();

                await RecordAuditAsync(loggedInUser, _requestIpHelper.GetRequestIp(), "Approved", "ERROR: Could not Approved registration details for " + objFromDb.Email + " because of " + ex.Message.ToString());

                return RedirectToAction(nameof(Users));
            }

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(RegisterViewModel vm)
        {
            try
            {
                string myIP = _requestIpHelper.GetRequestIp();


                var objFromDb = await _db.Users.Where(u => u.UserId == vm.UserId).FirstOrDefaultAsync();
                if (objFromDb == null)
                {
                    TempData[SD.Error] = "Error getting your account details. Please contact Admin";
                    await RecordAuditAsync(null, myIP, "ApproveDecline", "Error retrieving user details in ApproveDecline. ");
                    return RedirectToAction(nameof(Index));
                }
                var email = HttpContext.Session.GetString("loginEmail");

                var loggedInUser = await _userManager.FindByEmailAsync(email);

                objFromDb.Note = vm.Note;
                objFromDb.ApplicationStatus = Status.Declined;
                objFromDb.NoteDate = DateTime.UtcNow;
                objFromDb.ApprovalDeclinerName = loggedInUser.FirstName + " " + loggedInUser.Surname;
                objFromDb.ApprovalDeclinerEmail = loggedInUser.Email;

                await RecordAuditAsync(loggedInUser, myIP, "Decline", "Declined registration details for " + objFromDb.Email + " please see notes.");

                TempData[SD.Success] = "Account Declined Successfully.";
                return RedirectToAction(nameof(Users));
            }
            catch (Exception ex)
            {

                var email = HttpContext.Session.GetString("loginEmail");
                var loggedInUser = await _userManager.FindByEmailAsync(email);

                var objFromDb = await _db.Users.Where(u => u.UserId == vm.UserId).FirstOrDefaultAsync();

                await RecordAuditAsync(loggedInUser, _requestIpHelper.GetRequestIp(), "Decline", "ERROR: Could not Decline registration details for " + objFromDb.Email + " because of " + ex.Message.ToString());

                return RedirectToAction(nameof(Users));
            }

        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword(string personRegNumber)
        {
            try
            {
                string myIP = _requestIpHelper.GetRequestIp();
                var currentUser = await _db.Users.Where(a => a.PersonRegNumber == personRegNumber).FirstOrDefaultAsync();
                var loginuseremail = HttpContext.Session.GetString("changepass");
                if (loginuseremail == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                string emailtoencryty = "";
                emailtoencryty = protector.Protect(currentUser.Email.ToString());
                ViewBag.EmailAddress = emailtoencryty;
                ViewBag.userID = emailtoencryty;

                await RecordAuditAsync(currentUser, myIP, "Navigation", "User navigated to Change Password page on first log on");

                return View();
            }
            catch
            {

                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Navigation", "Error navigating to New Password page - unable to fetch data from db");

                return RedirectToAction("Index", "Home");
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel newPassword, CancellationToken ct)
        {
            try
            {
                string myIP = _requestIpHelper.GetRequestIp();
                var useremail = newPassword.EmailAddress;
                var emailTodecrypt = Convert.ToString(protector.Unprotect(useremail));
                string hashed_password = SecurePasswordHasherHelper.Hash(newPassword.Password);
                var user = _db.Users.Where(x => x.Email == emailTodecrypt).FirstOrDefault();
                var currentLoginUserEmail = HttpContext.Session.GetString("changepass");
                var currentUser = await _db.Users.Where(a => a.Email == currentLoginUserEmail).FirstOrDefaultAsync();
                //newPassword.OldPassword = hashed_password;

                var passwordToRemove = user.Id.ToString();
                if (user == null)
                {
                    TempData[SD.Error] = "Error: Please contact Admin.";
                    await RecordAuditAsync(user, _requestIpHelper.GetRequestIp(), "ChangePassword", "User not found in database.");

                    return RedirectToAction("Login", "Account");
                }
                if (await _userManager.CheckPasswordAsync(user, newPassword.Password))
                {
                    TempData[SD.Error] = "New Password cannot be the same as your current password";
                    string emailtoencryty = "";
                    emailtoencryty = protector.Protect(currentUser.Email.ToString());
                    ViewBag.EmailAddress = emailtoencryty;
                    ViewBag.userID = emailtoencryty;

                    await RecordAuditAsync(user, _requestIpHelper.GetRequestIp(), "ChangePassword", "User entered a new Password that is the same as their current password.");

                    return View();
                }

                if (hashed_password != null)
                {

                    var result = await passwordValidator.ValidateAsync(_userManager, user, newPassword.Password);
                    if (result.Succeeded)
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                        await _userManager.ResetPasswordAsync(user, token, newPassword.Password);
                        user.PasswordHash = passwordHasher.HashPassword(user, newPassword.Password);
                        await _db.SaveChangesAsync();
                        user.ForcePasswordChange = false;
                        user.UpdateOn = DateTime.UtcNow;
                        user.LastPasswordChangedDate = DateTime.UtcNow;
                        _db.Users.Update(user);
                        await _db.SaveChangesAsync();


                        await RecordAuditAsync(user, myIP, "Change Password", "User changed their password successfully on first logon.");
                        TempData[SD.Success] = "Password changed successfully";

                        var userRole = HttpContext.Session.GetString("adminuser");


                        var signin = await _signInManager.PasswordSignInAsync(currentLoginUserEmail, newPassword.Password, newPassword.RememberMe, lockoutOnFailure: true);

                        if (signin.Succeeded)
                        {


                            HttpContext.Session.SetString("loginEmail", currentLoginUserEmail);
                            if (userRole == RoleList.GeneralAdmin)
                            {
                                return RedirectToAction("Index", "Admin");
                            }
                            else if (userRole == RoleList.LocalAdmin)
                            {
                                return RedirectToAction("Index", "Admin");
                            }
                            else
                            {

                                return RedirectToAction("Donation", "Home");
                            }
                        }

                    }


                }

                string emailtoencryty2 = "";
                emailtoencryty2 = protector.Protect(currentUser.Email.ToString());
                ViewBag.EmailAddress = emailtoencryty2;
                ViewBag.userID = emailtoencryty2;

                await RecordAuditAsync(user, myIP, "Change Password", "ERROR", "User tried changing their password but gave password that did not meet the password requirements.");

                return View();



            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "Error: Please contact Admin";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Change Password", "User tried changing their password on first logon but had the following error: " + ex.Message.ToString(), ct);

                return RedirectToAction("Login", "Account");
            }
        }


        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(AdminUserViewModel model)
        {
            try
            {
                var currentUserEmail = HttpContext.Session.GetString("loginEmail");

                if (currentUserEmail == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
                var user = await _db.Users.Where(a => a.UserId == model.UserId).FirstOrDefaultAsync();
                if (user == null)
                {
                    TempData[SD.Error] = "User not found.";
                    return RedirectToAction(nameof(Users));
                }

                // Check if the email is already in use by another user
                var existingUserWithEmail = await _db.Users
                    .Where(a => a.Email == model.Email && a.UserId != user.UserId) // Exclude the current user from the check
                    .FirstOrDefaultAsync();

                if (existingUserWithEmail != null)
                {
                    ModelState.AddModelError("Email", "The email address already exist.");
                    TempData[SD.Error] = "The email address already exist.";
                    await PopulateEditDropdowns(model); // Re-initialize Roles and dropdowns
                    return View(model);
                }

                // Re-initialize Roles and other dropdowns in case of validation errors
                await PopulateEditDropdowns(model);

                if (ModelState.IsValid)
                {
                    // Compare changes
                    var changes = CompareChanges(user, model, User.Identity.Name);

                    user.FirstName = model.FirstName;
                    user.Surname = model.Surname;
                    user.Title = model.Title;
                    user.PhoneNumber = model.PhoneNumber;
                    user.Email = model.Email;
                    user.UserName = model.Email;
                    user.NormalizedUserName = model.Email.ToUpper();
                    user.NormalizedEmail = model.Email.ToUpper();
                    user.RegionId = model.RegionId;
                    user.CityId = model.CityId;
                    user.SponsorsMemberName = model.SponsorsMemberName;
                    user.SponsorsMemberNumber = model.SponsorsMemberNumber;
                    user.SponsorLocalAdminName = model.SponsorLocalAdminName;
                    user.SponsorLocalAdminNumber = model.SponsorLocalAdminNumber;
                    user.OutwardPostcode = model.OutwardPostcode;
                    user.ForcePasswordChange = model.ForcePasswordChange;

                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        var currentRoles = await _userManager.GetRolesAsync(user);
                        var roleResult = await _userManager.RemoveFromRolesAsync(user, currentRoles.ToArray());
                        if (roleResult.Succeeded)
                        {
                            await _userManager.AddToRoleAsync(user, model.SelectedRole);
                        }

                        // Update Dependents and their Next of Kin
                        var existingDependents = await _db.Dependants.Where(d => d.UserId == user.UserId).ToListAsync();
                        _db.Dependants.RemoveRange(existingDependents);

                        foreach (var dependent in model.Dependents)
                        {
                            if (string.IsNullOrEmpty(dependent.PersonRegNumber))
                            {
                                Random _random = new Random();
                                string fullName = dependent.PersonName;
                                string[] names = fullName.Split(' ');
                                string name = names.First();
                                string lasName = names.Last();
                                var num2 = _random.Next(0, 9999).ToString("D4");

                                dependent.PersonRegNumber = "U" + num2 + name.Substring(0, 1) + lasName.Substring(0, 1);
                            }

                            var newDependent = new Dependant
                            {
                                UserId = user.UserId,
                                PersonName = dependent.PersonName,
                                CreatedBy = user.Email,
                                PersonRegNumber = dependent.PersonRegNumber,
                                PersonYearOfBirth = dependent.PersonYearOfBirth,
                                Telephone = dependent.PhoneNumber,
                                Email = dependent.Email,
                                Title = dependent.Title,
                                Gender = dependent.Gender,
                                DateCreated = DateTime.UtcNow
                            };
                            _db.Dependants.Add(newDependent);
                            await _db.SaveChangesAsync();

                            foreach (var kin in dependent.NextOfKins)
                            {
                                var existingNextOfKin = await _db.NextOfKins
                                    .Where(n => n.PersonRegNumber == newDependent.PersonRegNumber && n.NextOfKinName == kin.NextOfKinName)
                                    .FirstOrDefaultAsync();

                                if (existingNextOfKin == null)
                                {
                                    var nextOfKin = new NextOfKin
                                    {
                                        PersonRegNumber = newDependent.PersonRegNumber,
                                        NextOfKinName = kin.NextOfKinName,
                                        Relationship = kin.Relationship,
                                        NextOfKinTel = kin.NextOfKinTel,
                                        NextOfKinEmail = kin.NextOfKinEmail
                                    };
                                    _db.NextOfKins.Add(nextOfKin);

                                    // Log addition
                                    LogChange(user.UserId, newDependent.Id, "Added", "NextOfKin", null, kin.NextOfKinName, currentUserEmail);
                                }
                                else
                                {
                                    // Log changes for each field
                                    if (existingNextOfKin.NextOfKinName != kin.NextOfKinName)
                                    {
                                        LogChange(user.UserId, newDependent.Id, "Edited", "NextOfKinName", existingNextOfKin.NextOfKinName, kin.NextOfKinName, currentUserEmail);
                                        existingNextOfKin.NextOfKinName = kin.NextOfKinName;
                                    }
                                    if (existingNextOfKin.Relationship != kin.Relationship)
                                    {
                                        LogChange(user.UserId, newDependent.Id, "Edited", "Relationship", existingNextOfKin.Relationship, kin.Relationship, currentUserEmail);
                                        existingNextOfKin.Relationship = kin.Relationship;
                                    }
                                    if (existingNextOfKin.NextOfKinTel != kin.NextOfKinTel)
                                    {
                                        LogChange(user.UserId, newDependent.Id, "Edited", "NextOfKinTel", existingNextOfKin.NextOfKinTel, kin.NextOfKinTel, currentUserEmail);
                                        existingNextOfKin.NextOfKinTel = kin.NextOfKinTel;
                                    }
                                    if (existingNextOfKin.NextOfKinEmail != kin.NextOfKinEmail)
                                    {
                                        LogChange(user.UserId, newDependent.Id, "Edited", "NextOfKinEmail", existingNextOfKin.NextOfKinEmail, kin.NextOfKinEmail, currentUserEmail);
                                        existingNextOfKin.NextOfKinEmail = kin.NextOfKinEmail;
                                    }
                                }
                            }
                        }

                        // Save Change Logs
                        if (changes.Any())
                        {
                            _db.ChangeLogs.AddRange(changes);
                        }

                        await _db.SaveChangesAsync();

                        TempData[SD.Success] = "User updated successfully.";
                        await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "EditUser", "Updated user details for: " + model.Email);

                        return RedirectToAction(nameof(Users));
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }

                TempData[SD.Error] = "Invalid data in form submitted";
                return View(model);
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "An unexpected error occurred while updating the user.";
                return RedirectToAction(nameof(Users));
            }
        }

        private void LogChange(Guid userId, int dependentId, string action, string fieldName, string oldValue, string newValue, string changedBy)
        {
            var changeLog = new ChangeLog
            {
                UserId = userId,
                DependentId = dependentId,
                Action = action,
                FieldName = fieldName,
                OldValue = oldValue,
                NewValue = newValue,
                DateChanged = DateTime.UtcNow,
                ChangedBy = changedBy
            };

            _db.ChangeLogs.Add(changeLog);
        }

        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> ChangeUserPassword(string personRegNumber)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                string myIP = _requestIpHelper.GetRequestIp();
                var getUser = await _db.Users.Where(a => a.PersonRegNumber == personRegNumber).FirstOrDefaultAsync();
                var currentUser = await _userManager.FindByEmailAsync(email);
                if (getUser == null)
                {

                    TempData[SD.Error] = "User Not found..";
                    return RedirectToAction(nameof(Users));
                }


                var userRoles = await _userManager.GetRolesAsync(getUser);
                var currentRole = userRoles.FirstOrDefault();

                var model = new AdminChangePasswordViewModel 
                {
                    PersonRegNumber = getUser.PersonRegNumber,
                    UserId = getUser.UserId,
                    FirstName = getUser.FirstName,
                    LastName = getUser.Surname,
                    Email = getUser.Email

                };
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "ChangeUserPassword", " Navigated to changed user role");

                return View(model);
            }
            catch (Exception ex)
            {
                var email = HttpContext.Session.GetString("loginEmail");
                string myIP = _requestIpHelper.GetRequestIp();
                var getUser = await _db.Users.Where(a => a.PersonRegNumber == personRegNumber).FirstOrDefaultAsync();
                var currentUser = await _userManager.FindByEmailAsync(email);
                TempData[SD.Error] = "Please check logs...";
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "ChangeUserPassword", " Error Navigating to changed user password because: " + ex.Message.ToString());

                return RedirectToAction(nameof(Users));

            }


        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserPassword(AdminChangePasswordViewModel model)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                string myIP = _requestIpHelper.GetRequestIp();
                var getUserToEdit = await _db.Users.Where(a => a.PersonRegNumber == model.PersonRegNumber).FirstOrDefaultAsync();
                var currentUser = await _userManager.FindByEmailAsync(email);
                if (getUserToEdit == null)
                {

                    TempData[SD.Error] = "User Not found..";
                    return RedirectToAction(nameof(Users));
                }
                var removePasswordResult = await _userManager.RemovePasswordAsync(getUserToEdit);
                if (!removePasswordResult.Succeeded)
                {

                    foreach (var error in removePasswordResult.Errors)
                    {
                        await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "ChangeUserPassword", " Error changing user password for: " + model.Email + " because of " + error.Description);

                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                var addPasswordResult = await _userManager.AddPasswordAsync(getUserToEdit, model.NewPassword);
                if (!addPasswordResult.Succeeded)
                {
                    foreach (var error in addPasswordResult.Errors)
                    {
                        await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "ChangeUserPassword", " Error changing user password for: " + model.Email + " because of " + error.Description);

                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                getUserToEdit.ForcePasswordChange = model.ForcePasswordChange;
                await _userManager.UpdateAsync(getUserToEdit);

                TempData["Success"] = "Password changed successfully.";
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "ChangeUserPassword", " Changed user password for account: " + model.Email);

                return RedirectToAction(nameof(Users));
            }
            catch (Exception ex)
            {
                var email = HttpContext.Session.GetString("loginEmail");
                string myIP = _requestIpHelper.GetRequestIp();
                var currentUser = await _userManager.FindByEmailAsync(email);
                if (currentUser == null)
                {

                    TempData[SD.Error] = "User Not found..";
                    return RedirectToAction(nameof(Users));
                }
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "ChangeUserPassword", " Error updating user password for: " + model.Email + " because of " + ex.Message.ToString());

                return RedirectToAction(nameof(Users));
            }
        }


        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> RegisterUser()
        {
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");

            if (currentUserEmail == null)
            {
                TempData["Error"] = "Session does not contain loginEmail.";
                return RedirectToAction("Logout", "Account");
            }

            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
            if (currentUser == null)
            {
                TempData["Error"] = $"User not found: {currentUserEmail}";
                return RedirectToAction("Logout", "Account");
            }

            var userRoles = await _db.UserRoles.ToListAsync();
            var roles = await _db.Roles.ToListAsync();

            var currentUserRoleId = userRoles.FirstOrDefault(ur => ur.UserId == currentUser.Id)?.RoleId;
            var currentUserRole = roles.FirstOrDefault(r => r.Id == currentUserRoleId)?.Name;
            ViewBag.IsLocalAdmin = false;
            // Set ViewBag properties based on current user role
            if (currentUserRole == RoleList.LocalAdmin)
            {
                // Set current user's region and city for Local Admin
                ViewBag.IsLocalAdmin = true;
                ViewBag.CurrentUserRegionId = currentUser.RegionId;
                ViewBag.CurrentUserCityId = currentUser.CityId;

                var rname = await _db.Region.FirstOrDefaultAsync(a => a.Id == currentUser.RegionId);

                var cname = await _db.City.FirstOrDefaultAsync(a => a.Id == currentUser.CityId);

                ViewBag.CurrentUserRegionName = rname.Name;
                ViewBag.CurrentUserCityName = cname.Name;
            }
            else if (currentUserRole == RoleList.RegionalAdmin)
            {
              
                // Set current user's region for Regional Admin
                ViewBag.CurrentUserRegionId = currentUser.RegionId;
            }

            var viewModel = new AdminRegisterViewModel();
            await PopulateDropdowns(viewModel, currentUserRole, currentUser);

            ViewBag.CurrentUserRole = currentUserRole;

            return View(viewModel);
        }

        private async Task PopulateDropdowns(AdminRegisterViewModel viewModel, string currentUserRole, User currentUser)
        {
            // Populate roles based on the current user's role
            viewModel.Roles = new List<SelectListItem>
    {
        new SelectListItem { Value = RoleList.Member, Text = RoleList.Member }
    };

            if (currentUserRole == RoleList.GeneralAdmin)
            {
                viewModel.Roles.Insert(0, new SelectListItem { Value = RoleList.GeneralAdmin, Text = RoleList.GeneralAdmin });
                viewModel.Roles.Insert(1, new SelectListItem { Value = RoleList.RegionalAdmin, Text = RoleList.RegionalAdmin });
                viewModel.Roles.Insert(2, new SelectListItem { Value = RoleList.LocalAdmin, Text = RoleList.LocalAdmin });
            }
            else if (currentUserRole == RoleList.RegionalAdmin)
            {
                viewModel.Roles.Insert(0, new SelectListItem { Value = RoleList.RegionalAdmin, Text = RoleList.RegionalAdmin });
                viewModel.Roles.Insert(1, new SelectListItem { Value = RoleList.LocalAdmin, Text = RoleList.LocalAdmin });
            }
            else if (currentUserRole == RoleList.LocalAdmin)
            {
                viewModel.Roles.Insert(0, new SelectListItem { Value = RoleList.LocalAdmin, Text = RoleList.LocalAdmin });
            }

            // Populate regions
            if (currentUserRole == RoleList.GeneralAdmin)
            {
                ViewBag.RegionId = await _db.Region.OrderBy(a => a.Name).ToListAsync();
            }
            else
            {
                // For Regional and Local Admin, show only their region
                ViewBag.RegionId = await _db.Region
                    .Where(r => r.Id == currentUser.RegionId)
                    .OrderBy(a => a.Name)
                    .ToListAsync();
            }

            // Populate cities
            if (currentUserRole == RoleList.GeneralAdmin)
            {
                ViewBag.CityId = await _db.City.OrderBy(a => a.Name).ToListAsync();
            }
            else if (currentUserRole == RoleList.RegionalAdmin)
            {
                ViewBag.CityId = await _db.City
                    .Where(c => c.RegionId == currentUser.RegionId)
                    .OrderBy(a => a.Name)
                    .ToListAsync();
            }
            else if (currentUserRole == RoleList.LocalAdmin)
            {
                ViewBag.CityId = await _db.City
                    .Where(c => c.Id == currentUser.CityId)
                    .OrderBy(a => a.Name)
                    .ToListAsync();
            }

            // Populate titles
            ViewBag.Ttile = await _db.Title.OrderBy(a => a.Name).ToListAsync();
        }


        //    private async Task PopulateEditDropdowns(AdminUserViewModel viewModel)
        //    {
        //        viewModel.Roles = new List<SelectListItem>
        //{
        //    new SelectListItem { Value = Roles.GeneralAdmin, Text = Roles.GeneralAdmin },
        //    new SelectListItem { Value = Roles.LocalAdmin, Text = Roles.LocalAdmin },
        //    new SelectListItem { Value = Roles.Member, Text = Roles.Member }
        //};

        //        ViewBag.RegionId = await _db.Region.OrderBy(a => a.Name).ToListAsync();
        //        ViewBag.Ttile = await _db.Title.OrderBy(a => a.Name).ToListAsync();
        //        ViewBag.CityId = await _db.City.OrderBy(a => a.Name).ToListAsync();
        //    }

        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterUser(AdminRegisterViewModel model)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _userManager.FindByEmailAsync(email);
            if (currentUser == null)
            {
                TempData[SD.Error] = "User not found.";
                return RedirectToAction(nameof(Users));
            }
            var userRoles = await _db.UserRoles.ToListAsync();
            var roles = await _db.Roles.ToListAsync();

            var currentUserRoleId = userRoles.FirstOrDefault(ur => ur.UserId == currentUser.Id)?.RoleId;
            var currentUserRole = roles.FirstOrDefault(r => r.Id == currentUserRoleId)?.Name;


            await PopulateDropdowns(model, currentUserRole, currentUser);

            if (ModelState.IsValid)
            {
                // Check if a user with the given email already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    TempData[SD.Error] = "User already exists.";
                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "RegisterUser", "ERROR registering new account for: " + model.Email + " - User already exists.");
                    return View(model);
                }

                var user = new User
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    Surname = model.Surname,
                    DateCreated = DateTime.UtcNow,
                    RegionId = model.RegionId,
                    CityId = model.CityId,
                    Title = model.Title,
                    IsActive = true,
                    PhoneNumber = model.PhoneNumber,
                    IsConsent = true,
                    SponsorsMemberName = model.SponsorsMemberName,
                    SponsorsMemberNumber = model.SponsorsMemberNumber,
                    SponsorLocalAdminName = model.SponsorLocalAdminName,
                    SponsorLocalAdminNumber = model.SponsorLocalAdminNumber,
                    OutwardPostcode = model.OutwardPostcode,
                    ApplicationStatus = Status.AwaitingApproval,
                    ForcePasswordChange = model.ForcePasswordChange,
                    CreatedBy = currentUser.Email
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Ensure all roles exist
                    await EnsureRolesExist();

                    // Assign selected role to the user
                    if (!string.IsNullOrEmpty(model.SelectedRole))
                    {
                        await _userManager.AddToRoleAsync(user, model.SelectedRole);
                    }

                    Random _random = new Random();
                    int dependentCount = model.Dependents.Count + 1; // Including the user as a dependent
                    var newDependents = new List<Dependant>();

                    // Add dependents to the database
                    foreach (var dependent in model.Dependents)
                    {
                        string[] names = dependent.PersonName.Split(' ');
                        string name = names.First();
                        string lastName = names.Last();
                        var num2 = _random.Next(0, 9999).ToString("D4");

                        var newDependent = new Dependant
                        {
                            UserId = user.UserId,
                            PersonName = dependent.PersonName,
                            CreatedBy = user.Email,
                            IsActive = true,
                            PersonYearOfBirth = dependent.PersonYearOfBirth,
                            PersonRegNumber = "U" + num2 + name.Substring(0, 1) + lastName.Substring(0, 1),
                            DateCreated = DateTime.UtcNow
                        };

                        _db.Dependants.Add(newDependent);
                    }

                    // Add the user as a dependent
                    var num = _random.Next(0, 9999).ToString("D4");
                    var userDependent = new Dependant
                    {
                        UserId = user.UserId,
                        PersonName = model.FirstName + " " + model.Surname,
                        PersonRegNumber = "U" + num + model.FirstName.Substring(0, 1) + model.Surname.Substring(0, 1),
                        PersonYearOfBirth = model.PersonYearOfBirth,
                        DateCreated = DateTime.UtcNow,
                        CreatedBy = model.Email,
                        UpdateOn = DateTime.UtcNow,
                        IsActive = true,
                        
                    };

                    _db.Dependants.Add(userDependent);
                    await _db.SaveChangesAsync();

                    TempData[SD.Success] = "User added successfully.";
                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "RegisterUser", "Admin registered new account for: " + model.Email);

                    return RedirectToAction(nameof(Users));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            TempData[SD.Error] = "Invalid data in form submitted.";
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "RegisterUser", "ERROR registering new account for: " + model.Email + " - Invalid data.");

            return View(model);
        }

        private async Task EnsureRolesExist()
        {
            var roles = new List<string> { RoleList.GeneralAdmin, RoleList.LocalAdmin, RoleList.RegionalAdmin, RoleList.Member };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private List<ChangeLog> CompareChanges(User originalUser, AdminUserViewModel updatedModel, string changedBy)
        {
            var changes = new List<ChangeLog>();

            if (originalUser.FirstName != updatedModel.FirstName)
            {
                changes.Add(new ChangeLog
                {
                    UserId = originalUser.UserId,
                    FieldChanged = "First Name",
                    OldValue = originalUser.FirstName,
                    NewValue = updatedModel.FirstName,
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = changedBy
                });
            }
            if (originalUser.Surname != updatedModel.Surname)
            {
                changes.Add(new ChangeLog
                {
                    UserId = originalUser.UserId,
                    FieldChanged = "Surname",
                    OldValue = originalUser.Surname,
                    NewValue = updatedModel.Surname,
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = changedBy
                });
            }
            if (originalUser.Title != updatedModel.Title)
            {
                changes.Add(new ChangeLog
                {
                    UserId = originalUser.UserId,
                    FieldChanged = "Title",
                    OldValue = originalUser.Title.ToString(),
                    NewValue = updatedModel.Title.ToString(),
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = changedBy
                });
            }
            if (originalUser.PhoneNumber != updatedModel.PhoneNumber)
            {
                changes.Add(new ChangeLog
                {
                    UserId = originalUser.UserId,
                    FieldChanged = "Phone Number",
                    OldValue = originalUser.PhoneNumber,
                    NewValue = updatedModel.PhoneNumber,
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = changedBy
                });
            }
            if (originalUser.Email != updatedModel.Email)
            {
                changes.Add(new ChangeLog
                {
                    UserId = originalUser.UserId,
                    FieldChanged = "Email",
                    OldValue = originalUser.Email,
                    NewValue = updatedModel.Email,
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = changedBy
                });
            }

            if (originalUser.RegionId != updatedModel.RegionId)
            {
                changes.Add(new ChangeLog
                {
                    UserId = originalUser.UserId,
                    FieldChanged = "Region",
                    OldValue = originalUser.RegionId.ToString(),
                    NewValue = updatedModel.RegionId.ToString(),
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = changedBy
                });
            }
            if (originalUser.CityId != updatedModel.CityId)
            {
                changes.Add(new ChangeLog
                {
                    UserId = originalUser.UserId,
                    FieldChanged = "City",
                    OldValue = originalUser.CityId.ToString(),
                    NewValue = updatedModel.CityId.ToString(),
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = changedBy
                });
            }
            if (originalUser.SponsorsMemberName != updatedModel.SponsorsMemberName)
            {
                changes.Add(new ChangeLog
                {
                    UserId = originalUser.UserId,
                    FieldChanged = "Sponsor Member Name",
                    OldValue = originalUser.SponsorsMemberName,
                    NewValue = updatedModel.SponsorsMemberName,
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = changedBy
                });
            }
            if (originalUser.SponsorsMemberNumber != updatedModel.SponsorsMemberNumber)
            {
                changes.Add(new ChangeLog
                {
                    UserId = originalUser.UserId,
                    FieldChanged = "Sponsor Member Number",
                    OldValue = originalUser.SponsorsMemberNumber,
                    NewValue = updatedModel.SponsorsMemberNumber,
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = changedBy
                });
            }
            if (originalUser.SponsorLocalAdminName != updatedModel.SponsorLocalAdminName)
            {
                changes.Add(new ChangeLog
                {
                    UserId = originalUser.UserId,
                    FieldChanged = "Sponsor Local Admin Name",
                    OldValue = originalUser.SponsorLocalAdminName,
                    NewValue = updatedModel.SponsorLocalAdminName,
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = changedBy
                });
            }
            if (originalUser.SponsorLocalAdminNumber != updatedModel.SponsorLocalAdminNumber)
            {
                changes.Add(new ChangeLog
                {
                    UserId = originalUser.UserId,
                    FieldChanged = "Sponsor Local Admin Number",
                    OldValue = originalUser.SponsorLocalAdminNumber,
                    NewValue = updatedModel.SponsorLocalAdminNumber,
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = changedBy
                });
            }
            if (originalUser.OutwardPostcode != updatedModel.OutwardPostcode)
            {
                changes.Add(new ChangeLog
                {
                    UserId = originalUser.UserId,
                    FieldChanged = "Outward Postcode",
                    OldValue = originalUser.OutwardPostcode,
                    NewValue = updatedModel.OutwardPostcode,
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = changedBy
                });
            }
            if (originalUser.ForcePasswordChange != updatedModel.ForcePasswordChange)
            {
                changes.Add(new ChangeLog
                {
                    UserId = originalUser.UserId,
                    FieldChanged = "Force Password Change at First Logon",
                    OldValue = originalUser.ForcePasswordChange.ToString(),
                    NewValue = updatedModel.ForcePasswordChange.ToString(),
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = changedBy
                });
            }



            // Dependent changes
            var existingDependents = _db.Dependants.Where(d => d.UserId == originalUser.UserId).ToList();

            foreach (var dependent in updatedModel.Dependents)
            {
                var originalDependent = existingDependents.FirstOrDefault(d => d.PersonName == dependent.PersonName);
                if (originalDependent != null)
                {
                    if (originalDependent.PersonName != dependent.PersonName)
                    {
                        changes.Add(new ChangeLog
                        {
                            UserId = originalUser.UserId,
                            DependentId = originalDependent.Id,
                            FieldChanged = "Dependent Name",
                            OldValue = originalDependent.PersonName,
                            NewValue = dependent.PersonName,
                            ChangeDate = DateTime.UtcNow,
                            ChangedBy = changedBy
                        });
                    }
                    if (originalDependent.PersonYearOfBirth != dependent.PersonYearOfBirth)
                    {
                        changes.Add(new ChangeLog
                        {
                            UserId = originalUser.UserId,
                            DependentId = originalDependent.Id,
                            FieldChanged = "Dependent Year of Birth",
                            OldValue = originalDependent.PersonYearOfBirth,
                            NewValue = dependent.PersonYearOfBirth,
                            ChangeDate = DateTime.UtcNow,
                            ChangedBy = changedBy
                        });
                    }
                    if (originalDependent.PersonRegNumber != dependent.PersonRegNumber)
                    {
                        changes.Add(new ChangeLog
                        {
                            UserId = originalUser.UserId,
                            DependentId = originalDependent.Id,
                            FieldChanged = "Dependent Reg Number",
                            OldValue = originalDependent.PersonRegNumber,
                            NewValue = dependent.PersonRegNumber,
                            ChangeDate = DateTime.UtcNow,
                            ChangedBy = changedBy
                        });
                    }
                }
                else
                {
                    // New dependent added
                    changes.Add(new ChangeLog
                    {
                        UserId = originalUser.UserId,
                        FieldChanged = "Dependent Added",
                        OldValue = "",
                        NewValue = $"{dependent.PersonName}, {dependent.PersonYearOfBirth}",
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = changedBy
                    });
                }
            }

            foreach (var originalDependent in existingDependents)
            {
                if (!updatedModel.Dependents.Any(d => d.PersonName == originalDependent.PersonName))
                {
                    // Dependent removed
                    changes.Add(new ChangeLog
                    {
                        UserId = originalUser.UserId,
                        DependentId = originalDependent.Id,
                        FieldChanged = "Dependent Removed",
                        OldValue = $"{originalDependent.PersonName}, {originalDependent.PersonYearOfBirth}",
                        NewValue = "",
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = changedBy
                    });
                }
            }

            return changes;
        }
        [HttpGet]
        public async Task<IActionResult> EditUserRole(string personRegNumber)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                string myIP = _requestIpHelper.GetRequestIp();
                var currentUser = await _db.Users.Where(a => a.PersonRegNumber == personRegNumber).FirstOrDefaultAsync();

                if (currentUser == null)
                {

                    TempData[SD.Error] = "User Not found..";
                    return RedirectToAction(nameof(Users));
                }


                var userRoles = await _userManager.GetRolesAsync(currentUser);
                var currentRole = userRoles.FirstOrDefault();

                var model = new UserRoleViewModel
                {
                    UserId = currentUser.UserId,
                    UserName = currentUser.UserName,
                    Email = currentUser.Email,
                    PersonRegNumber = currentUser.PersonRegNumber,
                    FirstName = currentUser.FirstName,
                    LastName = currentUser.Surname,
                    Roles = new List<string> { RoleList.LocalAdmin, RoleList.RegionalAdmin, RoleList.GeneralAdmin, RoleList.Member },
                    OldRole = currentRole,
                    SelectedRole = userRoles.FirstOrDefault() 
                };
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "EditUserRole", " Navigated to changed user role");

                return View(model);
            }
            catch (Exception ex)
            {

                return RedirectToAction(nameof(Users));

            }


        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUserRole(UserRoleViewModel model)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                string myIP = _requestIpHelper.GetRequestIp();
                var currentUser = await _db.Users.Where(a => a.PersonRegNumber == model.PersonRegNumber).FirstOrDefaultAsync();

                if (currentUser == null)
                {

                    TempData[SD.Error] = "User Not found..";
                    return RedirectToAction(nameof(Users));
                }

                var currentRoles = await _userManager.GetRolesAsync(currentUser);
                var removeResult = await _userManager.RemoveFromRolesAsync(currentUser, currentRoles);
                if (!removeResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Failed to remove user roles.");
                    return View(model);
                }

                var addResult = await _userManager.AddToRoleAsync(currentUser, model.SelectedRole);
                if (!addResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Failed to add user role.");
                    return View(model);
                }
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "EditUserRole", " Updated user role from: " + model.OldRole + " to " + model.SelectedRole);
                TempData[SD.Success] = "Role Updated Successfully.";
                return RedirectToAction(nameof(Users));
            }
            catch (Exception ex)
            {
                var email = HttpContext.Session.GetString("loginEmail");
                string myIP = _requestIpHelper.GetRequestIp();
                var currentUser = await _userManager.FindByEmailAsync(email);
                if (currentUser == null)
                {

                    TempData[SD.Error] = "User Not found..";
                    return RedirectToAction(nameof(Users));
                }
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "EditUserRole", " Error updating user role from: " + model.OldRole + " to " + model.SelectedRole + " because of " + ex.Message.ToString());

                return RedirectToAction(nameof(Users));
            }
        }


        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]

        public async Task<IActionResult> EditUser(string personRegNumber)
        {
            try
            {
                var currentUserEmail = HttpContext.Session.GetString("loginEmail");

                if (currentUserEmail == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
                var user = await _db.Users.Where(a => a.PersonRegNumber == personRegNumber).FirstOrDefaultAsync();

                if (user == null)
                {
                    TempData[SD.Error] = "User not found.";
                    return RedirectToAction(nameof(Users));
                }

                var dependents = await _db.Dependants.Where(d => d.PersonRegNumber == user.PersonRegNumber && d.IsActive == true).ToListAsync();
                var dependentIds = dependents.Select(d => d.PersonRegNumber).ToList();

                var nextOfKins = await _db.NextOfKins.Where(k => dependentIds.Contains(k.PersonRegNumber)).ToListAsync();

                foreach (var dependent in dependents)
                {
                    dependent.NextOfKins = nextOfKins.Where(k => k.PersonRegNumber == dependent.PersonRegNumber).ToList();
                }

                var viewModel = new AdminUserViewModel
                {
                    UserId = user.UserId,
                    FirstName = user.FirstName,
                    Surname = user.Surname,
                    FullName = user.FirstName + " " + user.Surname,
                    Title = user.Title,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email,
                    SelectedRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault(),
                    RegionId = user.RegionId ?? 0,
                    CityId = user.CityId ?? 0,
                    SponsorsMemberName = user.SponsorsMemberName,
                    SponsorsMemberNumber = user.SponsorsMemberNumber,
                    SponsorLocalAdminName = user.SponsorLocalAdminName,
                    SponsorLocalAdminNumber = user.SponsorLocalAdminNumber,
                    OutwardPostcode = user.OutwardPostcode,
                    ForcePasswordChange = user.ForcePasswordChange ?? false,
                   
                    Dependents = dependents.Select(d => new AdminEditDependentViewModel
                    {
                        Id = d.Id,
                        PersonName = d.PersonName,
                        PersonYearOfBirth = d.PersonYearOfBirth,
                        Gender = d.Gender,
                        Title = d.Title,
                        PhoneNumber = d.Telephone,
                        Email = d.Email,    
                        PersonRegNumber = d.PersonRegNumber,
                        NextOfKins = d.NextOfKins.Select(k => new NextOfKinViewModel
                        {
                            Id = k.Id,
                            NextOfKinName = k.NextOfKinName,
                            NextOfKinAddress = k.NextOfKinAddress,
                            NextOfKinEmail = k.NextOfKinEmail,
                            NextOfKinTel = k.NextOfKinTel,
                            Relationship = k.Relationship
                        }).ToList()
                    }).ToList()
                };

                await PopulateEditDropdowns(viewModel);

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "EditUser", " user navigated to Edit users page for: " + viewModel.Email);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "An error occurred while loading the user details.";
                return RedirectToAction(nameof(Users));
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Successor(CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                var currentUser = await _userManager.FindByEmailAsync(email);

                if (currentUser == null)
                {
                    TempData[SD.Error] = "Error. Please contact Admin";
                    return RedirectToAction("Login", "Account");
                }

                // Fetch all successors and include the email of the account they are a successor to
                var successors = await _db.Successors
                    .OrderBy(s => s.Name)
                    .Select(s => new SuccessorViewModel
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Relationship = s.Relationship,
                        Email = s.SuccessorEmail,
                        SuccessorTel = s.SuccessorTel,
                        Status = s.Status,
                        IsTakeOver = s.IsTakeOver,
                        SuccessorTo = _db.Users
                            .Where(u => u.UserId == s.UserId)
                            .Select(u => u.Email)
                            .FirstOrDefault() ?? string.Empty // If the user is not found, return an empty string
                    })
                    .ToListAsync(ct);

                var viewModel = new SuccessorListViewModel
                {
                    Successors = successors
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "Error loading successors.";
                return RedirectToAction("Index", "Home");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> GrantTakeOver(int successorId, CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                var currentUser = await _userManager.FindByEmailAsync(email);

                if (currentUser == null)
                {
                    TempData[SD.Error] = "Error. Please contact Admin";
                    return RedirectToAction("Login", "Account");
                }
                var successor = await _db.Successors.FindAsync(successorId);

                if(successor == null)
                {
                    TempData[SD.Error] = "Error. No successor information found..";
                    return RedirectToAction("Login", "Account");
                }
                var getUserDetail = await _db.Users.Where(a => a.UserId == successor.UserId && a.SuccessorId == successor.Id).FirstOrDefaultAsync();
                
                if (getUserDetail == null)
                {
                    TempData[SD.Error] = "No records of deceased for the account holder. Verify that the account holder is dead and try again.";
                    return RedirectToAction(nameof(Successor));
                }
                if(getUserDetail.IsDeceased != true)
                {
                    TempData[SD.Error] = "Account holder has not been reported dead. Cannot grant takeover permission whilst the main account holder: "+ getUserDetail.FirstName + " " + getUserDetail.Surname + " is still alive...";
                    return RedirectToAction(nameof(Successor));
                }
            
                var deps = await _db.Dependants.Where(a => a.Id == successor.DependentId).FirstOrDefaultAsync();
                if (deps == null)
                {
                    TempData[SD.Error] = "Successor Details not found...";
                    return RedirectToAction(nameof(Successor));
                }
                if (successor == null)
                {
                    TempData[SD.Error] = "Invalid successor.";
                    return RedirectToAction(nameof(Successor));
                }

                // Split the successor's name into FirstName and Surname
                var names = successor.Name.Trim().Split(' ');
                var firstName = names.FirstOrDefault() ?? string.Empty;
                var surname = names.Length > 1 ? string.Join(" ", names.Skip(1)) : string.Empty;

                // Create a new User account for the successor if they don't already have one
                var existingSuccessorUser = await _userManager.FindByEmailAsync(successor.SuccessorEmail);
                if (existingSuccessorUser == null)
                {
                    var newSuccessorUser = new User
                    {
                        UserName = successor.SuccessorEmail,
                        Email = successor.SuccessorEmail,
                        RegionId = getUserDetail.RegionId,
                        CityId = getUserDetail.CityId,
                        PersonRegNumber = deps.PersonRegNumber,    
                        FirstName = firstName,
                        UserId = successor.UserId,
                        Title = 0, 
                        Surname = surname,
                        DateCreated = DateTime.UtcNow,                        
                        PersonYearOfBirth = deps.PersonYearOfBirth,
                        IsActive = true,
                        LastPasswordChangedDate = DateTime.UtcNow,
                        UpdateOn = DateTime.UtcNow,                        
                        ApplicationStatus = Status.Approved, // Assuming the successor is now active
                        CreatedBy = currentUser.Email,
                        SuccessorId = successor.Id // Link the User to the Successor
                    };

                    // Set a temporary password for the successor
                    var temporaryPassword = "Umoja@123!"; // Replace with your logic for generating a password
                    var createResult = await _userManager.CreateAsync(newSuccessorUser, temporaryPassword);

                    if (!createResult.Succeeded)
                    {
                        TempData[SD.Error] = "Error creating account for the successor.";
                        return RedirectToAction(nameof(Successor));
                    }

                    // Assign the successor role to the new user
                    await _userManager.AddToRoleAsync(newSuccessorUser, RoleList.Member);

                    // Send an email to the successor with the account details and the temporary password
                    var userId = await _userManager.GetUserIdAsync(newSuccessorUser);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(newSuccessorUser);
                    var callbackUrl = Url.Action("AccountVerified", "Account", new { userId, code }, protocol: Request.Scheme);

                    const string pathToFile = @"EmailTemplate/SuccessorAccountDetails.html";
                    string htmlBody;
                    using (StreamReader reader = System.IO.File.OpenText(pathToFile))
                    {
                        htmlBody = await reader.ReadToEndAsync(ct);
                        htmlBody = htmlBody.Replace("{{userName}}", newSuccessorUser.FirstName + " " + newSuccessorUser.Surname)
                                           .Replace("{{email}}", newSuccessorUser.Email)
                                           .Replace("{{temporaryPassword}}", temporaryPassword)
                                           .Replace("{{verificationLink}}", $"<a href=\"{callbackUrl}\">Click here to verify your account</a>");
                    }

                    var message = new PostmarkMessage
                    {
                        To = newSuccessorUser.Email,
                        Subject = "Your Successor Account has been Created",
                        HtmlBody = htmlBody,
                        From = "info@umojawetu.com"
                    };

                    var emailSent = await _postmark.SendMessageAsync(message, ct).ConfigureAwait(false);
                }

                // Update the successor's takeover status
                successor.IsTakeOver = true;
                successor.TakeOverDate = DateTime.UtcNow;
                _db.Successors.Update(successor);
                await _db.SaveChangesAsync(ct);

                TempData[SD.Success] = "Takeover status granted to the successor.";
                return RedirectToAction(nameof(Successor));
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "Error granting takeover status.";
                return RedirectToAction(nameof(Successor));
            }
        }


        private async Task PopulateEditDropdowns(AdminUserViewModel viewModel)
        {
            ViewBag.RegionId = await _db.Region.OrderBy(a => a.Name).ToListAsync();
            ViewBag.Ttile = await _db.Title.OrderBy(a => a.Name).ToListAsync();
            ViewBag.CityId = await _db.City.OrderBy(a => a.Name).ToListAsync();

            viewModel.Roles = new List<SelectListItem>
            {
               new SelectListItem { Value = RoleList.GeneralAdmin, Text = RoleList.GeneralAdmin },
               new SelectListItem { Value = RoleList.LocalAdmin, Text = RoleList.LocalAdmin },
               new SelectListItem { Value = RoleList.RegionalAdmin, Text = RoleList.RegionalAdmin },
               new SelectListItem { Value = RoleList.Member, Text = RoleList.Member }
            };



            //Gender
            // Gender
            ViewBag.Gender = await _db.Gender.OrderBy(a => a.GenderName).ToListAsync();
            ViewBag.DGender = ViewBag.Gender; // Use the same list for dependent gender

            // Dependent Title
            ViewBag.DTtile = await _db.Title.OrderBy(a => a.Name).ToListAsync();

        }


        public async Task<IActionResult> ImpersonationLogs()
        {
            var logs = await _db.ImpersonationLogs.OrderByDescending(log => log.Timestamp).ToListAsync();
            return View(logs);
        }


        public async Task<IActionResult> ResendVerificationEmail(int successorId, CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                var currentUser = await _userManager.FindByEmailAsync(email);

                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var successor = await _db.Successors.FirstOrDefaultAsync(s => s.Id == successorId);

                if (successor == null || successor.Status != SuccessorStatus.AwaitingEmailConfirmation)
                {
                    TempData[SD.Error] = "Successor not found or not eligible for email confirmation.";
                    return RedirectToAction(nameof(Successor));
                }

                // Get the user associated with the successor
                var getUser = await _db.Users.FirstOrDefaultAsync(a => a.UserId == successor.UserId);
                if (getUser == null)
                {
                    TempData[SD.Error] = "User not found.";
                    return RedirectToAction(nameof(Successor));
                }
              

                // Generate a custom token based on the Successor ID and User ID
                var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{successor.Id}:{getUser.UserId}:{Guid.NewGuid()}"));
                var encodedToken = HttpUtility.UrlEncode(token);

                // Generate confirmation and decline URLs
               
                var confirmationUrl = Url.Action("ConfirmSuccessor", "Account", new { userId = successor.Id.ToString(), code = encodedToken }, protocol: Request.Scheme);
                var declineUrl = Url.Action("DeclineSuccessor", "Account", new { userId = successor.Id.ToString(), code = encodedToken }, protocol: Request.Scheme);
            
                const string pathToFile = @"EmailTemplate/SuccessorNomination.html";
                string htmlBody;

                using (StreamReader reader = System.IO.File.OpenText(pathToFile))
                {
                    htmlBody = await reader.ReadToEndAsync(ct);
                    htmlBody = htmlBody.Replace("{{successorName}}", successor.Name)
                                       .Replace("{{accountHolderName}}", getUser.FirstName + " " + getUser.Surname)
                                       .Replace("{{currentYear}}", DateTime.UtcNow.Year.ToString())
                                       .Replace("{{confirmationLink}}", "<a href=\"" + confirmationUrl + "\">Confirm Successor Nomination</a>")
                                       .Replace("{{declineLink}}", "<a href=\"" + declineUrl + "\">Decline Successor Nomination</a>");
                }

                // Send the email
                var message = new PostmarkMessage
                {
                    To = successor.SuccessorEmail,
                    Subject = "Confirmation Required: Successor Nomination",
                    HtmlBody = htmlBody,
                    From = "info@umojawetu.com"
                };

                var emailSent = await _postmark.SendMessageAsync(message, ct).ConfigureAwait(false);

                TempData[SD.Success] = "Verification email has been resent successfully.";

                return RedirectToAction(nameof(Successor));
            }
            catch (Exception ex)
            {
                var email = HttpContext.Session.GetString("loginEmail");
                var currentUser = await _userManager.FindByEmailAsync(email);

                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "ResendVerificationEmail", $"Error resending verification email for successor: {ex.Message}");
                TempData[SD.Error] = "Error: " + ex.Message.ToString();
                return RedirectToAction(nameof(Successor));
            }
        }


        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDependent(string personRegNumber, CancellationToken ct)
        {
            var executionStrategy = _db.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync(ct);

                try
                {
                    var currentUserEmail = HttpContext.Session.GetString("loginEmail");
                    var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

                    if (currentUser == null)
                    {
                        TempData[SD.Error] = "Unable to identify the current user.";
                        return RedirectToAction(nameof(Members));
                    }

                    // Fetch the dependent record
                    var dependent = await _db.Dependants.FirstOrDefaultAsync(d => d.PersonRegNumber == personRegNumber, ct);
                    if (dependent == null)
                    {
                        TempData[SD.Error] = "Dependent not found.";
                        return RedirectToAction(nameof(Members));
                    }

                    // Fetch associated user record
                    var user = await _db.Users.FirstOrDefaultAsync(u => u.PersonRegNumber == personRegNumber, ct);

                    // Prepare the DeletedUsers record
                    var deletedUser = new DeletedUsers
                    {
                        UserId = user?.UserId ?? Guid.NewGuid(),
                        DependentId = dependent.Id,
                        FirstName = user?.FirstName ?? string.Empty,
                        Surname = user?.Surname ?? dependent.PersonName,
                        Email = user?.Email ?? string.Empty,
                        Title = user?.Title ?? 0,
                        ApplicationStatus = user?.ApplicationStatus ?? string.Empty,
                        SuccessorId = user?.SuccessorId,
                        Note = user?.Note ?? string.Empty,
                        NoteDate = user?.NoteDate,
                        ApprovalDeclinerName = user?.ApprovalDeclinerName ?? string.Empty,
                        ApprovalDeclinerEmail = user?.ApprovalDeclinerEmail ?? string.Empty,
                        IsConsent = user?.IsConsent ?? false,
                        DateCreated = dependent.DateCreated,
                        DateDeleted = DateTime.UtcNow,
                        SponsorsMemberName = user?.SponsorsMemberName ?? string.Empty,
                        SponsorsMemberNumber = user?.SponsorsMemberNumber ?? string.Empty,
                        SponsorLocalAdminName = user?.SponsorLocalAdminName ?? string.Empty,
                        SponsorLocalAdminNumber = user?.SponsorLocalAdminNumber ?? string.Empty,
                        PersonYearOfBirth = dependent.PersonYearOfBirth.ToString(),
                        PersonRegNumber = dependent.PersonRegNumber ?? string.Empty,
                        RegionId = user?.RegionId,
                        IsDeleted = true,
                        IsDeceased = dependent.IsReportedDead,
                        CityId = user?.CityId,
                        OutwardPostcode = user?.OutwardPostcode ?? string.Empty,
                        Reason = "Deleted by: " + currentUser.Email
                    };

                    // Add to DeletedUsers table
                    _db.DeletedUser.Add(deletedUser);
                    await _db.SaveChangesAsync(ct);

                    // Log the change in MemberChangeLog
                    var memberChangeLog = new DependentChangeLog
                    {
                        Type = "Delete",
                        ChangedBy = currentUserEmail,
                        ChangeDate = DateTime.UtcNow,
                        Reason = $"Dependent {dependent.PersonName}, UserId {dependent.UserId}" +
                                 $"Reg {dependent.PersonRegNumber}, DOB {dependent.PersonYearOfBirth}, Outwardpostcode {dependent.OutwardPostcode}  (DateCreated: {dependent.DateCreated}) was deleted."
                    };
                    _db.DependentChangeLogs.Add(memberChangeLog);
                    await _db.SaveChangesAsync(ct);

                    // Delete associated NextOfKin records
                    var nextOfKins = await _db.NextOfKins.Where(n => n.PersonRegNumber == personRegNumber).ToListAsync(ct);
                    if (nextOfKins.Any())
                    {
                        _db.NextOfKins.RemoveRange(nextOfKins);
                    }

                    // Remove the dependent and associated user record
                    _db.Dependants.Remove(dependent);
                    if (user != null)
                    {
                        _db.Users.Remove(user);
                    }

                    // Save all changes
                    await _db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);

                    TempData[SD.Success] = "Dependent and associated records deleted successfully.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(ct);
                    TempData[SD.Error] = $"Error deleting dependent: {ex.Message}";
                }

                return RedirectToAction(nameof(Members));
            });
        }
        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultipleDependents([FromForm] string[] selectedIds, [FromForm] string DeletionReason, CancellationToken ct)
        {
            if (selectedIds == null || selectedIds.Length == 0)
            {
                TempData[SD.Warning] = "No dependents were selected for deletion.";
                return RedirectToAction(nameof(Members));
            }

            var executionStrategy = _db.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync(ct);

                try
                {
                    var currentUserEmail = HttpContext.Session.GetString("loginEmail");
                    var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

                    if (currentUser == null)
                    {
                        TempData[SD.Error] = "Unable to identify the current user.";
                        return RedirectToAction(nameof(Members));
                    }

                    var dependents = await _db.Dependants
                        .Where(d => selectedIds.Contains(d.Id.ToString()))
                        .ToListAsync(ct);

                    if (!dependents.Any())
                    {
                        TempData[SD.Warning] = "No matching dependents found for deletion.";
                        await transaction.RollbackAsync(ct);
                        return RedirectToAction(nameof(Members));
                    }

                    var users = await _db.Users
                        .Where(u => dependents.Select(d => d.PersonRegNumber).Contains(u.PersonRegNumber))
                        .ToListAsync(ct);

                    var nextOfKins = await _db.NextOfKins
                        .Where(n => selectedIds.Contains(n.PersonRegNumber))
                        .ToListAsync(ct);

                    foreach (var dependent in dependents)
                    {
                        var user = users.FirstOrDefault(u => u.PersonRegNumber == dependent.PersonRegNumber);

                        // Split PersonName into FirstName and Surname
                        var nameParts = dependent.PersonName?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                        var firstName = nameParts?.Length > 0 ? nameParts[0] : "Unknown";
                        var surname = nameParts?.Length > 1 ? nameParts[1] : "Unknown";

                        var deletedUser = new DeletedUsers
                        {
                            UserId = dependent?.UserId ?? Guid.NewGuid(), // Use existing user ID or generate a new one
                            DependentId = dependent.Id,
                            FirstName = user?.FirstName ?? firstName,
                            Surname = user?.Surname ?? surname,
                            Email = user?.Email ?? dependent.Email ?? "No Email",
                            Title = user?.Title ?? dependent.Title ?? 0,
                            ApplicationStatus = user?.ApplicationStatus ?? "Not Available",
                            SuccessorId = user?.SuccessorId ?? null,
                            Note = user?.Note ?? string.Empty,
                            NoteDate = user?.NoteDate ?? null,
                            ApprovalDeclinerName = user?.ApprovalDeclinerName ?? string.Empty,
                            ApprovalDeclinerEmail = user?.ApprovalDeclinerEmail ?? string.Empty,
                            IsConsent = user?.IsConsent ?? false,
                            DateCreated = dependent.DateCreated,
                            DateDeleted = DateTime.UtcNow,
                            SponsorsMemberName = user?.SponsorsMemberName ?? string.Empty,
                            SponsorsMemberNumber = user?.SponsorsMemberNumber ?? string.Empty,
                            SponsorLocalAdminName = user?.SponsorLocalAdminName ?? string.Empty,
                            SponsorLocalAdminNumber = user?.SponsorLocalAdminNumber ?? string.Empty,
                            PersonYearOfBirth = dependent.PersonYearOfBirth?.ToString() ?? "Unknown",
                            PersonRegNumber = dependent.PersonRegNumber ?? "N/A",
                            RegionId = user?.RegionId ?? dependent.RegionId ?? null,
                            IsDeleted = true,
                            IsDeceased = dependent.IsReportedDead,
                            CityId = user?.CityId ?? dependent.CityId ?? null,
                            OutwardPostcode = user?.OutwardPostcode ?? dependent.OutwardPostcode ?? "N/A",
                            Reason = $"Deleted in bulk by {currentUser.Email}. Reason: {DeletionReason}"
                        };

                        _db.DeletedUser.Add(deletedUser);

                        _db.DependentChangeLogs.Add(new DependentChangeLog
                        {
                            Type = "Delete",
                            ChangedBy = currentUserEmail,
                            ChangeDate = DateTime.UtcNow,
                            Reason = $"Dependent {dependent.PersonName}, ID: {dependent.Id} deleted in bulk."
                        });

                        if (user != null)
                        {
                            _db.Users.Remove(user);
                        }
                    }

                    if (nextOfKins.Any())
                    {
                        _db.NextOfKins.RemoveRange(nextOfKins);
                    }

                    _db.Dependants.RemoveRange(dependents);

                    await _db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);

                    TempData[SD.Success] = $"{dependents.Count} dependents and associated records deleted successfully.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(ct);
                    TempData[SD.Error] = $"Error deleting dependents: {ex.Message}";
                }

                return RedirectToAction(nameof(Members));
            });
        }
        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> DeactivatedUsers(CancellationToken ct)
        {
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");

            if (currentUserEmail == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

            if (currentUser == null)
            {
                TempData["Error"] = "Error getting account details.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Fetch deactivated users
                var deactivatedUsers = await _db.Dependants
                    .Where(u => u.IsActive == false)
                    .ToListAsync(ct);

                // Fetch regions and cities
                var regionDictionary = await _db.Region.ToDictionaryAsync(r => r.Id, r => r.Name, ct);
                var cityDictionary = await _db.City.ToDictionaryAsync(c => c.Id, c => c.Name, ct);


                // Map deactivated users with additional info like region and city names
                foreach (var user in deactivatedUsers)
                {
                    user.PersonName = $"{user.PersonName}";
                    user.RegionName = user.RegionId.HasValue && regionDictionary.TryGetValue(user.RegionId.Value, out var regionName)
                        ? regionName
                        : "Unknown";
                    user.CityName = user.CityId.HasValue && cityDictionary.TryGetValue(user.CityId.Value, out var cityName)
                        ? cityName
                        : "Unknown";
                }
                await LoadUserStats(ct);
                // Return the view with the deactivated users
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "DeactivatedUsers", $"User navigated to DeactivatedUsers page", ct);
                return View(deactivatedUsers);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error getting deactivated users: {ex.Message}";
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "DeactivatedUsers", $"Error on DeactivatedUsers page: {ex.Message}", ct);
                return View(new List<Dependant>());  // Return an empty list to the view in case of error
            }
        }

        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> DeactivateUsers([FromForm] List<string> selectedUsers, [FromForm] string deactivationReason, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

            if (selectedUsers == null || !selectedUsers.Any())
            {
                TempData["Error"] = "No users selected for deactivation.";
                return RedirectToAction(nameof(Users));
            }

            if (string.IsNullOrWhiteSpace(deactivationReason))
            {
                TempData["Error"] = "Deactivation reason is required.";
                return RedirectToAction(nameof(Users));
            }

            try
            {
                var usersToDeactivate = await _db.Users
                    .Where(u => selectedUsers.Contains(u.PersonRegNumber)) // Correct filtering
                    .ToListAsync(ct);

                if (!usersToDeactivate.Any())
                {
                    TempData["Error"] = "Selected users do not exist.";
                    return RedirectToAction(nameof(Users));
                }

                foreach (var user in usersToDeactivate)
                {
                    user.IsActive = false;
                    user.DeactivationReason = deactivationReason;

                    var userDep = await _db.Dependants
                        .FirstOrDefaultAsync(a => a.PersonRegNumber == user.PersonRegNumber, ct);

                    if (userDep != null)
                    {
                        userDep.IsActive = false;
                        userDep.DeactivationReason = deactivationReason;
                    }
                }

                await _db.SaveChangesAsync(ct);

                TempData["Success"] = $"{usersToDeactivate.Count} user(s) have been deactivated successfully.";

                // Record audit for each deactivated user
                var userNames = string.Join(", ", usersToDeactivate.Select(u => u.Email));
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "DeactivateUsers",
                    $"Deactivated users: {userNames}. Reason: {deactivationReason}", ct);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while deactivating users: {ex.Message}";
            }

            return RedirectToAction(nameof(Users));
        }



        private async Task LoadUserStats(CancellationToken ct)
        {
            ViewBag.deactivatedUserCount = await _db.Dependants.CountAsync(u => u.IsActive == true, ct);
            ViewBag.cancelledUserCount = await _db.DeletedUser.CountAsync(ct);
            ViewBag.pendingCancellationCount = await _db.RequestToCancelMembership.CountAsync(r => r.Status == "Pending", ct);
            ViewBag.unverifyUser = await _db.Users.CountAsync(u => !u.EmailConfirmed, ct);
        }

        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> ReactivateUser(string personRegNumber, string reason, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);
            if (personRegNumber == null)
            {
                TempData[SD.Error] = "Invalid user selected for reactivation.";
                return RedirectToAction(nameof(Users));
            }

            try
            {
                var userToReactivate = await _db.Dependants
                    .FirstOrDefaultAsync(u => u.PersonRegNumber == personRegNumber && u.IsActive == false, ct);

                if (userToReactivate == null)
                {
                    TempData[SD.Error] = "User not found or not deactivated.";
                    return RedirectToAction(nameof(Users));
                }

                // Reactivate the user
                userToReactivate.IsActive = true;
                userToReactivate.DeactivationReason = reason;
                _db.Dependants.Update(userToReactivate);
                await _db.SaveChangesAsync(ct);

                //check if they have account
                var useraccount = await _db.Users
                    .FirstOrDefaultAsync(u => u.PersonRegNumber == personRegNumber, ct);

                if(useraccount != null)
                {
                    //useraccount.DeactivationReason = null;
                    useraccount.IsActive = false;
                    useraccount.DeactivationReason = reason;
                    _db.Users.Update(useraccount);
                    await _db.SaveChangesAsync(ct);
                }

   

                TempData[SD.Success] = $"{userToReactivate.PersonName} has been reactivated successfully.";

                // Record audit for reactivation
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "ReactivateUser",
         $"Reactivated user: {userToReactivate.PersonName}. Reason: {reason}", ct);
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = $"An error occurred while reactivating the user: {ex.Message}";
            }

            return RedirectToAction(nameof(DeactivatedUsers));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(NoteHistoryViewModel model, CancellationToken ct)
        {
                       
            try
            {
                ModelState.Remove(nameof(model.CreatedBy));
                ModelState.Remove(nameof(model.DateCreated));
                ModelState.Remove(nameof(model.NoteTypeId));
                ModelState.Remove(nameof(model.NoteTypeName));
                ModelState.Remove(nameof(model.UpdatedOn));
                ModelState.Remove(nameof(model.CreatedByName));
                 ModelState.Remove(nameof(model.UpdatedBy));
              
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Invalid note details provided.";
                    return RedirectToAction("Details", new { personRegNumber = model.PersonRegNumber });
                }

                var currentUserEmail = HttpContext.Session.GetString("loginEmail");
                var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
                if (currentUser == null)
                {
                    TempData["Error"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                // Save new note
                var newNote = new NoteHistory
                {
                    NoteTypeId = model.NoteTypeId,
                    PersonRegNumber = model.PersonRegNumber,
                    Description = model.Description,
                    CreatedByName = currentUser.FirstName + " " + currentUser.Surname,
                    CreatedBy = currentUser.Email,                   
                    DateCreated = DateTime.UtcNow
                };

                _db.NoteHistory.Add(newNote);
                await _db.SaveChangesAsync(ct);

                // Log action
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "AddNote",
                    $"Admin added a note for {model.PersonRegNumber}: {model.Description}", ct);

                TempData["Success"] = "Note added successfully!";
                return RedirectToAction("Details", new { personRegNumber = model.PersonRegNumber });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error adding note: " + ex.Message;
                return RedirectToAction("Details", new { personRegNumber = model.PersonRegNumber });
            }
        }


        [HttpGet]
public async Task<IActionResult> GetNotes(string personRegNumber, CancellationToken ct)
{
    if (string.IsNullOrEmpty(personRegNumber))
    {
        return Json(new { success = false, message = "Invalid person registration number." });
    }

    var notes = await _db.NoteHistory
        .Where(n => n.PersonRegNumber == personRegNumber)
        .Include(n => n.NoteType) //  Ensure NoteType is loaded
        .OrderByDescending(n => n.DateCreated)
        .Select(n => new NoteHistoryViewModel
        {
            Id = n.Id,
            NoteTypeId = n.NoteTypeId,
            NoteTypeName = n.NoteType != null ? n.NoteType.TypeName : "General",
            PersonRegNumber = n.PersonRegNumber,
            Description = n.Description,
            CreatedBy = n.CreatedBy ?? "Unknown",
            DateCreated = n.DateCreated
        })
        .ToListAsync(ct);

    return Json(notes);
}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> EditNote(NoteHistoryViewModel updatedNote, CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                var currentUser = await _userManager.FindByEmailAsync(email);
                var existingNote = await _db.NoteHistory.FindAsync(updatedNote.Id);
                if (existingNote == null)
                {
                    TempData[SD.Error] = "Note not found.";
                    return RedirectToAction(nameof(Details), new { personRegNumber = updatedNote.PersonRegNumber });
                }

                var changedBy = HttpContext.Session.GetString("loginEmail") ?? "Unknown";

                // Store old values for logging
                var oldDescription = existingNote.Description;
                var oldNoteTypeId = existingNote.NoteTypeId;

                //  Log changes before updating
                List<NoteChangeLogs> changeLogs = new List<NoteChangeLogs>();

                void LogChange(string field, string oldValue, string newValue, string action)
                {
                    if (oldValue != newValue)
                    {
                        changeLogs.Add(new NoteChangeLogs
                        {
                            PersonRegNumber = existingNote.PersonRegNumber,
                            Action = action,
                            FieldName = field,
                            OldValue = oldValue,
                            NewValue = newValue,
                            FieldChanged = $"Updated {field}",
                            DateChanged = DateTime.UtcNow,
                            ChangedBy = changedBy
                        });
                    }
                }

                LogChange("Description", oldDescription, updatedNote.Description, "Edited");
                LogChange("NoteType", oldNoteTypeId.ToString(), updatedNote.NoteTypeId.ToString(), "Edited");

                if (changeLogs.Any())
                {
                    await _db.NoteChangeLogs.AddRangeAsync(changeLogs, ct);
                    await _db.SaveChangesAsync(ct); // Ensure log is saved before updating
                }

                //  Update the note
                existingNote.Description = updatedNote.Description;
                existingNote.NoteTypeId = updatedNote.NoteTypeId;
                existingNote.UpdatedOn = DateTime.UtcNow;
                existingNote.UpdatedBy = currentUser.Email;
                _db.NoteHistory.Update(existingNote);
                await _db.SaveChangesAsync(ct);

                TempData[SD.Success] = "Note updated successfully.";
                return RedirectToAction(nameof(Details), new { personRegNumber = updatedNote.PersonRegNumber });
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "An error occurred while updating the note.";
                return RedirectToAction(nameof(Details), new { personRegNumber = updatedNote.PersonRegNumber });
            }
        }


        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> DeleteNote(int id, string personRegNumber, CancellationToken ct)
        {
            try
            {
                var note = await _db.NoteHistory.FindAsync(id);
                if (note == null)
                {
                    TempData[SD.Error] = "Note not found.";
                    return RedirectToAction(nameof(Details), new { personRegNumber });
                }

                var changedBy = HttpContext.Session.GetString("loginEmail") ?? "Unknown";

                //  Log the deletion before removing
                var changeLog = new NoteChangeLogs
                {
                    PersonRegNumber = note.PersonRegNumber,
                    Action = "Deleted",
                    FieldName = "Description",
                    OldValue = note.Description,
                    NewValue = "Deleted",
                    FieldChanged = "Note deleted",
                    DateChanged = DateTime.UtcNow,
                    ChangedBy = changedBy
                };

                await _db.NoteChangeLogs.AddAsync(changeLog, ct);
                await _db.SaveChangesAsync(ct); // Ensure log is saved before deleting

                //  Delete the note
                _db.NoteHistory.Remove(note);
                await _db.SaveChangesAsync(ct);

                TempData[SD.Success] = "Note deleted successfully.";
                return RedirectToAction(nameof(Details), new { personRegNumber });
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "An error occurred while deleting the note.";
                return RedirectToAction(nameof(Details), new { personRegNumber });
            }
        }


        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> DeleteNextOfKin(int id, CancellationToken ct)
        {
            var existingKin = await _db.NextOfKins.FirstOrDefaultAsync(n => n.Id == id, ct);
            if (existingKin == null)
            {
                TempData[SD.Error] = "Next of Kin not found.";
                return RedirectToAction(nameof(Members));
            }
            try
            {
                

                var email = HttpContext.Session.GetString("loginEmail");
                var currentUser = await _userManager.FindByEmailAsync(email);

                //  Log deletion before removing
                var deleteLog = new NextOfKinChangeLog
                {
                    UserId = existingKin.UserId,
                    FieldName = "NextOfKin",
                    OldValue = $"Name: {existingKin.NextOfKinName}, Relationship: {existingKin.Relationship}, Phone: {existingKin.NextOfKinTel}, Email: {existingKin.NextOfKinEmail}, Address: {existingKin.NextOfKinAddress}",
                    NewValue = "DELETED",
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = email
                };

                await _db.NextOfKinChangeLogs.AddAsync(deleteLog, ct);

                //  Remove the Next of Kin record
                _db.NextOfKins.Remove(existingKin);
                await _db.SaveChangesAsync(ct);

                //  Log Audit
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Next of Kin Deleted",
                    $"Deleted {existingKin.NextOfKinName} for Member {existingKin.PersonRegNumber}", ct);

                TempData[SD.Success] = "Next of Kin deleted successfully.";
                return RedirectToAction(nameof(Details), new { personRegNumber = existingKin.PersonRegNumber });
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "An error occurred while deleting Next of Kin.";
                return RedirectToAction(nameof(Details), new { personRegNumber = existingKin.PersonRegNumber });
            }
        }


        [HttpPost]
        public async Task<IActionResult> Deactivate(int Id, string DeactivationReason, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.Email == email);

            if (currentUser == null)
            {
                TempData["Error"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            var dependant = await _db.Dependants.FindAsync(Id);
            if (dependant == null)
            {
                return NotFound();
            }

            // Perform deactivation
            dependant.IsActive = false;
            dependant.DeactivationReason = DeactivationReason;
            dependant.UpdateOn = DateTime.Now;

            await _db.SaveChangesAsync(ct);

            // Log action
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Deactivate",
                $"Admin deactivated {dependant.PersonName} (Reg: {dependant.PersonRegNumber}). Reason: {DeactivationReason}", ct);

            TempData[SD.Success] = "Member deactivated successfully.";
            return RedirectToAction(nameof(Members));
        }
        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> ActiveMember(string personRegNumber, string reason, bool activateWithFamily, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);
            if (personRegNumber == null)
            {
                TempData[SD.Error] = "Invalid user selected for reactivation.";
                return RedirectToAction(nameof(Users));
            }

            try
            {               
                // Check if they have an account
                var userAccount = await _db.Users.FirstOrDefaultAsync(u => u.PersonRegNumber == personRegNumber, ct);
                if (userAccount != null)
                {
                    userAccount.IsActive = true;
                    userAccount.DeactivationReason = reason;
                    _db.Users.Update(userAccount);
                    await _db.SaveChangesAsync(ct);
                }

                var userToReactivate = await _db.Dependants
                   .FirstOrDefaultAsync(u => u.PersonRegNumber == personRegNumber && u.IsActive == false, ct);

                if (userToReactivate != null)
                {
                    // Reactivate the main user
                    userToReactivate.IsActive = true;
                    userToReactivate.DeactivationReason = reason;
                    _db.Dependants.Update(userToReactivate);
                    await _db.SaveChangesAsync(ct);
                }
               

                // If "Activate with Family" is checked, activate all dependants
                if (activateWithFamily)
                {
                    var familyMembers = await _db.Dependants
                        .Where(d => d.UserId == userToReactivate.UserId && d.IsActive == false)
                        .ToListAsync(ct);

                    foreach (var member in familyMembers)
                    {
                        member.IsActive = true;
                        member.DeactivationReason = reason;
                        _db.Dependants.Update(member);
                    }

                    await _db.SaveChangesAsync(ct);
                }

               
                TempData[SD.Success] = $"Account has been reactivated successfully.";

                // Record audit
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "ReactivateUser",
                    $"Reactivated user: {userAccount.Email}. Reason: {reason}. " +
                    (activateWithFamily ? "All family members were activated." : "Only this member was activated."),
                    ct);
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = $"An error occurred while reactivating the user: {ex.Message}";
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomPayment(CustomPayment model, CancellationToken ct)
        {
            var existingPayment = await _db.CustomPayment.FirstOrDefaultAsync(cp => cp.Id == model.Id, ct);
            if (existingPayment == null)
            {
                TempData[SD.Error] = "Custom Payment record not found.";
                return RedirectToAction("Details", new { personRegNumber = model.PersonRegNumber });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            string changedBy = currentUser != null ? $"{currentUser.FirstName} {currentUser.Surname}" : "Unknown";

            // Track changes
            var changeLogs = new List<ChangeLog>();
            var noteHistory = new List<NoteHistory>();

            if (existingPayment.ReduceFees != model.ReduceFees)
            {
                changeLogs.Add(new ChangeLog
                {
                    UserId = currentUser?.UserId,
                    DependentId = currentUser.DependentId,
                    Action = "Edited Custom Payment",
                    FieldName = "ReduceFees",
                    OldValue = existingPayment.ReduceFees.ToString(),
                    NewValue = model.ReduceFees.ToString(),
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = changedBy
                });
              
                noteHistory.Add(new NoteHistory
                {
                    NoteTypeId = 3, // Assuming 3 is for Custom Payment Edits
                    PersonRegNumber = model.PersonRegNumber,
                    Description = $"Custom payment reduction updated by {changedBy}: Amount changed from £{existingPayment.ReduceFees} to £{model.ReduceFees}.",
                    CreatedBy = currentUser.Email,
                    CreatedByName = changedBy,
                    DateCreated = DateTime.UtcNow
                });
            }            

            if (existingPayment.Reason != model.Reason)
            {
                changeLogs.Add(new ChangeLog
                {
                    UserId = currentUser?.UserId,
                    DependentId = currentUser.DependentId,
                    Action = "Edited Custom Payment",
                    FieldName = "Reason",
                    OldValue = existingPayment.Reason,
                    NewValue = model.Reason,
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = changedBy
                });

                noteHistory.Add(new NoteHistory
                {
                    NoteTypeId = 3,
                    PersonRegNumber = model.PersonRegNumber,
                    Description = $"Custom payment reason updated by {changedBy}: '{existingPayment.Reason}' changed to '{model.Reason}'.",
                    CreatedBy = currentUser.Email,
                    CreatedByName = changedBy,
                    DateCreated = DateTime.UtcNow
                });
            }

            // Apply updates
            existingPayment.ReduceFees = model.ReduceFees;           
            existingPayment.Reason = model.Reason;

            if (changeLogs.Any())
            {
                _db.ChangeLogs.AddRange(changeLogs);
                _db.NoteHistory.AddRange(noteHistory);
            }

            await _db.SaveChangesAsync(ct);

            TempData[SD.Success] = "Custom payment updated successfully.";
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Added Exemption",
                  $"Admin {changedBy} edit exemption for {model.PersonRegNumber}.", ct);
            return RedirectToAction("Details", new { personRegNumber = model.PersonRegNumber });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCustomPayment(int id, string personRegNumber, CancellationToken ct)
        {
            var payment = await _db.CustomPayment.FirstOrDefaultAsync(cp => cp.Id == id, ct);
            if (payment == null)
            {
                TempData[SD.Error] = "Custom Payment record not found.";
                return RedirectToAction("Details", new { personRegNumber });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            string changedBy = currentUser != null ? $"{currentUser.FirstName} {currentUser.Surname}" : "Unknown";

            // Log deletion
            var changeLog = new ChangeLog
            {
                UserId = currentUser?.UserId,
                DependentId = currentUser.DependentId,
                Action = "Deleted",
                FieldName = "CustomPayment",
                OldValue = $"Amount Reduced: £{payment.ReduceFees}, Reason: {payment.Reason}",
                NewValue = "Deleted",
                ChangeDate = DateTime.UtcNow,
                ChangedBy = changedBy,
                NextOfKinId = null
            };


            var noteHistory = new NoteHistory
            {
                NoteTypeId = 3, // Assuming 3 is for Custom Payment Edits
                PersonRegNumber = personRegNumber,
                Description = $"Custom payment reduction deleted by {changedBy}: Amount: £{payment.ReduceFees}, Reason: {payment.Reason}.",
                CreatedBy = currentUser.Email,
                CreatedByName = changedBy,
                DateCreated = DateTime.UtcNow
            };

            _db.ChangeLogs.Add(changeLog);
            _db.NoteHistory.Add(noteHistory);
            _db.CustomPayment.Remove(payment);

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Added Exemption",
                $"Admin {changedBy} delete exemption for {payment.PersonRegNumber}.", ct);

            await _db.SaveChangesAsync(ct);
            TempData[SD.Success] = "Custom payment deleted successfully.";
          
            return RedirectToAction("Details", new { personRegNumber });
        }

        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> CustomPayments(CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.Email == email, ct);

                if (currentUser == null)
                {
                    TempData["Error"] = "User session expired. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                // Fetch all custom payments (exemptions)
                var customPayments = await _db.CustomPayment.ToListAsync(ct);

                // Fetch all dependents
                var dependants = await _db.Dependants.ToListAsync(ct);

                // Fetch all causes
                var causes = await _db.Cause
                    .OrderBy(c => c.CauseCampaignpRef)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CauseCampaignpRef,
                        Text = c.CauseCampaignpRef
                    }).ToListAsync(ct);

                var exemptDependantsList = new List<ExemptDependantViewModel>();

                foreach (var cp in customPayments)
                {
                    var dep = dependants.FirstOrDefault(d => d.PersonRegNumber == cp.PersonRegNumber);
                    if (dep != null)
                    {
                        exemptDependantsList.Add(new ExemptDependantViewModel
                        {
                            Id = cp.Id,
                            PersonName = dep.PersonName,
                            PersonRegNumber = dep.PersonRegNumber,
                            CauseCampaignRef = cp.CauseCampaignpRef, //  Include CauseCampaignRef
                            ReduceFees = cp.ReduceFees,
                            Reason = cp.Reason,
                            DateCreated = cp.DateCreated
                        });
                    }
                }

                // Fetch dependents for dropdown
                var dependentsList = dependants
                    .OrderBy(d => d.PersonName) //  Order by Name for easier search
                    .Select(d => new SelectListItem
                    {
                        Value = d.PersonRegNumber,
                        Text = $"{d.PersonName} ({d.PersonRegNumber})"
                    }).ToList();

                ViewBag.DependentsList = dependentsList;
                ViewBag.CauseList = causes; //  Pass causes to view

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "View Exemptions",
                    $"Admin {currentUser.Email} navigated to the custom exempt Payment page.", ct);

                return View(exemptDependantsList);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading exemptions. Please try again later.";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Error",
                    $"Error in CustomPayments page: {ex.Message}", ct);
                return RedirectToAction("Dashboard", "Admin");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrUpdateExemption(ExemptDependantViewModel model, CancellationToken ct)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                string changedBy = currentUser != null ? $"{currentUser.FirstName} {currentUser.Surname}" : "Unknown";

                if (string.IsNullOrWhiteSpace(model.PersonRegNumber) || string.IsNullOrWhiteSpace(model.CauseCampaignRef))
                {
                    TempData[SD.Error] = "Member and Cause selection are required.";
                    return RedirectToAction(nameof(CustomPayments));
                }

                var existingDependant = await _db.Dependants.FirstOrDefaultAsync(d => d.PersonRegNumber == model.PersonRegNumber, ct);
                if (existingDependant == null)
                {
                    TempData[SD.Error] = "Dependant not found.";
                    return RedirectToAction(nameof(CustomPayments));
                }

                //  Check if an exemption already exists for the same PersonRegNumber and CauseCampaignRef
                var existingPayment = await _db.CustomPayment
                    .FirstOrDefaultAsync(cp => cp.PersonRegNumber == model.PersonRegNumber && cp.CauseCampaignpRef == model.CauseCampaignRef, ct);

                var changeLogs = new List<ChangeLog>();
                var noteHistory = new List<NoteHistory>();

                if (existingPayment != null)
                {
                    //  If it's an "Add" operation but the record already exists, show an error
                    if (model.Id == 0)
                    {
                        TempData[SD.Error] = "A reduction for this cause already exists.";
                        return RedirectToAction(nameof(CustomPayments));
                    }

                    //  If it's an "Edit" operation, update the existing record
                    if (existingPayment.ReduceFees != model.ReduceFees)
                    {
                        changeLogs.Add(new ChangeLog
                        {
                            UserId = currentUser?.UserId,
                            DependentId = existingDependant.Id,
                            Action = "Edited Custom Payment",
                            FieldName = "ReduceFees",
                            OldValue = $"£{existingPayment.ReduceFees}",
                            NewValue = $"£{model.ReduceFees}",
                            ChangeDate = DateTime.UtcNow,
                            ChangedBy = changedBy
                        });

                        noteHistory.Add(new NoteHistory
                        {
                            NoteTypeId = 3,
                            PersonRegNumber = model.PersonRegNumber,
                            Description = $"Custom payment reduction updated by {changedBy}: Amount changed from £{existingPayment.ReduceFees} to £{model.ReduceFees}.",
                            CreatedBy = currentUser.Email,
                            CreatedByName = changedBy,
                            DateCreated = DateTime.UtcNow
                        });

                        existingPayment.ReduceFees = model.ReduceFees ?? 0;
                    }

                    if (existingPayment.Reason != model.Reason)
                    {
                        changeLogs.Add(new ChangeLog
                        {
                            UserId = currentUser?.UserId,
                            DependentId = existingDependant.Id,
                            Action = "Edited Custom Payment",
                            FieldName = "Reason",
                            OldValue = existingPayment.Reason,
                            NewValue = model.Reason,
                            ChangeDate = DateTime.UtcNow,
                            ChangedBy = changedBy
                        });

                        noteHistory.Add(new NoteHistory
                        {
                            NoteTypeId = 3,
                            PersonRegNumber = model.PersonRegNumber,
                            Description = $"Custom payment reason updated by {changedBy}: '{existingPayment.Reason}' changed to '{model.Reason}'.",
                            CreatedBy = currentUser.Email,
                            CreatedByName = changedBy,
                            DateCreated = DateTime.UtcNow
                        });

                        existingPayment.Reason = model.Reason;
                    }

                    existingPayment.CreatedBy = currentUser.Email;
                    existingPayment.CreatedByName = changedBy;
                    existingPayment.DateCreated = DateTime.UtcNow;

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Update Custom Payment",
                        $"Admin {changedBy} updated Custom Payment for {model.PersonRegNumber}.", ct);
                }
                else
                {
                    //  If no existing record for this CauseCampaignRef, add a new exemption
                    var newExemption = new CustomPayment
                    {
                        PersonRegNumber = model.PersonRegNumber,
                        ReduceFees = model.ReduceFees ?? 0,
                        UserId = existingDependant.UserId,
                        CauseCampaignpRef = model.CauseCampaignRef,
                        Reason = model.Reason,
                        CreatedBy = currentUser.Email,
                        CreatedByName = changedBy,
                        DateCreated = DateTime.UtcNow
                    };

                    _db.CustomPayment.Add(newExemption);

                    noteHistory.Add(new NoteHistory
                    {
                        NoteTypeId = 3,
                        PersonRegNumber = model.PersonRegNumber,
                        Description = $"Custom payment exemption added by {changedBy}. Amount reduced: £{model.ReduceFees}.",
                        CreatedBy = currentUser.Email,
                        CreatedByName = changedBy,
                        DateCreated = DateTime.UtcNow
                    });

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Added Custom Payment",
                        $"Admin {changedBy} added Custom Payment for {model.PersonRegNumber}.", ct);
                }

                if (changeLogs.Any())
                    _db.ChangeLogs.AddRange(changeLogs);
                if (noteHistory.Any())
                    _db.NoteHistory.AddRange(noteHistory);

                await _db.SaveChangesAsync(ct);

                TempData[SD.Success] = "Custom Payment saved successfully.";
                return RedirectToAction(nameof(CustomPayments));
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "An error occurred while saving the Custom Payment.";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Error",
                    $"Error saving Custom Payment: {ex.Message}", ct);
                return RedirectToAction(nameof(CustomPayments));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CustomPaymentDelete(int id, CancellationToken ct)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var exemption = await _db.CustomPayment.FindAsync(id);
            if (exemption == null)
                return NotFound();

            _db.CustomPayment.Remove(exemption);

            var changeLog = new ChangeLog
            {
                UserId = currentUser?.UserId,
                DependentId = null,
                Action = "Deleted",
                FieldName = "CustomPayment",
                OldValue = $"Amount Reduced: £{exemption.ReduceFees}, Reason: {exemption.Reason}",
                NewValue = "Deleted",
                ChangeDate = DateTime.UtcNow,
                ChangedBy = $"{currentUser?.FirstName} {currentUser?.Surname}",
                NextOfKinId = null
            };

            var noteHistory = new NoteHistory
            {
                NoteTypeId = 3,
                PersonRegNumber = exemption.PersonRegNumber,
                Description = $"Custom payment removed by {currentUser?.FirstName} {currentUser?.Surname}.",
                CreatedBy = currentUser?.Email,
                CreatedByName = $"{currentUser?.FirstName} {currentUser?.Surname}",
                DateCreated = DateTime.UtcNow
            };

            _db.ChangeLogs.Add(changeLog);
            _db.NoteHistory.Add(noteHistory);

            await _db.SaveChangesAsync(ct);
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Deleted Custom Payment",
                $"Admin {currentUser?.FirstName} {currentUser?.Surname} deleted Custom Payment for {exemption.PersonRegNumber}.", ct);

            TempData["Success"] = "Custom Payment deleted successfully.";
            return RedirectToAction(nameof(CustomPayments));
        }

       
    }


}




