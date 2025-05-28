using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyModel;
using RotativaCore;
using System;
using WebOptimus.Data;
using WebOptimus.Helpers;
using WebOptimus.Models;
using WebOptimus.Models.ViewModel;
using WebOptimus.Models.ViewModel.Admin;
using WebOptimus.Services;
using WebOptimus.StaticVariables;


namespace WebOptimus.Controllers
{
   
    public class NextOfKinsController : BaseController
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
        public NextOfKinsController(IMapper mapper, UserManager<User> userManager,
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
                var nextOfKinData = await _db.NextOfKins
                    .Join(
                        _db.Dependants,
                        nextOfKin => nextOfKin.PersonRegNumber,
                        dependent => dependent.PersonRegNumber,
                        (nextOfKin, dependent) => new NextOfKinWithDependentViewModel
                        {
                            Id = nextOfKin.Id,
                            UserId = nextOfKin.UserId,
                            DependentName = dependent.PersonName,
                            PersonRegNumber = dependent.PersonRegNumber,
                            NextOfKinName = nextOfKin.NextOfKinName,
                            NextOfKinAddress = nextOfKin.NextOfKinAddress,
                            NextOfKinEmail = nextOfKin.NextOfKinEmail,
                            NextOfKinTel = nextOfKin.NextOfKinTel,
                            Relationship = nextOfKin.Relationship
                        }
                    )
                    .OrderBy(a=>a.NextOfKinName).ToListAsync();

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Next of Kin", "User navigated to Next of Kin ", ct);
                return View(nextOfKinData);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Next of Kin", "ERROR: Navigating to Next of Kin page because of: " + ex.Message.ToString(), ct);
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> AddNextOfKin(CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);

            var dependentsWithoutNextOfKin = await _db.Dependants
                .Where(d => !_db.NextOfKins.Any(k => k.PersonRegNumber == d.PersonRegNumber))
                .OrderBy(d => d.PersonName)
                .Select(d => new SelectListItem
                {
                    Value = d.PersonRegNumber, 
                    Text = $"{d.PersonName} ({d.PersonRegNumber})" //  Display Name + Reg Number for clarity
                })
                .ToListAsync(ct);

            var viewModel = new AddNextOfKinViewModel
            {
                Dependents = dependentsWithoutNextOfKin
            };

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "AddNextOfKin", "User navigated to Next of Kin page ", ct);

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNextOfKin(AddNextOfKinViewModel model, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);

            if (!ModelState.IsValid)
            {
                // Reload dependents without next of kin for the dropdown
                model.Dependents = await _db.Dependants
                    .Where(d => !_db.NextOfKins.Any(n => n.PersonRegNumber == d.PersonRegNumber))
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.PersonName
                    })
                    .ToListAsync(ct);

                return View(model);
            }


         

                var existingNextOfKin = await _db.NextOfKins
                    .FirstOrDefaultAsync(n => n.PersonRegNumber == model.PersonRegNumber);

                if (existingNextOfKin != null)
                {
                    TempData[SD.Error] = "Next of Kin already added.";
                    model.Dependents = await _db.Dependants.Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.PersonName
                    }).ToListAsync();
                    return View(model);
                }

                var dependent = await _db.Dependants.FirstOrDefaultAsync(d => d.PersonRegNumber == model.PersonRegNumber && d.IsActive == true);
                if (dependent == null)
                {
                    TempData[SD.Error] = "Selected dependent is invalid.";
                    model.Dependents = await _db.Dependants.Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.PersonName
                    }).ToListAsync();
                    return View(model);
                }

                var nextOfKin = new NextOfKin
                {
                    UserId = dependent.UserId,
                    PersonRegNumber = model.PersonRegNumber,
                    NextOfKinName = model.NextOfKinName,
                    NextOfKinAddress = model.NextOfKinAddress,
                    NextOfKinEmail = model.NextOfKinEmail,
                    NextOfKinTel = model.NextOfKinTel,
                    Relationship = model.Relationship,
                    DependentId = dependent.Id,
                    DateCreated = DateTime.UtcNow,
                    CreatedBy = currentUser.Email,
                };

                _db.NextOfKins.Add(nextOfKin);
                await _db.SaveChangesAsync();
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "AddNextOfKin", "Added Next of kin: " + model.NextOfKinName, ct);

                TempData[SD.Success] = "Next of Kin added successfully.";
                return RedirectToAction(nameof(Index));
            

        }
        [HttpGet]
        public async Task<IActionResult> GetDependentRegNumber(string personRegNumber)
        {
            var dependent = await _db.Dependants.FirstOrDefaultAsync(d => d.PersonRegNumber == personRegNumber && d.IsActive == true);
            if (dependent != null)
            {
                return Json(dependent.PersonRegNumber);
            }
            return Json("");
        }

        //GET Edit 
        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> EditNextOfKin(int id, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);
            var nextOfKin = await _db.NextOfKins.FindAsync(id);
            if (nextOfKin == null)
            {
                TempData[SD.Error] = "Next of Kin not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check if the dependent exists in Dependants table
            var dependent = await _db.Dependants.FirstOrDefaultAsync(d => d.PersonRegNumber == nextOfKin.PersonRegNumber && d.IsActive == true);

            // If not found, check in the ConfirmedDeath table
            if (dependent == null)
            {
                var confirmedDeath = await _db.ConfirmedDeath.FirstOrDefaultAsync(cd => cd.PersonRegNumber == nextOfKin.PersonRegNumber);
                if (confirmedDeath != null)
                {
                    dependent = new Dependant
                    {
                        PersonRegNumber = confirmedDeath.PersonRegNumber,
                        PersonName = confirmedDeath.PersonName, // Assuming this is the correct field in ConfirmedDeath
                        UserId = confirmedDeath.UserId
                    };
                }
            }

            // Ensure dependent is not null before accessing properties
            var viewModel = new AddNextOfKinViewModel
            {
                Id = nextOfKin.Id,
                PersonRegNumber = nextOfKin.PersonRegNumber,
                DependentRegNumber = dependent?.PersonRegNumber,
                NextOfKinName = nextOfKin.NextOfKinName,
                NextOfKinAddress = nextOfKin.NextOfKinAddress,
                NextOfKinEmail = nextOfKin.NextOfKinEmail,
                NextOfKinTel = nextOfKin.NextOfKinTel,
                Relationship = nextOfKin.Relationship,
                DependentName = dependent?.PersonName ?? "Unknown", // Handle missing name
                Dependents = await _db.Dependants
                    .Where(d => d.IsActive == true) // Only show active dependents
                    .Select(d => new SelectListItem
                    {
                        Value = d.PersonRegNumber,
                        Text = d.PersonName
                    }).ToListAsync()
            };

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "EditNextOfKin", "Navigated to Edit Next of Kin: " + id, ct);

            viewModel.Id = nextOfKin.Id;
            viewModel.UserId = dependent.UserId;
            viewModel.PersonRegNumber = dependent.PersonRegNumber;
            return View(viewModel);
        }


        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNextOfKin(AddNextOfKinViewModel model, CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                if (email == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                var currentUser = await _userManager.FindByEmailAsync(email);
                if (ModelState.IsValid)
                {
                    var existingNextOfKin = await _db.NextOfKins
                        .FirstOrDefaultAsync(n => n.PersonRegNumber == model.PersonRegNumber && n.Id != model.Id);

                    if (existingNextOfKin != null)
                    {
                        TempData[SD.Error] = "Next of Kin already added for the selected dependent.";
                        model.Dependents = await _db.Dependants.Select(d => new SelectListItem
                        {
                            Value = d.Id.ToString(),
                            Text = d.PersonName
                        }).ToListAsync();
                        return View(model);
                    }

                    var nextOfKin = await _db.NextOfKins.FindAsync(model.Id);
                    if (nextOfKin == null)
                    {
                        TempData[SD.Error] = "Next of Kin not found.";
                        return RedirectToAction(nameof(Index));
                    }

                    if(model.NextOfKinEmail == null)
                    {
                        nextOfKin.NextOfKinEmail = "";
                    }

                    nextOfKin.UserId = model.UserId;
                    nextOfKin.PersonRegNumber = model.PersonRegNumber;
                    nextOfKin.NextOfKinName = model.NextOfKinName;
                    nextOfKin.NextOfKinAddress = model.NextOfKinAddress;
                    nextOfKin.NextOfKinEmail = model.NextOfKinEmail;
                    nextOfKin.NextOfKinTel = model.NextOfKinTel;
                    nextOfKin.Relationship = model.Relationship;

                    _db.NextOfKins.Update(nextOfKin);
                    await _db.SaveChangesAsync();

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "EditNextOfKin", "Updated Next of kin: " + model.Id, ct);

                    TempData[SD.Success] = "Next of Kin updated successfully.";
                    return RedirectToAction(nameof(Index));
                }

                model.Dependents = await _db.Dependants.Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.PersonName
                }).ToListAsync();

                return View(model);
            }catch(Exception ex)
            {
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
                    var nextofKin = await _db.NextOfKins.FirstOrDefaultAsync(m => m.Id == id);
                    if (nextofKin == null)
                    {
                        TempData[SD.Error] = "Next of Kin not found.";
                        return RedirectToAction(nameof(Index));
                    }

                    _db.NextOfKins.Remove(nextofKin);
                    await _db.SaveChangesAsync();

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "DeleteNextOfKin", "Deleted Next of Kin: " + nextofKin.NextOfKinName, ct);
                }

                TempData[SD.Success] = "Next of Kin deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return RedirectToAction("PageNotFound", "Home");
            }
        }


    }
}