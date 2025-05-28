using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotativaCore;
using WebOptimus.Custom_Validation;
using WebOptimus.Data;
using WebOptimus.Helpers;
using WebOptimus.Models;
using WebOptimus.Models.ViewModel;
using WebOptimus.Models.ViewModel.Constitution;
using WebOptimus.Services;
using WebOptimus.StaticVariables;


namespace WebOptimus.Controllers
{
   
    public class UploadController : BaseController
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
        private readonly IFileUploadService _fileUploadService;
        private readonly IAuditService _auditService;
        public UploadController(IMapper mapper, UserManager<User> userManager,
           SignInManager<User> signInManager, IWebHostEnvironment hostEnvironment, IFileUploadService fileUploadService, IAuditService auditService, RoleManager<IdentityRole> roleManager, IUserStore<User> userStore,
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
            _fileUploadService = fileUploadService;
            _auditService = auditService;
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
                var constitutions = await _db.Constitution
                    .OrderByDescending(c => c.UploadedOn)
                    .ToListAsync(ct);

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Constitution", "User navigated to Constitution Management", ct);

                return View(constitutions);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Constitution", "ERROR: Navigating to Constitution Management page because of: " + ex.Message, ct);
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> Upload(CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Constitution Upload", "User navigated to Constitution upload page", ct);

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Upload(ConstitutionUploadViewModel model, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null) return RedirectToAction("Index", "Home");

            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(email);
            var allowedExtensions = new[] { ".pdf" };

            // Compute file hash
            var fileHash = await FileHelper.ComputeSha256HashAsync(model.File, ct);

            //  Check for duplicate by hash
            var isDuplicate = await _db.Constitution
                .AnyAsync(c => c.FileHash == fileHash, ct);

            if (isDuplicate)
            {
                TempData[SD.Error] = "This file has already been uploaded.";
                return View(model);
            }

            // Proceed with upload
            var filePath = await _fileUploadService.UploadFileAsync(model.File, "constitutions", allowedExtensions, ct);

            var record = new Constitution
            {
                FilePath = filePath,
                UploadedBy = user.Email,
                UploadedOn = DateTime.UtcNow,
                IsActive = false,
                FileHash = fileHash
            };

            _db.Constitution.Add(record);
            await _db.SaveChangesAsync(ct);
            await _auditService.RecordAuditAsync("Upload", "Constitution", $"Uploaded new constitution file: {filePath}", ct);

            TempData[SD.Success] = "File uploaded successfully.";
            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> SetActiveConstitution(int id, CancellationToken ct)
        {
            var constitution = await _db.Constitution.FindAsync(id);
            if (constitution == null)
                return NotFound();

            if (constitution.IsActive)
            {
                // Unset
                constitution.IsActive = false;

                TempData[SD.Success] = "File is Inactive.";
                await _auditService.RecordAuditAsync("Constitution", "Update", $"Unset active constitution: {constitution.FilePath}", ct);
            }
            else
            {
                // Set only this as active, unset others
                var all = await _db.Constitution.ToListAsync(ct);
                foreach (var item in all)
                    item.IsActive = false;

                constitution.IsActive = true;
                TempData[SD.Success] = "File is Active.";
                await _auditService.RecordAuditAsync("Constitution", "Update", $"Set constitution as active: {constitution.FilePath}", ct);
            }

            await _db.SaveChangesAsync(ct);
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
                return RedirectToAction("Index", "Home");

            var currentUser = await _userManager.FindByEmailAsync(email);

            var constitution = await _db.Constitution.FindAsync(id);
            if (constitution == null)
                return NotFound();

            // Optionally delete file from disk
            var fullPath = Path.Combine(_hostEnvironment.WebRootPath, constitution.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            _db.Constitution.Remove(constitution);
            await _db.SaveChangesAsync(ct);

            await _auditService.RecordAuditAsync("Delete", "Constitution", $"User {currentUser.Email} deleted constitution file: {constitution.FilePath}", ct);

            TempData[SD.Success] = "File deleted successfully.";
            return RedirectToAction("Index");
        }


    }
}