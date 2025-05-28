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


namespace WebOptimus.Controllers
{
    public class AnnouncementController : BaseController
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

        public AnnouncementController(IMapper mapper, UserManager<User> userManager,
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
                var news = await _db.Announcements.ToListAsync(ct);
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Announcement", "User navigated to Announcement page ", ct);
                return View(news);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Announcement", "ERROR: Navigating to Announcement page because of: " + ex.Message.ToString(), ct);

                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> NewAnnouncement(CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "New Announcement", "Navigated to Announcement page. ", ct);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewAnnouncement(AnnouncementViewModel viewmodel, CancellationToken ct)
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
                viewmodel.IsActiveToInternMember = viewmodel.IsActiveToInternMember;
                viewmodel.IsActiveToPublic = viewmodel.IsActiveToPublic;
                var news = _mapper.Map<Announcement>(viewmodel);
                _db.Announcements.Add(news);
                await _db.SaveChangesAsync(ct);
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "New Announcement", "Added new announcements title: " + viewmodel.Title, ct);

                TempData["Success"] = "Announcement Added Successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "New Announcement", "ERROR: Adding New Announcement because of: " + ex.Message.ToString(), ct);

                TempData["Error"] = "Error, please check logs.";
                return RedirectToAction(nameof(Index));
            }
        }


        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> EditAnnouncement(int Id, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);

            var getnews = await _db.Announcements.Where(a => a.Id == Id).FirstOrDefaultAsync();
            if (getnews == null) {

                TempData["Error"] = "Announcement not found.";
                return RedirectToAction("Index", "Home");
            
            }
            var news = _mapper.Map<AnnouncementViewModel>(getnews);
            news.Id = getnews.Id;
        

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Announcement", "Navigated to edit Announcement page. ", ct);

            return View(news);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAnnouncement(AnnouncementViewModel viewmodel, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);
            try
            {
                var getNewToUpdate = await _db.Announcements.Where(a=>a.Id == viewmodel.Id).AsNoTracking().FirstOrDefaultAsync();
                if (getNewToUpdate == null)
                {
                    TempData["Error"] = "Details Not Found";
                    return RedirectToAction(nameof(Index));
                }

                viewmodel.Date = getNewToUpdate.Date;
                viewmodel.Author = getNewToUpdate.Author;

                getNewToUpdate = _mapper.Map<Announcement>(viewmodel);

            
                _db.Announcements.Update(getNewToUpdate);
                await _db.SaveChangesAsync(ct);
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Announcement", "Edited announcements title: " + viewmodel.Title, ct);

                TempData["Success"] = "Updated Successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Announcement", "ERROR: Editing Announcement because of: " + ex.Message.ToString(), ct);

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
                    var news = await _db.Announcements.FirstOrDefaultAsync(m => m.Id == id);

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Delete Announcement", "Delete Announcement title: " + news.Title, ct);

                    _db.Announcements.Remove(news);
                    await _db.SaveChangesAsync();
                }


                TempData[SD.Success] = "Announcement deleted successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex) 
            {
                TempData[SD.Error] = "Could not delete check logs...";
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Delete Announcement", "ERROR: Deleting Announcement because of " + ex.Message.ToString(), ct);

                return RedirectToAction(nameof(Index));
            }
        }


    }
}
