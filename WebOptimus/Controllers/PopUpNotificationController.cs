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
    public class PopUpNotificationController : BaseController
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

        public PopUpNotificationController(IMapper mapper, UserManager<User> userManager,
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
                var PopUpNotification = await _db.PopUpNotification.ToListAsync(ct);
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Notification", "User navigated to Notification page ", ct);
                return View(PopUpNotification);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Notification", "ERROR: Navigating to Notification page because of: " + ex.Message.ToString(), ct);

                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpGet]
        public async Task<IActionResult> NewNotification(CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "New Notification", "Navigated to Notification page. ", ct);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewNotification(PopUpNotificationViewModel viewmodel, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);
            try
            {
                viewmodel.Author = currentUser.FirstName + " " + currentUser.Surname;
                viewmodel.Date = DateTime.UtcNow;
                var notification = _mapper.Map<PopUpNotification>(viewmodel);
                _db.PopUpNotification.Add(notification);
                await _db.SaveChangesAsync(ct);
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "New Notification", "Added new notification", ct);

                TempData["Success"] = "Notification Added Successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "New Notification", "ERROR: Adding New Notification because of: " + ex.Message.ToString(), ct);

                TempData["Error"] = "Error, please check logs.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditNotification(int id, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);

            var notification = await _db.PopUpNotification.FindAsync(id);
            if (notification == null)
            {
                TempData["Error"] = "Notification not found.";
                return RedirectToAction("Index", "Home");
            }
            var viewModel = _mapper.Map<PopUpNotificationViewModel>(notification);

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Notification", "Navigated to edit Notification page. ", ct);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNotification(PopUpNotificationViewModel viewmodel, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);
            try
            {
                var notificationToUpdate = await _db.PopUpNotification.Where(n => n.Id == viewmodel.Id).AsNoTracking().FirstOrDefaultAsync();
                if (notificationToUpdate == null)
                {
                    TempData["Error"] = "Notification Not Found";
                    return RedirectToAction(nameof(Index));
                }

                viewmodel.Date = notificationToUpdate.Date;  // Preserve the original date
                viewmodel.Author = notificationToUpdate.Author;  // Preserve the original author

                notificationToUpdate = _mapper.Map<PopUpNotification>(viewmodel);

                _db.PopUpNotification.Update(notificationToUpdate);
                await _db.SaveChangesAsync(ct);
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Notification", "Edited notification", ct);

                TempData["Success"] = "Notification Updated Successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Notification", "ERROR: Editing Notification because of: " + ex.Message.ToString(), ct);

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
                    var notification = await _db.PopUpNotification.FirstOrDefaultAsync(m => m.Id == id);

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Delete Notification", "Deleted Notification", ct);

                    _db.PopUpNotification.Remove(notification);
                    await _db.SaveChangesAsync();
                }

                TempData[SD.Success] = "Notification deleted successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "Could not delete, check logs...";
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Delete Notification", "ERROR: Deleting Notification because of " + ex.Message.ToString(), ct);

                return RedirectToAction(nameof(Index));
            }
        }
    


}
}
