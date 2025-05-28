using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RotativaCore;
using System.Security.Claims;
using WebOptimus.Data;
using WebOptimus.Helpers;
using WebOptimus.Models;
using WebOptimus.Models.ViewModel;
using WebOptimus.Services;
using WebOptimus.StaticVariables;


namespace WebOptimus.Controllers
{

    public class SettingController : BaseController
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
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IPostmarkClient _postmark;
        public SettingController(IMapper mapper, UserManager<User> userManager,
           SignInManager<User> signInManager, IWebHostEnvironment hostEnvironment, RoleManager<IdentityRole> roleManager, IUserStore<User> userStore,
            RequestIpHelper ipHelper,
          HttpClient httpClient,
             IPostmarkClient postmark,
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
            _hostEnvironment = hostEnvironment;
        }

        [HttpGet]

        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> Index(CancellationToken ct)
        {

            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);

            try
            {
                var Setting = await _db.Settings.ToListAsync();
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Setting", "User navigated to Setting ", ct);
                
                return View(Setting);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Setting", "ERROR: Navigating to Setting page because of: " + ex.Message.ToString(), ct);

                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpGet]

        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> AddSetting(CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);       
            return View();           

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSetting(SettingViewModel viewmodel, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);


            try
            {
                var doesSubCategoryExists = _db.Settings.Where(s => s.Name == viewmodel.Name);
               


                if (doesSubCategoryExists.Count() > 0)
                {
                    TempData[SD.Error] = "Setting name: " + doesSubCategoryExists.First().Name + " already exists.";                  

                    return View(viewmodel);

                }
                else
                {
                    Settings Set = new Settings()
                    {
                        Name = viewmodel.Name,
                        DateCreated = DateTime.UtcNow,
                        IsActive = viewmodel.IsActive,
                    };
                  
                    _db.Settings.Add(Set);
                    await _db.SaveChangesAsync();

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Add Setting", "Setting name: " + viewmodel.Name, " added successfully", ct);
                    TempData[SD.Success] = "Setting added successfully";
                    return RedirectToAction(nameof(Index));
                }





            }
            catch (Exception ex)
            {

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Setting", "ERROR: Adding Setting because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Internal Error saving..";
                return RedirectToAction("Index", "Admin");

            }

        }


        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> EditSetting(int? id, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

            try
            {
                if (id == null)
                {
                    return NotFound();
                }

                // Fetch the Setting entity from the database
                var setting = await _db.Settings.FirstOrDefaultAsync(a => a.Id == id);

                if (setting == null)
                {
                    return NotFound();
                }

                // Map the Setting entity to SettingViewModel
                var viewModel = new SettingViewModel
                {
                    Id = setting.Id,
                    Name = setting.Name,
                    IsActive = setting.IsActive,
                    MinimumAge = setting.MinimumAge

                };

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Setting",
                    "Navigated to Edit Setting page for: " + setting.Name, ct);

                // Pass the mapped viewModel to the view
                return View(viewModel);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Setting",
                    "ERROR: Navigating to Edit Setting page because of: " + ex.Message, ct);

                TempData[SD.Error] = "Internal Error loading the page.";
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSetting(SettingViewModel viewmodel, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);


            try
            {
                var doesSubCategoryExists = _db.Settings.Where(s => s.Name == viewmodel.Name && s.Id != viewmodel.Id);

             

                if (doesSubCategoryExists.Count() > 0)
                {
                    TempData[SD.Error] = "Setting name already exists ";

                    return View(viewmodel);

                }
                else
                {
                    var getdata = await _db.Settings.FirstOrDefaultAsync(s => s.Id == viewmodel.Id);

                    getdata.Name = viewmodel.Name;
                    getdata.IsActive = viewmodel.IsActive;
                    getdata.MinimumAge = viewmodel.MinimumAge;

                    _db.Settings.Update(getdata);
                    await _db.SaveChangesAsync();

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Setting", "Setting name: " + viewmodel.Name, " updated successfully", ct);
                    TempData[SD.Success] = "Setting details updated successfully";
                    return RedirectToAction(nameof(Index));
                }

            }
            catch (Exception ex)
            {

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Setting", "ERROR: Adding Setting because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Internal Error saving..";
                return RedirectToAction("Index", "Admin");

            }
        }

    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSetting(int id, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);

            try
            {
                if (id != 0)
                {
                    var Setting = await _db.Settings.FirstOrDefaultAsync(m => m.Id == id);

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Delete Setting", "Delete Setting name: " + Setting.Name + " id: " + Setting.Id + " deleted from database.", ct);

                    _db.Settings.Remove(Setting);
                    await _db.SaveChangesAsync();
                }


                TempData[SD.Success] = "Setting deleted successfully";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return RedirectToAction("PageNotFound", "Home");
            }
        }

        //[HttpGet]
        //public IActionResult IsPollActive()
        //{
        //    // Retrieve the "IsPollActive" setting
        //    var isPollActive = _db.Settings.FirstOrDefault(s => s.Name == "Is Poll Active")?.IsActive ?? false;
        //    return Json(new { isPollActive });
        //}

        [HttpGet]
        public async Task<IActionResult> IsPollActiveAndNotCompleted(CancellationToken ct)
        {
            // Get the current user's ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if the poll is active in the settings table
            var pollActiveSetting = await _db.Settings.FirstOrDefaultAsync(s => s.Name == "Is Poll Active", ct);
            bool isPollActive = pollActiveSetting?.IsActive ?? false;

            if (!isPollActive)
            {
                return Json(new { isPollActive = false, hasCompletedPoll = false });
            }

            // Check if the user has already submitted responses to any poll
            var hasCompletedPoll = await _db.PollResponse
                .AnyAsync(r => r.UserId == userId, ct);

            // Return the result in JSON format
            return Json(new { isPollActive = isPollActive, hasCompletedPoll = hasCompletedPoll });
        }



    }
}