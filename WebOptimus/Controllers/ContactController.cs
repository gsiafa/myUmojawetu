using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebOptimus.Data;
using WebOptimus.Helpers;
using WebOptimus.Models;
using WebOptimus.Services;
using WebOptimus.StaticVariables;

namespace WebOptimus.Controllers
{
    public class ContactController : BaseController
    {
        private readonly IMapper _mapper;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<User> _userStore;
        private readonly IDataProtector protector;
        private readonly IPasswordValidator<User> passwordValidator;
        private readonly IUserValidator<User> userValidator;
        private readonly IPasswordHasher<User> passwordHasher;
        private readonly RequestIpHelper _requestIpHelper;
        private readonly HttpClient httpClient;
        private readonly IPostmarkClient _postmark;
        public ContactController(IMapper mapper, UserManager<User> userManager,
           SignInManager<User> signInManager, RoleManager<IdentityRole> roleManager, IUserStore<User> userStore,
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
            this._requestIpHelper = ipHelper;
            this.httpClient = httpClient;
            _postmark = postmark;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var contact = await _db.Contacts.Where(a=>a.Status != "Replied to query").ToListAsync();
            foreach (var i in contact)
            {
                i.DateReplied = i.DateCreated.ToString("dd/MM/yyyy h:mm:ss tt");  

            }
            ViewBag.total = _db.Contacts.Where(a=>a.Status == "Replied to query").Count();
            return View(contact);
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> PreviousQuries()
        {
            var contact = await _db.Contacts.Where(a => a.Status == "Replied to query").ToListAsync();
            foreach (var i in contact)
            {
                i.DateReplied = i.DateCreated.ToString("dd/MM/yyyy h:mm:ss tt");

            }           
            return View(contact);
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);

            string myIP = _requestIpHelper.GetRequestIp();
            try
            {

              
                if (id == 0)
                {
                    TempData[SD.Error] = "Error getting user.";
                    await RecordAuditAsync(null, myIP, "Database", "Error fecting user from database to send confirm email.");
                    return RedirectToAction(nameof(Index));
                }
                var contact = await _db.Contacts.FirstOrDefaultAsync(m => m.Id == id);

                if (contact == null) 
                {
                    TempData[SD.Error] = "Error getting user.";

                }

                return View (contact);
            }
            catch (Exception ex) 
            {
                await RecordAuditAsync(currentUser, myIP, "Contact Details", "Error fecting contacts because: "+ ex.Message.ToString());
                return RedirectToAction(nameof(Index));
            }
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> PreviousDetails(int id, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);

            string myIP = _requestIpHelper.GetRequestIp();
            try
            {


                if (id == 0)
                {
                    TempData[SD.Error] = "Error getting user.";
                    await RecordAuditAsync(null, myIP, "Database", "Error fecting user from database to send confirm email.");
                    return RedirectToAction(nameof(Index));
                }
                var contact = await _db.Contacts.FirstOrDefaultAsync(m => m.Id == id);

                if (contact == null)
                {
                    TempData[SD.Error] = "Error getting user.";

                }
               
                    contact.DateReplied = contact.UpdateOn.ToString("dd/MM/yyyy h:mm:ss tt");

               
                return View(contact);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, myIP, "Contact Details", "Error fecting contacts because: " + ex.Message.ToString());
                return RedirectToAction(nameof(Index));
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(Contact model, CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                if (email == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                var currentUser = await _userManager.FindByEmailAsync(email);

                string myIP = _requestIpHelper.GetRequestIp();
                if (model.Id == 0)
                {
                    TempData[SD.Error] = "Error getting user.";
                    await RecordAuditAsync(null, myIP, "Database", "Error fecting user from database to send confirm email.");
                    return RedirectToAction(nameof(Index));
                }
                var contact = await _db.Contacts.FirstOrDefaultAsync(m => m.Id == model.Id);
                contact.Status = "Replied to query";
                contact.UpdateOn = DateTime.UtcNow;                
                contact.ApprovalNote = model.ApprovalNote;
                contact.RepliedEmail = currentUser.Email;
                _db.Contacts.Update(contact);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return RedirectToAction("Index", "Account");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Approved()
        {
            var contact = await _db.Contacts.Where(a => a.Status == "Replied to query").ToListAsync();
            foreach (var i in contact)
            {
                i.DateReplied = i.UpdateOn.ToString("dd/MM/yyyy");

            }
            return View(contact);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            try
            {
                               
                var email = HttpContext.Session.GetString("loginEmail");
                string myIP = _requestIpHelper.GetRequestIp();
                var objFromDb = _db.Contacts.FirstOrDefault(a=>a.Id==id);               
                if (objFromDb == null)
                {
                    return RedirectToAction(nameof(Index));
                }
               
                _db.Contacts.Remove(objFromDb);
                await _db.SaveChangesAsync(ct);

               
                TempData[SD.Success] = "Record deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await RecordAuditAsync("Database", "User tried to delete previous contact records but had internal error", ct);
                TempData[SD.Error] = "Internal server error - invalid request.";
                return RedirectToAction(nameof(Index));
            }
        }


    }
}
