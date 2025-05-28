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
   
    public class GenderController : BaseController
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
        public GenderController(IMapper mapper, UserManager<User> userManager,
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
                var Gender =  await _db.Gender.ToListAsync();
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Gender", "User navigated to Gender ", ct);
                return View(Gender);
            }
            catch (Exception ex)
            {


                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Gender", "ERROR: Navigating to Gender page because of: " + ex.Message.ToString(), ct);
                
                return RedirectToAction("Index", "Admin");
            }
        }

        public IActionResult AddGender()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGender(GenderViewModel viewmodel, CancellationToken ct)
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
               
                var Gender = _mapper.Map<Gender>(viewmodel);

                var isExists = await _db.Gender.Where(a => a.GenderName == Gender.GenderName).FirstOrDefaultAsync();

                if(isExists != null)
                {
                    TempData[SD.Error] = "Gender Name already exists";

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Add Gender", "Gender Name: " + viewmodel.GenderName, " already exists. ", ct);

                    return View(viewmodel);


                }
                else
                {
                    _db.Gender.Add(Gender);
                    await _db.SaveChangesAsync();
                    TempData[SD.Success] = "Gender added successfully";

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Add Gender", "added Gender: " + viewmodel.GenderName, " to table. ", ct);

                    return RedirectToAction(nameof(Index));
                }


              
            }
            catch (Exception ex)
            {

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Gender", "ERROR: Adding Gender because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Internal Error saving..";
                return RedirectToAction("Index", "Admin");

            }

        }


        //GET Edit 
        [HttpGet]

        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> EditGender(int? id, CancellationToken ct)
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
                var Gender = await _db.Gender.SingleOrDefaultAsync(m => m.Id == id);
                if (Gender == null)
                {
                    return NotFound();
                }
                var vm = _mapper.Map<GenderViewModel>(Gender);
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Gender", "Navigated to Edit Gender page for: " + vm.GenderName + " id: " + vm.Id, ct);
             
                return View(vm);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Gender", "ERROR: Navigating to Edit Gender page because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Internal Error saving..";
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGender(GenderViewModel viewmodel, CancellationToken ct)
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
                var Genderdetails = _mapper.Map<Gender>(viewmodel);
                if (ModelState.IsValid)
                {
                    _db.Gender.Update(Genderdetails);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Gender", "Edit Gender name: " + viewmodel.GenderName + " id: " + viewmodel.Id + " added successfully.", ct);

                TempData[SD.Success] = "Gender added successfully";
                return View(viewmodel);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Gender", "ERROR: Saving Edited Gender because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Internal Error saving..";
                return View(viewmodel);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGender(int id, CancellationToken ct)
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
                    var Gender = await _db.Gender.FirstOrDefaultAsync(m => m.Id == id);

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Delete Gender", "Delete Gender name: " + Gender.GenderName + " id: " + Gender.Id + " deleted from database.", ct);

                    _db.Gender.Remove(Gender);
                    await _db.SaveChangesAsync();
                }

             
                TempData[SD.Success] = "Gender deleted successfully";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return RedirectToAction("PageNotFound", "Home");
            }
        }
              


    }
}