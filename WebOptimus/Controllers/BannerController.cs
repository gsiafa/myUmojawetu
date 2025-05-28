using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebOptimus.Data;
using WebOptimus.Helpers;
using WebOptimus.Models;
using WebOptimus.Models.ViewModel;
using WebOptimus.Services;
using WebOptimus.StaticVariables;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace WebOptimus.Controllers
{
    public class BannerController : BaseController
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

        public BannerController(IMapper mapper, UserManager<User> userManager,
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
                var news = await _db.Banners.ToListAsync(ct);
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Banner", "User navigated to Banner page ", ct);
                return View(news);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Banner", "ERROR: Navigating to Banner page because of: " + ex.Message.ToString(), ct);

                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> NewBanner(CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "New Banner", "Navigated to Banner page. ", ct);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewBanner(BannerViewModel viewmodel, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);
            try
            {
                viewmodel.UserId = currentUser.UserId;
                viewmodel.Author = currentUser.FirstName + " " + currentUser.Surname;
                viewmodel.Date = DateTime.UtcNow;
                viewmodel.IsActive = viewmodel.IsActive;
                var news = _mapper.Map<Banner>(viewmodel);
                _db.Banners.Add(news);
                await _db.SaveChangesAsync(ct);
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "New Banner", "Added new Banners title: " + viewmodel.Title, ct);

                TempData["Success"] = "Banner Added Successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "New Banner", "ERROR: Adding New Banner because of: " + ex.Message.ToString(), ct);

                TempData["Error"] = "Error, please check logs.";
                return RedirectToAction(nameof(Index));
            }
        }


        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> EditBanner(int Id, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);

            var getnews = await _db.Banners.Where(a => a.Id == Id).FirstOrDefaultAsync();
            if (getnews == null) {

                TempData["Error"] = "Banner not found.";
                return RedirectToAction("Index", "Home");
            
            }
            var news = _mapper.Map<BannerViewModel>(getnews);
            news.Id = getnews.Id;
        

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Banner", "Navigated to edit Banner page. ", ct);

            return View(news);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBanner(BannerViewModel viewmodel, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);
            try
            {
                var getNewToUpdate = await _db.Banners.Where(a=>a.Id == viewmodel.Id).AsNoTracking().FirstOrDefaultAsync();
                if (getNewToUpdate == null)
                {
                    TempData["Error"] = "Details Not Found";
                    return RedirectToAction(nameof(Index));
                }

                viewmodel.Date = getNewToUpdate.Date;
                viewmodel.Author = getNewToUpdate.Author;

                getNewToUpdate = _mapper.Map<Banner>(viewmodel);

            
                _db.Banners.Update(getNewToUpdate);
                await _db.SaveChangesAsync(ct);
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Banner", "Edited Banners title: " + viewmodel.Title, ct);

                TempData["Success"] = "Updated Successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Banner", "ERROR: Editing Banner because of: " + ex.Message.ToString(), ct);

                TempData["Error"] = "Error, please check logs.";
                return RedirectToAction(nameof(Index));
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
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
                    var news = await _db.Banners.FirstOrDefaultAsync(m => m.Id == id);

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Delete Banner", "Delete Banner title: " + news.Title, ct);

                    _db.Banners.Remove(news);
                    await _db.SaveChangesAsync();
                }


                TempData[SD.Success] = "Banner deleted successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex) 
            {
                TempData[SD.Error] = "Could not delete check logs...";
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Delete Banner", "ERROR: Deleting Banner because of " + ex.Message.ToString(), ct);

                return RedirectToAction(nameof(Index));
            }
        }


    }
}
