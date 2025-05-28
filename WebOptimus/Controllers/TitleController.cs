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
   
    public class TitleController : BaseController
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
        public TitleController(IMapper mapper, UserManager<User> userManager,
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
                var Title =  await _db.Title.ToListAsync();
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Title", "User navigated to Title ", ct);
                return View(Title);
            }
            catch (Exception ex)
            {


                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Title", "ERROR: Navigating to Title page because of: " + ex.Message.ToString(), ct);
                
                return RedirectToAction("Index", "Admin");
            }
        }

        public IActionResult AddTitle()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTitle(TitleViewModel viewmodel, CancellationToken ct)
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
               
                var Title = _mapper.Map<Title>(viewmodel);

                var isExists = await _db.Title.Where(a => a.Name == Title.Name).FirstOrDefaultAsync();

                if(isExists != null)
                {
                    TempData[SD.Error] = "Title Name already exists";

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Add Title", "Title Name: " + viewmodel.Name, " already exists. ", ct);

                    return View(viewmodel);


                }
                else
                {
                    _db.Title.Add(Title);
                    await _db.SaveChangesAsync();
                    TempData[SD.Success] = "Title added successfully";

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Add Title", "added Title: " + viewmodel.Name, " to table. ", ct);

                    return RedirectToAction(nameof(Index));
                }


              
            }
            catch (Exception ex)
            {

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Title", "ERROR: Adding Title because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Internal Error saving..";
                return RedirectToAction("Index", "Admin");

            }

        }


        //GET Edit 
        [HttpGet]

        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> EditTitle(int? id, CancellationToken ct)
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
                var Title = await _db.Title.SingleOrDefaultAsync(m => m.Id == id);
                if (Title == null)
                {
                    return NotFound();
                }
                var vm = _mapper.Map<TitleViewModel>(Title);
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Title", "Navigated to Edit Title page for: " + vm.Name + " id: " + vm.Id, ct);
             
                return View(vm);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Title", "ERROR: Navigating to Edit Title page because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Internal Error saving..";
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTitle(TitleViewModel viewmodel, CancellationToken ct)
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
                var Titledetails = _mapper.Map<Title>(viewmodel);
                if (ModelState.IsValid)
                {
                    _db.Title.Update(Titledetails);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Title", "Edit Title name: " + viewmodel.Name + " id: " + viewmodel.Id + " added successfully.", ct);

                TempData[SD.Success] = "Title added successfully";
                return View(viewmodel);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Title", "ERROR: Saving Edited Title because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Internal Error saving..";
                return View(viewmodel);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTitle(int id, CancellationToken ct)
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
                    var Title = await _db.Title.FirstOrDefaultAsync(m => m.Id == id);

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Delete Title", "Delete Title name: " + Title.Name + " id: " + Title.Id + " deleted from database.", ct);

                    _db.Title.Remove(Title);
                    await _db.SaveChangesAsync();
                }

             
                TempData[SD.Success] = "Title deleted successfully";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return RedirectToAction("PageNotFound", "Home");
            }
        }
              


    }
}