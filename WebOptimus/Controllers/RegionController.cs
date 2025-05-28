using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotativaCore;
using WebOptimus.Data;
using WebOptimus.Helpers;
using WebOptimus.Models;
using WebOptimus.Models.ViewModel;
using WebOptimus.Services;
using WebOptimus.StaticVariables;


namespace WebOptimus.Controllers
{
   
    public class RegionController : BaseController
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
        public RegionController(IMapper mapper, UserManager<User> userManager,
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
                var region =  await _db.Region.ToListAsync();
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Region", "User navigated to Region ", ct);
                return View(region);
            }
            catch (Exception ex)
            {


                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Region", "ERROR: Navigating to Region page because of: " + ex.Message.ToString(), ct);
                
                return RedirectToAction("Index", "Admin");
            }
        }

        public IActionResult AddRegion()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRegion(RegionViewModel viewmodel, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);

            if (!ModelState.IsValid)
            {
                return View(viewmodel);
            }
            try
            {
               
                var region = _mapper.Map<Region>(viewmodel);

                var isExists = await _db.Region.Where(a => a.Name == region.Name).FirstOrDefaultAsync();

                if(isExists != null)
                {
                    TempData[SD.Error] = "Region Name already exists";

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Add Region", "Region Name: " + viewmodel.Name, " already exists. ", ct);

                    return View(viewmodel);


                }
                else
                {
                    region.UpdateOn = DateTime.UtcNow;
                    region.DateCreated = DateTime.UtcNow;
                    region.CreatedBy = currentUser.Email;
                    _db.Region.Add(region);
                    await _db.SaveChangesAsync();
                    TempData[SD.Success] = "Region added successfully";

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Add Region", "added Region: " + viewmodel.Name, " to table. ", ct);

                    return RedirectToAction(nameof(Index));
                }


              
            }
            catch (Exception ex)
            {

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Region", "ERROR: Adding Region because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Internal Error saving..";
                return RedirectToAction("Index", "Admin");

            }

        }


        //GET Edit 
        [HttpGet]

        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> EditRegion(int? id, CancellationToken ct)
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
                var region = await _db.Region.SingleOrDefaultAsync(m => m.Id == id);
                if (region == null)
                {
                    return NotFound();
                }
                var vm = _mapper.Map<RegionViewModel>(region);
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Region", "Navigated to Edit Region page for: " + vm.Name + " id: " + vm.Id, ct);
             
                return View(vm);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Region", "ERROR: Navigating to Edit Region page because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Internal Error saving..";
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRegion(RegionViewModel viewmodel, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);


            try
            {
                if (viewmodel.Id == 0)
                {
                    return NotFound();
                }
                var regiondetails = _mapper.Map<Region>(viewmodel);
                if (ModelState.IsValid)
                {
                    _db.Region.Update(regiondetails);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Region", "Edit Region name: " + viewmodel.Name + " id: " + viewmodel.Id + " added successfully.", ct);

                TempData[SD.Success] = "Region added successfully";
                return View(viewmodel);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Region", "ERROR: Saving Edited Region because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Internal Error saving..";
                return View(viewmodel);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRegion(int id, CancellationToken ct)
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
                    var region = await _db.Region.FirstOrDefaultAsync(m => m.Id == id);

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Delete Region", "Delete Region name: " + region.Name + " id: " + region.Id + " deleted from database.", ct);

                    _db.Region.Remove(region);
                    await _db.SaveChangesAsync();
                }

             
                TempData[SD.Success] = "Region deleted successfully";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return RedirectToAction("PageNotFound", "Home");
            }
        }
              


    }
}