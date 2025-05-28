using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebOptimus.Data;
using WebOptimus.Helpers;
using WebOptimus.Models;
using WebOptimus.Models.ViewModel;
using WebOptimus.Services;
using WebOptimus.StaticVariables;


namespace WebOptimus.Controllers
{

    public class CityController : BaseController
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
        public CityController(IMapper mapper, UserManager<User> userManager,
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
                var city = await _db.City.Include(s => s.Region).ToListAsync();
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "City", "User navigated to City ", ct);
                
                return View(city);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "City", "ERROR: Navigating to City page because of: " + ex.Message.ToString(), ct);

                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpGet]

        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> AddCity(CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);

            try
            {
                CityViewModel model = new CityViewModel()
                {
                    RegionList = await _db.Region.ToListAsync(),
                    Cities = new Models.City(),
                    CityList = await _db.City.OrderBy(p => p.Name).Select(p => p.Name).Distinct().ToListAsync()
                };

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Add City", "Navigating to Add City for: ", ct);

                return View(model);
            }
            catch (Exception ex)
            {

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Add City", "ERROR: Navigating to City page because of: " + ex.Message.ToString(), ct);

                return RedirectToAction("Index", "Admin");

            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCity(CityViewModel viewmodel, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);


            try
            {
                var doesSubCategoryExists = _db.City.Include(s => s.Region).Where(s => s.Name == viewmodel.Cities.Name && s.Region.Id == viewmodel.Cities.RegionId);
               

                CityViewModel modelVM = new CityViewModel()
                {
                    RegionList = await _db.Region.ToListAsync(),
                    Cities = viewmodel.Cities,
                    CityList = await _db.City.OrderBy(p => p.Name).Select(p => p.Name).ToListAsync(),

                };

                if (doesSubCategoryExists.Count() > 0)
                {
                    TempData[SD.Error] = "City name already exists under: " + doesSubCategoryExists.First().Region.Name;                  

                    return View(modelVM);

                }
                else
                {
                    viewmodel.Cities.DateCreated = DateTime.UtcNow;
                    viewmodel.Cities.UpdateOn = DateTime.UtcNow;
                    viewmodel.Cities.CreatedBy = currentUser.Email;
                    _db.City.Add(viewmodel.Cities);
                    await _db.SaveChangesAsync();

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Add City", "City name: " + viewmodel.Cities.Name, " added successfully under the region: " + doesSubCategoryExists.First().Region.Name, ct);
                    TempData[SD.Success] = "City added successfully";
                    return View(modelVM);
                }





            }
            catch (Exception ex)
            {

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "City", "ERROR: Adding City because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Internal Error saving..";
                return RedirectToAction("Index", "Admin");

            }

        }


        //GET Edit 
        [HttpGet]

        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> EditCity(int? id, CancellationToken ct)
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
             
          
               CityViewModel vm = new CityViewModel()
                {
                    RegionList = await _db.Region.ToListAsync(),
                    Cities = await _db.City.SingleOrDefaultAsync(m => m.Id == id),
                    CityList = await _db.City.OrderBy(p => p.Name).Select(p => p.Name).Distinct().ToListAsync()
                };

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit City", "Navigated to Edit City page for: " + vm.Cities.Name + " id: " + vm.Cities.Id, ct);

                ViewBag.regionId  = _db.Region.Where(a=>a.Id == vm.Cities.RegionId).FirstOrDefault();

                return View(vm);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit City", "ERROR: Navigating to Edit City page because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Internal Error saving..";
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCity(CityViewModel viewmodel, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);


            try
            {
                var doesSubCategoryExists = _db.City.Include(s => s.Region).Where(s => s.Name == viewmodel.Cities.Name && s.Region.Id == viewmodel.Cities.RegionId);

                CityViewModel modelVM = new CityViewModel()
                {
                    RegionList = await _db.Region.Where(a=>a.Id == viewmodel.Region.Id).ToListAsync(),
                    Cities = viewmodel.Cities,
                    CityList = await _db.City.OrderBy(p => p.Name).Select(p => p.Name).ToListAsync(),

                };

                if (doesSubCategoryExists.Count() > 0)
                {
                    TempData[SD.Error] = "City name already exists under: " + doesSubCategoryExists.First().Region.Name;

                    return View(modelVM);

                }
                else
                {
                    _db.City.Update(viewmodel.Cities);
                    await _db.SaveChangesAsync();

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit City", "City name: " + viewmodel.Cities.Name, " updated successfully under the region: " + doesSubCategoryExists.First().Region.Name, ct);
                    TempData[SD.Success] = "City details updated successfully";
                    return RedirectToAction(nameof(Index));
                }

            }
            catch (Exception ex)
            {

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "City", "ERROR: Adding City because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Internal Error saving..";
                return RedirectToAction("Index", "Admin");

            }
        }

        [ActionName("GetCity")]
        public async Task<IActionResult> GetCity(int id)
        {
            List<City> subCategories = new List<City>();

            subCategories = await (from subCategory in _db.City
                                   where subCategory.RegionId == id
                                   select subCategory).ToListAsync();

            //city
            var cit = _db.City.OrderBy(a => a.Name).ToList();
            List<City> ci = new();
            ci = cit;

            ViewBag.CityId = ci;
            return Json(new SelectList(subCategories, "Id", "Name"));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCity(int id, CancellationToken ct)
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
                    var City = await _db.City.FirstOrDefaultAsync(m => m.Id == id);

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Delete City", "Delete City name: " + City.Name + " id: " + City.Id + " deleted from database.", ct);

                    _db.City.Remove(City);
                    await _db.SaveChangesAsync();
                }


                TempData[SD.Success] = "City deleted successfully";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return RedirectToAction("PageNotFound", "Home");
            }
        }



    }
}