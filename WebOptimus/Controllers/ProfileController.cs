namespace Tanzania.Controllers
{
    using AutoMapper;
    using DocumentFormat.OpenXml.Spreadsheet;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyModel;
    using Microsoft.IdentityModel.Tokens;
    using Newtonsoft.Json;
    using System;
    using System.Drawing;
    using System.Runtime.Intrinsics.Arm;
    using System.Security.Claims;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Web;
    using WebOptimus.Configuration;
    using WebOptimus.Controllers;
    using WebOptimus.Data;
    using WebOptimus.Helpers;
    using WebOptimus.Models;
    using WebOptimus.Models.ViewModel;
    using WebOptimus.Services;
    using WebOptimus.StaticVariables;

    public class ProfileController : BaseController
    {
        private readonly IMapper _mapper;
        private readonly SignInManager<User> _signInManager;
        private readonly IPostmarkClient _postmark;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<User> _userStore;
        private readonly IDataProtector protector;
        private readonly IPasswordValidator<User> passwordValidator;
        private readonly IUserValidator<User> userValidator;
        private readonly IPasswordHasher<User> passwordHasher;
        private readonly RequestIpHelper ipHelper;
        private readonly UrlEncoder _urlEncoder;


        public ProfileController(IMapper mapper, UserManager<User> userManager,
            SignInManager<User> signInManager, RoleManager<IdentityRole> roleManager, IUserStore<User> userStore,           
            IDataProtectionProvider dataProtectionProvider, DataProtectionPurposeStrings dataProtectionPurposeStrings,
		   RequestIpHelper ipHelper,
            IPostmarkClient postmark,
            IPasswordHasher<User> passwordHash,
            IUserValidator<User> userValid,
            UrlEncoder urlEncoder,
            IPasswordValidator<User> passwordVal,
            ApplicationDbContext db) :
            base(userManager, db, ipHelper)
        {
            _mapper = mapper;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _userStore = userStore;
            _postmark = postmark;
            passwordValidator = passwordVal;
            userValidator = userValid;
            passwordHasher = passwordHash;
            this.ipHelper = ipHelper;
            _urlEncoder = urlEncoder;
			protector = dataProtectionProvider.CreateProtector(dataProtectionPurposeStrings.EmailAddress);
		}



        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                var getUser = await _userManager.FindByEmailAsync(email);

                if (getUser == null)
                {
                    TempData[SD.Error] = "Error. Please contact Admin";
                    await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Profile", "Error retrieving profile because user can't be found in database.");
                    return RedirectToAction("Login", "Account");
                }

                var user = _mapper.Map<ProfileViewModel>(getUser);
                var pname = user.FirstName + " " + user.Surname;
                //user.Deps = await _db.Dependants.Where(a => a.UserId == getUser.UserId && a.PersonName == pname && a.IsDead == false).FirstOrDefaultAsync();

                //if (user.Deps != null)
                //{
                //    user.DOB = user.Deps.PersonYearOfBirth;
                //}

                var tit = await _db.Title.FirstOrDefaultAsync(a => a.Id == getUser.Title);
                user.TitleName = tit == null ? " " : tit.Name;

                var reg = await _db.Region.FirstOrDefaultAsync(a => a.Id == getUser.RegionId);
                user.RegionName = reg == null ? " " : reg.Name;

                var city = await _db.City.FirstOrDefaultAsync(a => a.Id == getUser.CityId);
                user.CityName = city == null ? " " : city.Name;

                var getRed = await _db.Region.OrderBy(a => a.Name).ToListAsync();
                ViewBag.RegionId = getRed;

                var mtitle = await _db.Title.OrderBy(a => a.Name).ToListAsync();
                ViewBag.Ttile = mtitle;

                var cit = await _db.City.OrderBy(a => a.Name).ToListAsync();
                ViewBag.CityId = cit;

                //user.ProfileEdit = new ProfileEditViewModel
                //{
                //    UserId = user.UserId,
                //    FirstName = user.FirstName,
                //    Surname = user.Surname,
                //    PhoneNumber = user.PhoneNumber,
                //    OutwardPostcode = user.OutwardPostcode,
                //    PersonYearOfBirth = user.PersonYearOfBirth,
                //    Email = user.Email,
                //    CityId = user.CityId,
                //    RegionId = user.RegionId,
                //    Title = user.Title
                //};

                user.OldEmail = getUser.Email;
                user.OldPhoneNumber = getUser.PhoneNumber;
                user.OutwardPostcode = getUser.OutwardPostcode;
                user.DateJoined = getUser.DateCreated.ToString("dd/MM/yyyy");
                user.FullName = user.FirstName + " " + user.Surname;
                user.UserId = user.UserId;
                user.DependentId = user.DependentId;
                await RecordAuditAsync(getUser, _requestIpHelper.GetRequestIp(), "Profile", "User navigated to Profile page.");
                return View(user);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Profile", "User navigated to but had the following error:" + ex.Message);
                HttpContext.Session.Clear();
                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel vm, CancellationToken ct)
        {
            try
            {
                string myIP = _requestIpHelper.GetRequestIp();
                var email = HttpContext.Session.GetString("loginEmail");

                var currentUser = await _userManager.FindByEmailAsync(email);

                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

              
                    if (vm.OldEmail != vm.Email)
                    {
                        var doesuserExists = await _userManager.FindByEmailAsync(vm.Email);
                        if (doesuserExists != null)
                        {
                            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "User Profile", "User tried to Edit an account for " + currentUser.Email + " but the email entered: " + vm.Email + " already exists. ");

                            TempData[SD.Error] = "Email already exists";

                            vm.OldEmail = currentUser.Email;

                            return View(vm);
                        }
                    }

                   
                    currentUser.Title = vm.Title;
                    currentUser.FirstName = vm.FirstName;
                    currentUser.Surname = vm.Surname;            
                    currentUser.OutwardPostcode = vm.OutwardPostcode;
                    currentUser.PhoneNumber = vm.PhoneNumber;
                    currentUser.Email = vm.Email;

                    currentUser.UserName = vm.Email;
                    currentUser.NormalizedUserName = vm.Email.ToUpper();
                    currentUser.NormalizedEmail = vm.Email.ToUpper();
                    currentUser.UpdateOn = DateTime.UtcNow;

                if (vm.OldPhoneNumber != vm.PhoneNumber)
                {
                    _db.Users.Update(currentUser);
                    _db.SaveChanges();


                    var userobj = _mapper.Map<ProfileViewModel>(currentUser);


                    var tit = await _db.Title.FirstOrDefaultAsync(a => a.Id == currentUser.Title);
                    userobj.TitleName = tit.Name;

                    var reg = await _db.Region.FirstOrDefaultAsync(a => a.Id == currentUser.RegionId);
                    userobj.RegionName = reg.Name;

                    var city = await _db.City.FirstOrDefaultAsync(a => a.Id == currentUser.CityId);
                    userobj.CityName = reg.Name;




                    vm.OldEmail = currentUser.Email;

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Profile", "Updated their phone number from: " + vm.OldPhoneNumber + " to " + vm.PhoneNumber, ct);
                    vm.OldPhoneNumber = currentUser.PhoneNumber;
                    TempData[SD.Success] = "Details updated successfully.";
                    return View(userobj);
                }

                else
                {
                    var userobj = _mapper.Map<ProfileViewModel>(currentUser);


                    var tit = await _db.Title.FirstOrDefaultAsync(a => a.Id == currentUser.Title);
                    userobj.TitleName = tit.Name;

                    var reg = await _db.Region.FirstOrDefaultAsync(a => a.Id == currentUser.RegionId);
                    userobj.RegionName = reg.Name;

                    var city = await _db.City.FirstOrDefaultAsync(a => a.Id == currentUser.CityId);
                    userobj.CityName = reg.Name;

                    vm.OldEmail = currentUser.Email;                  
                    vm.OldPhoneNumber = currentUser.PhoneNumber;               
                    return View(userobj);
                }
              
                   
                
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Profile", "User navigated to but had the following error:" + ex.Message.ToString());
                HttpContext.Session.Clear();
                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetFamily(CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            var getUser = await _userManager.FindByEmailAsync(email);

            if (getUser == null)
            {
                TempData[SD.Error] = "Error. Please contact Admin";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Profile", "Error retrieving profile because user can't be found in database.");
                return RedirectToAction("Login", "Account");
            }

            // Fetch dependants for the user with their respective title and gender names
            var dependants = await (from dep in _db.Dependants
                                    join title in _db.Title on dep.Title equals title.Id
                                    join gender in _db.Gender on dep.Gender equals gender.Id
                                    where dep.UserId == getUser.UserId
                                    orderby dep.PersonYearOfBirth descending
                                    select new DependantViewModel
                                    {
                                        Id = dep.Id,
                                        UserId = dep.UserId,
                                        PersonName = dep.PersonName,
                                        PersonYearOfBirth = dep.PersonYearOfBirth,
                                        PersonRegNumber = dep.PersonRegNumber,
                                        Title = title.Name, // Fetch Title Name
                                        Gender = gender.GenderName, // Fetch Gender Name
                                        DependentPhoneNumber = dep.Telephone,
                                        DependentEmail = dep.Email
                                    })
                                    .ToListAsync(ct);

            await RecordAuditAsync(getUser, _requestIpHelper.GetRequestIp(), "Dependents", "User navigated to Dependents page.");
            return Json(new { data = dependants });
        }

        private async Task<List<DependantViewModel>> FetchUserDependents(Guid userId, CancellationToken ct)
        {
            return await (from dep in _db.Dependants
                          join title in _db.Title on dep.Title equals title.Id
                          join gender in _db.Gender on dep.Gender equals gender.Id
                          where dep.UserId == userId
                          orderby dep.PersonYearOfBirth descending
                          select new DependantViewModel
                          {
                              Id = dep.Id,
                              PersonName = dep.PersonName,
                              PersonYearOfBirth = dep.PersonYearOfBirth,
                              PersonRegNumber = dep.PersonRegNumber,
                              Title = title.Name,  // Fetch the Title Name
                              Gender = gender.GenderName,  // Fetch the Gender Name
                              DependentPhoneNumber = dep.Telephone,
                              DependentEmail = dep.Email,
                              Person11 = dep.PersonName, // Assuming this field is related
                              Person11YearOfBirth = dep.PersonYearOfBirth,
                              Person11RegNumber = dep.PersonRegNumber,
                              Person2 = dep.PersonName // Assuming this field is related
                          }).ToListAsync(ct);
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Dependents(CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                var adminUser = HttpContext.Session.GetString("adminuser");
                ViewBag.AdminUser = adminUser;
                var getUser = await _userManager.FindByEmailAsync(email);

                if (getUser == null)
                {
                    TempData[SD.Error] = "Error. Please contact Admin";
                    await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Profile", "Error retrieving profile because user can't be found in database.");
                    return RedirectToAction("Login", "Account");
                }

                ViewBag.personRegNumber = getUser.PersonRegNumber;

                // Fetch titles and genders once
                var titles = await _db.Title.OrderBy(a => a.Name).ToListAsync(ct);
                var genders = await _db.Gender.OrderBy(a => a.GenderName).ToListAsync(ct);

                // Fetch dependents for the user
                var dependents = await _db.Dependants
                .Where(a => a.UserId == getUser.UserId && a.IsActive == true) // Ensure only active dependents
                .Select(dep => new
                {
                    Dependent = dep,
                    HasAccount = _db.Users.Any(u => u.PersonRegNumber == dep.PersonRegNumber) // Check if account exists
                })
                .ToListAsync(ct);


                // Prepare the dependent list with associated titles and genders
                var dependentsWithDetails = dependents.Select(d => new Dependant
                {
                    Id = d.Dependent.Id,
                    PersonName = d.Dependent.PersonName,
                    PersonYearOfBirth = d.Dependent.PersonYearOfBirth,
                    Telephone = d.Dependent.Telephone,
                    Email = d.Dependent.Email,
                    PersonRegNumber = d.Dependent.PersonRegNumber,
                    Title = d.Dependent.Title,
                    TitleName = titles.FirstOrDefault(t => t.Id == d.Dependent.Title)?.Name ?? "Unknown",
                    Gender = d.Dependent.Gender,
                    GenderName = genders.FirstOrDefault(g => g.Id == d.Dependent.Gender)?.GenderName ?? "Unknown",
                    HasAccount = d.HasAccount
                }).ToList();

                // Prepare the view model
                DependantViewModel model = new DependantViewModel
                {
                    dependants = dependentsWithDetails
                };

                // Set ViewBags for dropdowns (only if needed in the view)
                ViewBag.DTtile = titles.Select(t => new { t.Id, t.Name }).ToList();
                ViewBag.DGender = genders.Select(g => new { g.Id, g.GenderName }).ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Profile", "User navigated to but had the following error: " + ex.Message);
                HttpContext.Session.Clear();
                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditDependents(int id, CancellationToken ct)
        {
            try
            {

                //Dependent
                var dtitle = await _db.Title.OrderBy(a => a.Name).ToListAsync();
                List<Title> detitle = new();
                detitle = dtitle;

                ViewBag.DTtile = detitle;

                //Gender
                var dgender = await _db.Gender.OrderBy(a => a.GenderName).ToListAsync();
                List<Gender> degender = new();
                degender = dgender;

                ViewBag.DGender = degender;

                return View();
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Profile", "User navigated to but had the following error: " + ex.Message);
                HttpContext.Session.Clear();
                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }
        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dependents(DependantViewModel vm, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");

            try
            {
                // Remove fields from the ModelState that you want to ignore
                ModelState.Remove("PersonRegNumber");
                ModelState.Remove("Person11");
                ModelState.Remove("Person11YearOfBirth");
                ModelState.Remove("Person11RegNumber");
                ModelState.Remove("Person2");
                ModelState.Remove("DependentEmail");
                ModelState.Remove("DependentPhoneNumber");
                ModelState.Remove("DependentTitle");
                ModelState.Remove("DependentGender");
                ModelState.Remove("Title");
                ModelState.Remove("Gender");
                ModelState.Remove("dependants");

                if (ModelState.IsValid)
                {

                    // Get current user
                    var currentUser = await _userManager.FindByEmailAsync(email);
                    if (currentUser == null)
                    {
                        TempData[SD.Error] = "User not found. Please login again.";
                        return RedirectToAction("Login", "Account");
                    }

                    // Find Title and Gender entities based on ID
                    var titleEntity = await _db.Title.FirstOrDefaultAsync(t => t.Id == vm.DependentTitle);
                    var genderEntity = await _db.Gender.FirstOrDefaultAsync(g => g.Id == vm.DependentGender);

                    if (titleEntity == null || genderEntity == null)
                    {
                        TempData[SD.Error] = "Invalid Title or Gender.";
                        return RedirectToAction(nameof(Dependents));
                    }
                    var personName = vm.PersonName?.Trim() ?? "";
                    var duplicateExists = await _db.Dependants.AnyAsync(d =>
                        (
                            d.PersonName == personName &&
                            d.PersonYearOfBirth == vm.PersonYearOfBirth &&
                            d.OutwardPostcode == currentUser.OutwardPostcode
                        )
                        || (!string.IsNullOrWhiteSpace(vm.DependentEmail) && d.Email == vm.DependentEmail)
                        || (!string.IsNullOrWhiteSpace(vm.DependentPhoneNumber) && d.Telephone == vm.DependentPhoneNumber),
                        ct);

                    if (duplicateExists)
                    {
                        var errorMessage = "A family member with similar details already exists. Please check before adding or contact us.";
                        ModelState.AddModelError(string.Empty, errorMessage);

                        await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Dependents",
                            $"Attempted to add duplicate dependent: {personName} for user: {email}", ct);

                        TempData[SD.Error] = errorMessage;
                        return View(vm);
                    }
                    // Generate a unique PersonRegNumber
                    Random _random = new Random();
                    var num = _random.Next(0, 9999).ToString("D4");               
                    string firstNameInitial = personName.Length > 0 ? personName.Split(' ').FirstOrDefault()?.Substring(0, 1).ToUpper() : "X";
                    string lastNameInitial = personName.Length > 0 ? personName.Split(' ').LastOrDefault()?.Substring(0, 1).ToUpper() : "X";

                    var regNumber = $"U{num}{firstNameInitial}{lastNameInitial}";

                    // Prepare dependant for insertion
                    var dep = new Dependant
                    {
                        UserId = currentUser.UserId,                       
                        IsReportedDead = false,
                        PersonName = vm.PersonName,
                        PersonRegNumber = regNumber,
                        Gender = genderEntity.Id,  // Save Gender ID
                        Title = titleEntity.Id,    // Save Title ID
                        Telephone = vm.DependentPhoneNumber,
                        Email = vm.DependentEmail,
                        RegionId = currentUser.RegionId,
                        CityId = currentUser.CityId,
                        PersonYearOfBirth = vm.PersonYearOfBirth,
                        IsActive = true,
                        DateCreated = DateTime.UtcNow,
                        CreatedBy = currentUser.Email,
                        UpdateOn = DateTime.UtcNow
                    };

                    // Save dependant to the database
                    _db.Dependants.Add(dep);
                    await _db.SaveChangesAsync(ct);

                    // Record audit log
                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Dependents", $"Dependent {vm.PersonName} added successfully for user: {email}", ct);

                    TempData[SD.Success] = "Family member added successfully.";
                    return RedirectToAction(nameof(Dependents));
                }

                TempData[SD.Error] = "Please check the form for invalid field(s).";
                return View(vm);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Profile", $"Error updating dependents for user: {email}. Exception: {ex.Message}", ct);
                TempData[SD.Error] = "Please contact Admin.";
                return RedirectToAction(nameof(Dependents));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDependentAccount(DependentAccountViewModel model, CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                var adminUser = HttpContext.Session.GetString("adminuser");
                ViewBag.AdminUser = adminUser;
                var getUser = await _userManager.FindByEmailAsync(email);
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Please ensure all fields are completed correctly.";
                    return RedirectToAction("Dependents");
                }

                if(model.Password != model.ConfirmPassword)
                {
                    TempData["Error"] = "Password do not match.";
                    return RedirectToAction("Dependents");
                }

                // Validate if dependent exists
                var dependent = await _db.Dependants.FirstOrDefaultAsync(d => d.Id == model.DependentId);
                if (dependent == null)
                {
                    TempData["Error"] = "Dependent not found. Please try again.";
                    return RedirectToAction("Dependents");
                }

                var emailExists = await _db.Users.AnyAsync(u => u.Email == model.Email, ct);

                var phoneExists = await _db.Users.AnyAsync(u => u.PhoneNumber == model.Telephone, ct);
                                  

                if (emailExists)
                {
                    TempData[SD.Error] = "This email is already in use. Please provide a different email address.";
                    return RedirectToAction(nameof(Dependents));
                }

                if (phoneExists)
                {
                    TempData[SD.Error] = "This phone number is already associated with another member. Please provide a unique number.";
                    return RedirectToAction(nameof(Dependents));
                }

                // Update the email in the Dependents table if it's different
                if (!string.Equals(dependent.Email, model.Email, StringComparison.OrdinalIgnoreCase))
                {
                    string oldEmail = dependent.Email;
                    dependent.Email = model.Email;
                    _db.Dependants.Update(dependent);

                    // Log the change
                    await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "DependentAccount",
                        $"Email updated for dependent {dependent.PersonName} (ID: {dependent.Id}) from {oldEmail} to {model.Email} by {User.Identity?.Name}.", ct);

                    await _db.SaveChangesAsync();
                }
                // Split PersonName into FirstName and Surname
                string[] nameParts = dependent.PersonName?.Split(' ') ?? new[] { "Unknown", "Dependent" };
                string firstName = nameParts.FirstOrDefault() ?? "Unknown";
                string surname = nameParts.Length > 1 ? nameParts.Last() : "Dependent";

                // Create the new user
                var newUser = new User
                {
                    FirstName = firstName,
                    UserId = dependent.UserId,
                    Surname = surname,
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = dependent.Telephone,
                    PersonYearOfBirth = dependent.PersonYearOfBirth,
                    DateCreated = dependent.DateCreated,
                    CityId = dependent.CityId,
                    RegionId = dependent.RegionId,
                    PersonRegNumber = dependent.PersonRegNumber,
                    DependentId = dependent.Id,
                    CreatedBy = User.Identity?.Name,
                    ForcePasswordChange = false,
                    OutwardPostcode = dependent.OutwardPostcode,
                    SponsorsMemberName = "Account created by Family member account holder"
                };

                var result = await _userManager.CreateAsync(newUser, model.Password);
                if (result.Succeeded)
                {
                 
                    // Ensure roles exist
                    if (!await _roleManager.RoleExistsAsync(RoleList.Dependent))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(RoleList.Dependent));
                    }

                    // Add the user to the "Dependent" role
                    await _userManager.AddToRoleAsync(newUser, RoleList.Dependent);

                    // Generate email confirmation token
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
                    const string pathToFile = @"EmailTemplate/DepRegistration.html";
                    const string subject = "Verify your account";

                    code = HttpUtility.UrlEncode(code);
                    var callbackUrl = Url.Action(
                        "AccountVerified",
                        "Account",
                        new { userId = newUser.Id, code },
                        protocol: Request.Scheme);

                    string htmlBody;

                    using (StreamReader reader = System.IO.File.OpenText(pathToFile))
                    {
                        htmlBody = await reader.ReadToEndAsync(ct);
                        htmlBody = htmlBody.Replace("{{userName}}", $"{newUser.FirstName} {newUser.Surname}")                                        
                                            .Replace("{{callbackUrl}}", "<a href=\"" + callbackUrl + "\">" + "Verify Your Account" + "</a>")
                                           .Replace("{{currentYear}}", DateTime.UtcNow.Year.ToString())
                                           .Replace("{{creator}}", $"{getUser.FirstName + " " + getUser.Surname}");
                    }

                    var message = new PostmarkMessage
                    {
                        To = newUser.Email,
                        Subject = subject,
                        HtmlBody = htmlBody,
                        From = "info@umojawetu.com"
                    };
                    var emailVerification = new EmailVerification
                    {
                        UserId = newUser.UserId,
                        PersonRegNumber = newUser.PersonRegNumber,
                        Token = code,
                        ExpirationTime = DateTime.UtcNow.AddHours(24), // Set expiration time to 24 hours
                        Used = false,
                        DateCreated = DateTime.UtcNow,
                        CreatedBy = User.Identity?.Name ?? "System"
                    };
                    _db.EmailVerifications.Add(emailVerification);
                    await _db.SaveChangesAsync();
                    var emailSent = await _postmark.SendMessageAsync(message, ct);
                    await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "DependentAccount",
                    $"User {User.Identity?.Name} created an account for dependent {dependent.PersonName} with email {model.Email}.", ct);
                    TempData["Success"] = $"Login account created for {dependent.PersonName}. A confirmation email has been sent to {model.Email}.";
                }
                else
                {
                    TempData["Error"] = $"Failed to create account: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }

                return RedirectToAction("Dependents");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while creating the account. Please try again.";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "DependentAccount", $"Error: {ex.Message}");
                return RedirectToAction("Dependents");
            }
        }

        private async Task ReloadDropdowns()
        {
            // Reload dropdowns for Title and Gender
            var dtitle = await _db.Title.OrderBy(a => a.Name).ToListAsync();
            ViewBag.DTtile = dtitle;

            var dgender = await _db.Gender.OrderBy(a => a.GenderName).ToListAsync();
            ViewBag.DGender = dgender;
        }



        [HttpGet]
        [Authorize]
        public async Task<IActionResult> NextOfKin(CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                var getUser = await _db.Users.FirstOrDefaultAsync(a => a.Email == email && a.IsActive == true);
                var adminUser = HttpContext.Session.GetString("adminuser");
                ViewBag.AdminUser = adminUser;

                if (getUser == null)
                {
                    TempData[SD.Error] = "Error. Please contact Admin";
                    await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Profile", "Error retrieving profile because user can't be found in database.");
                    return RedirectToAction("Login", "Account");
                }

                NextOfKinViewModels deps = new NextOfKinViewModels
                {
                    NextOfKins = await _db.NextOfKins
                        .Where(a => a.UserId == getUser.UserId)
                        .OrderBy(a => a.NextOfKinName)
                        .ToListAsync()
                };

                // Get all dependents (for edit functionality)
                var allDependents = await _db.Dependants
                    .Where(a => a.UserId == getUser.UserId && a.IsActive == true)
                    .OrderBy(a => a.PersonName)
                    .ToListAsync();

                // Get dependents who already have Next of Kin assigned
                var dependentIdsWithNextOfKin = deps.NextOfKins
                    .Select(n => n.PersonRegNumber)
                    .ToList();

                // Get only dependents who DON'T have a Next of Kin (for adding new ones)
                ViewBag.Dep = allDependents
                    .Where(d => !dependentIdsWithNextOfKin.Contains(d.PersonRegNumber))
                    .ToList();

                // Create a lookup for the edit modal to properly display member names
                var dependentLookup = allDependents.Select(d => new
                {
                    d.PersonRegNumber,
                    d.PersonName
                }).ToList();

                // Set ViewBags
                ViewBag.DependentsJson = JsonConvert.SerializeObject(ViewBag.Dep); // For Add Modal
                ViewBag.AllDependentsJson = JsonConvert.SerializeObject(dependentLookup); // For Edit Modal

                // Ensure the NextOfKins list gets the correct member names
                foreach (var nextOfKin in deps.NextOfKins)
                {
                    if (!string.IsNullOrEmpty(nextOfKin.PersonRegNumber))
                    {
                        var dep = dependentLookup.FirstOrDefault(d => d.PersonRegNumber == nextOfKin.PersonRegNumber);
                        if (dep != null)
                        {
                            nextOfKin.DepName = dep.PersonName; // Assign correctly
                        }
                    }
                }

                await RecordAuditAsync(getUser, _requestIpHelper.GetRequestIp(), "Next Of Kin", "User navigated to Next of Kin page.");
                return View(deps);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Profile", "User navigated to but had the following error: " + ex.Message);
                HttpContext.Session.Clear();
                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NextOfKin(NextOfKinViewModels vm, CancellationToken ct)
        {
            try
            {

                string myIP = _requestIpHelper.GetRequestIp();
                var email = HttpContext.Session.GetString("loginEmail");

                var currentUser = await _db.Users.FirstOrDefaultAsync(a=>a.Email == email && a.IsActive == true);

                if (currentUser == null)
                {
                    TempData[SD.Error] = "Error. Please contact Admin quoting ProNext01 error code";
                    return RedirectToAction("Login", "Account");
                }

                if (vm.NextOfKinEmail.IsNullOrEmpty())
                {
                    vm.NextOfKinEmail = "";
                }
                var getAllDep = await _db.Dependants.Where(a => a.UserId == currentUser.UserId && a.IsActive == true).ToListAsync();

                var nextofkin = _mapper.Map<NextOfKin>(vm);

                // List of required fields with their error messages
                var requiredFields = new Dictionary<string, string>
                {
                    { vm.PersonRegNumber, "Please select a family member for the Next of Kin." },
                    { vm.NextOfKinName, "Next of Kin Name is required." },
                    { vm.Relationship, "Relationship with Next of Kin is required." },
                    { vm.NextOfKinTel, "Next of Kin Mobile Number is required." },
                    { vm.NextOfKinAddress, "Next of Kin Address is required." }
                };

                                // Check for missing fields and return the first validation error
                                foreach (var field in requiredFields)
                                {
                                    if (string.IsNullOrWhiteSpace(field.Key))
                                    {
                                        TempData[SD.Error] = field.Value;
                                        return RedirectToAction(nameof(NextOfKin));
                                    }
                                }

                                // Set a default value for email if empty
                                vm.NextOfKinEmail ??= "";


                nextofkin.UserId = currentUser.UserId;          
                nextofkin.PersonRegNumber = vm.PersonRegNumber;
                nextofkin.DateCreated = DateTime.UtcNow;                
                nextofkin.CreatedBy = currentUser.Email;
                

                _db.NextOfKins.Add(nextofkin);
                await _db.SaveChangesAsync();

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Next of kin", "Next of kin info added successfully for: ", ct);
                TempData[SD.Success] = "Next of kin added successfully.";
                return RedirectToAction(nameof(NextOfKin));

            

            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Next of Kin", "User tried to add next of kin details but had the following error:" + ex.Message.ToString());
                TempData[SD.Error] = "An error occurred while saving Next of Kin. Please try again or contact support.";
                return RedirectToAction(nameof(NextOfKin));
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> EditNextOfKin(NextOfKinViewModels model, CancellationToken ct)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var nextOfKin = await _db.NextOfKins.FindAsync(model.Id);

                if (nextOfKin == null)
                {
                    TempData[SD.Error] = "Next of Kin not found.";
                    return RedirectToAction(nameof(NextOfKin));
                }

                // Validate Required Fields Before Saving
                var requiredFields = new Dictionary<string, string>
        {
            { model.PersonRegNumber, "Please select a family member for the Next of Kin." },
            { model.NextOfKinName, "Next of Kin Name is required." },
            { model.Relationship, "Relationship with Next of Kin is required." },
            { model.NextOfKinTel, "Next of Kin Mobile Number is required." },           
            { model.NextOfKinAddress, "Next of Kin Address is required." }
        };

                foreach (var field in requiredFields)
                {
                    if (string.IsNullOrWhiteSpace(field.Key))
                    {
                        TempData[SD.Error] = field.Value;
                        return RedirectToAction(nameof(NextOfKin));
                    }
                }
                model.NextOfKinEmail ??= "";
                var changes = new List<NextOfKinChangeLog>();

                // Check if changes were made and log them
                if (nextOfKin.PersonRegNumber != model.PersonRegNumber)
                {
                    changes.Add(new NextOfKinChangeLog
                    {
                        UserId = model.UserId,
                        FieldName = "PersonRegNumber",
                        OldValue = nextOfKin.PersonRegNumber,
                        NewValue = model.PersonRegNumber,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    nextOfKin.PersonRegNumber = model.PersonRegNumber;
                }

                if (nextOfKin.NextOfKinName != model.NextOfKinName)
                {
                    changes.Add(new NextOfKinChangeLog
                    {
                        UserId = model.UserId,
                        FieldName = "NextOfKinName",
                        OldValue = nextOfKin.NextOfKinName,
                        NewValue = model.NextOfKinName,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    nextOfKin.NextOfKinName = model.NextOfKinName;
                }

                if (nextOfKin.Relationship != model.Relationship)
                {
                    changes.Add(new NextOfKinChangeLog
                    {
                        UserId = model.UserId,
                        FieldName = "Relationship",
                        OldValue = nextOfKin.Relationship,
                        NewValue = model.Relationship,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    nextOfKin.Relationship = model.Relationship;
                }

                if (nextOfKin.NextOfKinTel != model.NextOfKinTel)
                {
                    changes.Add(new NextOfKinChangeLog
                    {
                        UserId = model.UserId,
                        FieldName = "NextOfKinTel",
                        OldValue = nextOfKin.NextOfKinTel,
                        NewValue = model.NextOfKinTel,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    nextOfKin.NextOfKinTel = model.NextOfKinTel;
                }

                if (nextOfKin.NextOfKinEmail != model.NextOfKinEmail)
                {
                    changes.Add(new NextOfKinChangeLog
                    {
                        UserId = model.UserId,
                        FieldName = "NextOfKinEmail",
                        OldValue = nextOfKin.NextOfKinEmail,
                        NewValue = model.NextOfKinEmail,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    nextOfKin.NextOfKinEmail = model.NextOfKinEmail;
                }

                if (nextOfKin.NextOfKinAddress != model.NextOfKinAddress)
                {
                    changes.Add(new NextOfKinChangeLog
                    {
                        UserId = model.UserId,
                        FieldName = "NextOfKinAddress",
                        OldValue = nextOfKin.NextOfKinAddress,
                        NewValue = model.NextOfKinAddress,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    nextOfKin.NextOfKinAddress = model.NextOfKinAddress;
                }

                _db.NextOfKins.Update(nextOfKin);
                await _db.SaveChangesAsync();

                if (changes.Any())
                {
                    _db.NextOfKinChangeLogs.AddRange(changes);
                    await _db.SaveChangesAsync();
                }

                TempData[SD.Success] = "Next of Kin updated successfully.";
                return RedirectToAction(nameof(NextOfKin));
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "Error updating Next of Kin.";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Next of Kin", "User tried to update next of kin details but had the following error: " + ex.Message);
                return RedirectToAction(nameof(NextOfKin));
            }
        }



        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditProfile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData[SD.Error] = "Invalid input.";
                return RedirectToAction("Index");
            }

            try
            {
                var getUser = await _db.Users.FirstOrDefaultAsync(a => a.PersonRegNumber == model.PersonRegNumber && a.IsActive == true);
                if (getUser == null)
                {
                    TempData[SD.Error] = "User not found.";
                    return RedirectToAction("Index");
                }

                var changes = new List<UserProfileChangeLog>();
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (getUser.FirstName != model.FirstName)
                {
                    changes.Add(new UserProfileChangeLog
                    {
                        UserId = model.UserId,
                        FieldName = "FirstName",
                        OldValue = getUser.FirstName,
                        NewValue = model.FirstName,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    getUser.FirstName = model.FirstName;
                }

                if (getUser.Surname != model.Surname)
                {
                    changes.Add(new UserProfileChangeLog
                    {
                        UserId = model.UserId,
                        FieldName = "Surname",
                        OldValue = getUser.Surname,
                        NewValue = model.Surname,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    getUser.Surname = model.Surname;
                }

                if (getUser.Title != model.Title)
                {
                    changes.Add(new UserProfileChangeLog
                    {
                        UserId = model.UserId,
                        FieldName = "Title",
                        OldValue = getUser.Title.ToString(),
                        NewValue = model.Title.ToString(),
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    getUser.Title = model.Title;
                }

                if (getUser.PhoneNumber != model.PhoneNumber)
                {
                    changes.Add(new UserProfileChangeLog
                    {
                        UserId = model.UserId,
                        FieldName = "PhoneNumber",
                        OldValue = getUser.PhoneNumber,
                        NewValue = model.PhoneNumber,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    getUser.PhoneNumber = model.PhoneNumber;
                }

                if (getUser.Email != model.Email)
                {
                    changes.Add(new UserProfileChangeLog
                    {
                        UserId = model.UserId,
                        FieldName = "Email",
                        OldValue = getUser.Email,
                        NewValue = model.Email,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    getUser.Email = model.Email;
                }

                if (getUser.RegionId != model.RegionId)
                {
                    changes.Add(new UserProfileChangeLog
                    {
                        UserId = model.UserId,
                        FieldName = "RegionId",
                        OldValue = getUser.RegionId.ToString(),
                        NewValue = model.RegionId.ToString(),
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    getUser.RegionId = model.RegionId;
                }

                if (getUser.CityId != model.CityId)
                {
                    changes.Add(new UserProfileChangeLog
                    {
                        UserId = model.UserId,
                        FieldName = "CityId",
                        OldValue = getUser.CityId.ToString(),
                        NewValue = model.CityId.ToString(),
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    getUser.CityId = model.CityId;
                }
                if (getUser.OutwardPostcode != model.OutwardPostcode)
                {
                    changes.Add(new UserProfileChangeLog
                    {
                        UserId = model.UserId,
                        FieldName = "OutwardPostcode",
                        OldValue = getUser.OutwardPostcode.ToString(),
                        NewValue = model.OutwardPostcode.ToString(),
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    getUser.OutwardPostcode = model.OutwardPostcode;
                }
                if (getUser.PersonYearOfBirth != model.PersonYearOfBirth)
                {
                    changes.Add(new UserProfileChangeLog
                    {
                        UserId = model.UserId,
                        FieldName = "PersonYearOfBirth",
                        OldValue = getUser.PersonYearOfBirth.ToString(),
                        NewValue = model.PersonYearOfBirth.ToString(),
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    getUser.PersonYearOfBirth = model.PersonYearOfBirth;
                }


                getUser.PersonYearOfBirth = model.PersonYearOfBirth;
                getUser.OutwardPostcode = model.OutwardPostcode;

                var result = await _userManager.UpdateAsync(getUser);
                await _db.SaveChangesAsync();
                if (result.Succeeded)
                {
                    if (changes.Any())
                    {
                        _db.UserProfileChangeLog.AddRange(changes);
                        await _db.SaveChangesAsync();
                    }

                    var na = model.FirstName + " " + model.Surname;

                    var dep = await _db.Dependants.FirstOrDefaultAsync(a => a.PersonRegNumber == model.PersonRegNumber && a.IsActive == true);

                    if (dep == null)
                    {
                        Random _random = new Random();
                        string fullName = model.FirstName + " " + model.Surname;
                        string[] names = fullName.Split(' ');
                        string name = names.First();
                        string lasName = names.Last();
                        var num = _random.Next(0, 9999).ToString("D4");

                        Dependant addDep = new Dependant
                        {
                            UserId = model.UserId,
                            PersonName = model.FirstName + " " + model.Surname ?? "",
                            PersonRegNumber = "U" + num + model.FirstName.Trim().Substring(0, 1) + model.Surname.Trim().Substring(0, 1),
                            PersonYearOfBirth = model.PersonYearOfBirth,
                            DateCreated = DateTime.UtcNow,
                            CreatedBy = model.Email,
                            UpdateOn = DateTime.UtcNow,
                            IsActive = false
                        };

                        _db.Dependants.Add(addDep);
                        await _db.SaveChangesAsync();
                    }
                    else
                    {
                        dep.PersonYearOfBirth = model.PersonYearOfBirth;
                        dep.OutwardPostcode = model.OutwardPostcode;
                        dep.Title = model.Title;
                        dep.RegionId = model.RegionId;
                        dep.CityId = model.CityId;
                        dep.Telephone = model.PhoneNumber;
                        dep.Email = model.Email;
                        _db.Dependants.Update(dep);
                        await _db.SaveChangesAsync();
                    }

                    TempData[SD.Success] = "Profile updated successfully.";
                }
                else
                {
                    TempData[SD.Error] = "Error updating profile.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "Error updating profile.";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Profile", "Error updating profile: " + ex.Message);
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDependent(DependantViewModel model)
        {
            try
            {
                // Fetch the dependent from the database
                var dependent = await _db.Dependants.FirstOrDefaultAsync(d => d.Id == model.Id);
                if (dependent == null)
                {
                    TempData[SD.Error] = "Dependent not found.";
                    return RedirectToAction(nameof(Dependents));
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var changes = new List<DependentChangeLog>();

                // Update and log changes for each field if different from the existing value

                // Update Title
                if (dependent.Title != model.DependentTitle)
                {
                    var oldTitleName = await _db.Title.Where(t => t.Id == dependent.Title)
                                                      .Select(t => t.Name).FirstOrDefaultAsync();
                    var newTitleName = await _db.Title.Where(t => t.Id == model.DependentTitle)
                                                      .Select(t => t.Name).FirstOrDefaultAsync();
                    changes.Add(new DependentChangeLog
                    {
                        UserId = dependent.UserId,
                        DependentId = model.Id,
                        FieldName = "Title",
                        OldValue = oldTitleName ?? "",
                        NewValue = newTitleName ?? "",
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    dependent.Title = model.DependentTitle;
                }

                // Update Gender
                if (dependent.Gender != model.DependentGender)
                {
                    var oldGenderName = await _db.Gender.Where(g => g.Id == dependent.Gender)
                                                        .Select(g => g.GenderName).FirstOrDefaultAsync();
                    var newGenderName = await _db.Gender.Where(g => g.Id == model.DependentGender)
                                                        .Select(g => g.GenderName).FirstOrDefaultAsync();
                    changes.Add(new DependentChangeLog
                    {
                        UserId = dependent.UserId,
                        DependentId = model.Id,
                        FieldName = "Gender",
                        OldValue = oldGenderName ?? "",
                        NewValue = newGenderName ?? "",
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    dependent.Gender = model.DependentGender;
                }

                // Update PersonName
                if (dependent.PersonName != model.PersonName)
                {
                    changes.Add(new DependentChangeLog
                    {
                        UserId = dependent.UserId,
                        DependentId = model.Id,
                        FieldName = "PersonName",
                        OldValue = dependent.PersonName,
                        NewValue = model.PersonName,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    dependent.PersonName = model.PersonName;
                }

                // Update Year of Birth
                if (dependent.PersonYearOfBirth != model.PersonYearOfBirth)
                {
                    changes.Add(new DependentChangeLog
                    {
                        UserId = dependent.UserId,
                        DependentId = model.Id,
                        FieldName = "YearOfBirth",
                        OldValue = dependent.PersonYearOfBirth.ToString(),
                        NewValue = model.PersonYearOfBirth.ToString(),
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    dependent.PersonYearOfBirth = model.PersonYearOfBirth;
                }

                // Update Phone Number
                if (dependent.Telephone != model.DependentPhoneNumber)
                {
                    changes.Add(new DependentChangeLog
                    {
                        UserId = dependent.UserId,
                        DependentId = model.Id,
                        FieldName = "Telephone",
                        OldValue = dependent.Telephone ?? "",
                        NewValue = model.DependentPhoneNumber ?? "",
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    dependent.Telephone = model.DependentPhoneNumber;
                }

                // Update Email
                if (dependent.Email != model.DependentEmail)
                {
                    changes.Add(new DependentChangeLog
                    {
                        UserId = dependent.UserId,
                        DependentId = model.Id,
                        FieldName = "Email",
                        OldValue = dependent.Email ?? "",
                        NewValue = model.DependentEmail ?? "",
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    dependent.Email = model.DependentEmail;
                }

                // Update and save the dependent details in the database
                _db.Dependants.Update(dependent);
                await _db.SaveChangesAsync();

                // Log changes if any fields were modified
                if (changes.Any())
                {
                    _db.DependentChangeLogs.AddRange(changes);
                    await _db.SaveChangesAsync();
                }

                TempData[SD.Success] = "Dependent updated successfully.";
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "Error updating dependent.";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Profile", $"Error updating dependent: {ex.Message}");
            }

            return RedirectToAction(nameof(Dependents));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UpdateNextOfKin(NextOfKinViewModels model)
        {
            // Remove the NextOfKins property from the model state
            ModelState.Remove(nameof(model.NextOfKins));
            if (!ModelState.IsValid)
            {
                TempData[SD.Error] = "Invalid input.";
                return RedirectToAction("NextOfKin");
            }

            try
            {
                var nextOfKin = await _db.NextOfKins.FirstOrDefaultAsync(n => n.Id == model.Id);
                if (nextOfKin == null)
                {
                    TempData[SD.Error] = "Next of Kin not found.";
                    return RedirectToAction("NextOfKin");
                }

                var changes = new List<NextOfKinChangeLog>();
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Track changes for audit
                if (nextOfKin.NextOfKinName != model.NextOfKinName)
                {
                    changes.Add(new NextOfKinChangeLog
                    {
                        UserId = nextOfKin.UserId,
                        FieldName = "NextOfKinName",
                        OldValue = nextOfKin.NextOfKinName ?? string.Empty,
                        NewValue = model.NextOfKinName ?? string.Empty,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    nextOfKin.NextOfKinName = model.NextOfKinName;
                }

                if (nextOfKin.Relationship != model.Relationship)
                {
                    changes.Add(new NextOfKinChangeLog
                    {
                        UserId = nextOfKin.UserId,
                        FieldName = "Relationship",
                        OldValue = nextOfKin.Relationship ?? string.Empty,
                        NewValue = model.Relationship ?? string.Empty,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    nextOfKin.Relationship = model.Relationship;
                }

                if (nextOfKin.NextOfKinTel != model.NextOfKinTel)
                {
                    changes.Add(new NextOfKinChangeLog
                    {
                        UserId = nextOfKin.UserId,
                        FieldName = "NextOfKinTel",
                        OldValue = nextOfKin.NextOfKinTel ?? string.Empty,
                        NewValue = model.NextOfKinTel ?? string.Empty,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    nextOfKin.NextOfKinTel = model.NextOfKinTel;
                }

                if (nextOfKin.NextOfKinEmail != model.NextOfKinEmail)
                {
                    changes.Add(new NextOfKinChangeLog
                    {
                        UserId = nextOfKin.UserId,
                        FieldName = "NextOfKinEmail",
                        OldValue = nextOfKin.NextOfKinEmail ?? string.Empty,
                        NewValue = model.NextOfKinEmail ?? string.Empty,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    nextOfKin.NextOfKinEmail = model.NextOfKinEmail;
                }

                if (nextOfKin.NextOfKinAddress != model.NextOfKinAddress)
                {
                    changes.Add(new NextOfKinChangeLog
                    {
                        UserId = nextOfKin.UserId,
                        FieldName = "NextOfKinAddress",
                        OldValue = nextOfKin.NextOfKinAddress ?? string.Empty,
                        NewValue = model.NextOfKinAddress ?? string.Empty,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    nextOfKin.NextOfKinAddress = model.NextOfKinAddress;
                }

                // Update DependentId if changed
                if (nextOfKin.PersonRegNumber != model.PersonRegNumber)
                {
                    changes.Add(new NextOfKinChangeLog
                    {
                        UserId = nextOfKin.UserId,
                        FieldName = "PersonRegNumber",
                        OldValue = nextOfKin.PersonRegNumber.ToString() ?? string.Empty,
                        NewValue = model.PersonRegNumber.ToString() ?? string.Empty,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserId
                    });
                    nextOfKin.PersonRegNumber = model.PersonRegNumber;
                }

                // Save changes
                _db.NextOfKins.Update(nextOfKin);
                await _db.SaveChangesAsync();

                // Save change history
                if (changes.Any())
                {
                    _db.NextOfKinChangeLogs.AddRange(changes);
                    await _db.SaveChangesAsync();
                }

                // Get member name for audit
                var memberName = await _db.Dependants
                    .Where(d => d.PersonRegNumber == model.PersonRegNumber)
                    .Select(d => d.PersonName)
                    .FirstOrDefaultAsync();

                // Record audit
                var currentUser = await _userManager.GetUserAsync(User);
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "NextOfKin", $"Updated Next of Kin (Id: {model.Id}), member name: {memberName}.");

                TempData[SD.Success] = "Next of Kin updated successfully.";
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "Error updating Next of Kin.";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Profile", "Error updating Next of Kin: " + ex.Message);
            }

            return RedirectToAction("NextOfKin");
        }

        [HttpPost]     
        public async Task<IActionResult> DeleteNextOfKin(int id)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                var currentUser = await _userManager.FindByEmailAsync(email);

                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var nextOfKin = await _db.NextOfKins.FindAsync(id);
                if (nextOfKin == null)
                {
                    return Json(new { success = false, message = "Next of Kin not found." });
                }

                // Create changelog entry before deleting
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var changeLog = new NextOfKinChangeLog
                {
                    UserId = nextOfKin.UserId,
                    FieldName = "Deleted",
                    OldValue = nextOfKin.NextOfKinName,
                    NewValue = "Deleted Value", // Replace null with "Deleted" or another descriptive value
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = currentUserId
                };

                _db.NextOfKinChangeLogs.Add(changeLog);
                await _db.SaveChangesAsync();

                // Remove the record
                _db.NextOfKins.Remove(nextOfKin);
                await _db.SaveChangesAsync();

                // Record audit log
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "NextOfKin", $"Deleted Next of Kin: {nextOfKin.NextOfKinName}");
                TempData[SD.Success] = "Next of kin deleted successfully.";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Log error
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "NextOfKin", $"Error deleting Next of Kin: {ex.Message}");
                return Json(new { success = false, message = "Error occurred while deleting Next of Kin." });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Successor(CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                var getUser = await _userManager.FindByEmailAsync(email);
                var adminUser = HttpContext.Session.GetString("adminuser");
                ViewBag.AdminUser = adminUser;
                if (getUser == null)
                {
                    TempData[SD.Error] = "Error. Please contact Admin";
                    await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Profile", "Error retrieving profile because user can't be found in the database.");
                    return RedirectToAction("Login", "Account");
                }

                var model = new SuccessorViewModel
                {
                    Successors = await _db.Successors
                        .Where(s => s.UserId == getUser.UserId)
                        .OrderBy(s => s.Name)
                        .ToListAsync(ct)
                };

                // Get all dependents belonging to the user
                var allDependents = await _db.Dependants
                    .Where(d => d.UserId == getUser.UserId)
                    .OrderBy(d => d.PersonName)
                    .ToListAsync(ct);

                // Add all dependents to ViewBag.Dep
                ViewBag.Dep = allDependents;
                ViewBag.HasSuccessor = model.Successors.Any(); // Pass flag to view

                await RecordAuditAsync(getUser, _requestIpHelper.GetRequestIp(), "Successor", "User navigated to Successor page.");
                return View(model);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Profile", "User navigated to but had the following error:" + ex.Message);
                HttpContext.Session.Clear();
                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Successor(SuccessorViewModel model, CancellationToken ct)
        {
            try
            {
                string myIP = _requestIpHelper.GetRequestIp();
                var email = HttpContext.Session.GetString("loginEmail");
                var currentUser = await _userManager.FindByEmailAsync(email);

                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                if (string.IsNullOrEmpty(model.Email))
                {
                    model.Email = string.Empty;
                }

                var dpe = await _db.Dependants.FirstOrDefaultAsync(a => a.Id == model.DependentId);

           
                if (dpe == null)
                {
                    TempData[SD.Error] = "Dependent not found.";
                    return RedirectToAction(nameof(Successor));
                }
                var getUser = await _db.Users.FirstOrDefaultAsync(a => a.UserId == dpe.UserId);
                if (getUser == null)
                {
                    TempData[SD.Error] = "User not found.";
                    return RedirectToAction(nameof(Successor));
                }

                model.Name = dpe.PersonName;
                // Create a new Successor entry
                var newSuccessor = new Successor
                {
                    UserId = currentUser.UserId,
                    DependentId = model.DependentId,
                    Name = model.Name,
                    Relationship = model.Relationship,
                    SuccessorTel = model.SuccessorTel,
                    SuccessorEmail = model.Email,
                    Status = SuccessorStatus.AwaitingEmailConfirmation, // Initial status
                    DateCreated = DateTime.UtcNow,
                    CreatedBy = currentUser.Email,
                    IsTakeOver = false // Successor has not taken over initially
                };

                _db.Successors.Add(newSuccessor);
                await _db.SaveChangesAsync(ct);

                //update the successor to the user
                getUser.SuccessorId = newSuccessor.Id;
                _db.Users.Update(getUser);
                await _db.SaveChangesAsync(ct);

                // Send email for confirmation
                var userId = newSuccessor.Id.ToString(); // Use Successor ID for link
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(currentUser);
                code = HttpUtility.UrlEncode(code);

                // Generate confirmation and decline URLs
                //var confirmationUrl = Url.Action("ConfirmSuccessor", "Account", new { userId, code }, protocol: Request.Scheme);
                //var declineUrl = Url.Action("DeclineSuccessor", "Account", new { userId, code }, protocol: Request.Scheme);
                
                var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{newSuccessor.Id}:{getUser.UserId}:{Guid.NewGuid()}"));
                var encodedToken = HttpUtility.UrlEncode(token);

                var confirmationUrl = Url.Action("ConfirmSuccessor", "Account", new { userId = newSuccessor.Id.ToString(), code = encodedToken }, protocol: Request.Scheme);
                var declineUrl = Url.Action("DeclineSuccessor", "Account", new { userId = newSuccessor.Id.ToString(), code = encodedToken }, protocol: Request.Scheme);

                // Load email template and replace placeholders
                const string pathToFile = @"EmailTemplate/SuccessorNomination.html";
                string htmlBody = await System.IO.File.ReadAllTextAsync(pathToFile, ct);
                htmlBody = htmlBody.Replace("{{successorName}}", newSuccessor.Name)
                                   .Replace("{{accountHolderName}}", getUser.FirstName + " " + getUser.Surname)
                                   .Replace("{{currentYear}}", DateTime.UtcNow.Year.ToString())
                                   .Replace("{{confirmationLink}}", $"<a href=\"{confirmationUrl}\">Confirm Successor Nomination</a>")
                                   .Replace("{{declineLink}}", $"<a href=\"{declineUrl}\">Decline Successor Nomination</a>");

                // Send email
                var message = new PostmarkMessage
                {
                    To = model.Email,
                    Subject = "Confirmation Required: Successor Nomination",
                    HtmlBody = htmlBody,
                    From = "info@umojawetu.com"
                };

                var emailSent = await _postmark.SendMessageAsync(message, ct).ConfigureAwait(false);

                TempData[SD.Success] = "Successor nomination sent. Awaiting email confirmation.";
                return RedirectToAction(nameof(Successor));
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Successor", $"User tried to add successor details but encountered an error: {ex.Message}");
                TempData[SD.Error] = "Error - please contact Admin.";
                return RedirectToAction(nameof(Successor));
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UpdateSuccessor(SuccessorViewModel model, CancellationToken ct)
        {
            try
            {
                string myIP = _requestIpHelper.GetRequestIp();
                var email = HttpContext.Session.GetString("loginEmail");
                var currentUser = await _userManager.FindByEmailAsync(email);

                if (currentUser == null)
                {
                    TempData[SD.Error] = "Error - please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                if (string.IsNullOrEmpty(model.Email))
                {
                    model.Email = string.Empty;
                }


                var existingSuccessor = await _db.Successors.FirstOrDefaultAsync(s => s.Id == model.Id, ct);

                if (existingSuccessor == null)
                {
                    TempData[SD.Error] = "Successor not found.";
                    return RedirectToAction(nameof(Successor));
                }

                var dpe = await _db.Dependants.FirstOrDefaultAsync(a => a.Id == model.DependentId);

                if (dpe == null)
                {
                    TempData[SD.Error] = "Dependent not found.";
                    return RedirectToAction(nameof(Successor));
                }

                // Update the Successor details
                existingSuccessor.DependentId = model.DependentId;
                existingSuccessor.Name = dpe.PersonName; // Update with dependent's name
                existingSuccessor.Relationship = model.Relationship;
                existingSuccessor.SuccessorTel = model.SuccessorTel;
                existingSuccessor.SuccessorEmail = model.Email;              
                existingSuccessor.UpdateOn = DateTime.UtcNow;
               

                _db.Successors.Update(existingSuccessor);
                await _db.SaveChangesAsync(ct);


                var getUser = await _db.Users.FirstOrDefaultAsync(a => a.UserId == dpe.UserId);
                if (getUser == null)
                {
                    TempData[SD.Error] = "User not found.";
                    return RedirectToAction(nameof(Successor));
                }
                // Optionally send an email if important information like email is changed
                if (existingSuccessor.SuccessorEmail != model.Email)
                {
                    var userId = existingSuccessor.Id.ToString();
            

                    var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{existingSuccessor.Id}:{getUser.UserId}:{Guid.NewGuid()}"));
                    var encodedToken = HttpUtility.UrlEncode(token);

                    var confirmationUrl = Url.Action("ConfirmSuccessor", "Account", new { userId = existingSuccessor.Id.ToString(), code = encodedToken }, protocol: Request.Scheme);
                    var declineUrl = Url.Action("DeclineSuccessor", "Account", new { userId = existingSuccessor.Id.ToString(), code = encodedToken }, protocol: Request.Scheme);


                    const string pathToFile = @"EmailTemplate/SuccessorNomination.html";
                    string htmlBody = await System.IO.File.ReadAllTextAsync(pathToFile, ct);
                    htmlBody = htmlBody.Replace("{{successorName}}", existingSuccessor.Name)
                                       .Replace("{{accountHolderName}}", getUser.FirstName + " " + getUser.Surname)
                                       .Replace("{{currentYear}}", DateTime.UtcNow.Year.ToString())
                                       .Replace("{{confirmationLink}}", $"<a href=\"{confirmationUrl}\">Confirm Successor Nomination</a>")
                                       .Replace("{{declineLink}}", $"<a href=\"{declineUrl}\">Decline Successor Nomination</a>");

                    var message = new PostmarkMessage
                    {
                        To = model.Email,
                        Subject = "Confirmation Required: Successor Nomination",
                        HtmlBody = htmlBody,
                        From = "info@umojawetu.com"
                    };

                    await _postmark.SendMessageAsync(message, ct).ConfigureAwait(false);
                }

                TempData[SD.Success] = "Successor details updated successfully.";
                return RedirectToAction(nameof(Successor));
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Successor", $"User tried to update successor details but encountered an error: {ex.Message}");
                TempData[SD.Error] = "Error - please contact Admin.";
                return RedirectToAction(nameof(Successor));
            }
        }


        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmSuccessor(string userId, string code)
        {
            try
            {
                var currentUser = await _db.Users.Where(a => a.Id == userId).FirstOrDefaultAsync();
                if (currentUser == null)
                {
                    TempData[SD.Error] = "Invalid confirmation link.";
                    return RedirectToAction("Login", "Account");
                }

                var successor = await _db.Successors.FirstOrDefaultAsync(s => s.UserId == currentUser.UserId && s.Status == SuccessorStatus.AwaitingEmailConfirmation);
                if (successor == null)
                {
                    TempData[SD.Error] = "Invalid confirmation link.";
                    return RedirectToAction("Login", "Account");
                }

                // Confirm email token (this should be done to ensure the link is valid)
                var result = await _userManager.ConfirmEmailAsync(currentUser, code);
                if (!result.Succeeded)
                {
                    TempData[SD.Error] = "Error confirming email.";
                    return RedirectToAction("Login", "Account");
                }

                successor.Status = SuccessorStatus.Approved;
                successor.UpdateOn = DateTime.UtcNow;

                _db.Successors.Update(successor);
                await _db.SaveChangesAsync();

                TempData[SD.Success] = "Successor nomination confirmed.";
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "Error confirming successor.";
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> DeclineSuccessor(string userId, string code)
        {
            try
            {
                var currentUser = await _db.Users.Where(a => a.Id == userId).FirstOrDefaultAsync();
                if (currentUser == null)
                {
                    TempData[SD.Error] = "Invalid confirmation link.";
                    return RedirectToAction("Login", "Account");
                }

                var successor = await _db.Successors.FirstOrDefaultAsync(s => s.UserId == currentUser.UserId && s.Status == SuccessorStatus.AwaitingEmailConfirmation);
                if (successor == null)
                {
                    TempData[SD.Error] = "Invalid confirmation link.";
                    return RedirectToAction("Login", "Account");
                }

                // Confirm email token (this should be done to ensure the link is valid)
                var result = await _userManager.ConfirmEmailAsync(currentUser, code);
                if (!result.Succeeded)
                {
                    TempData[SD.Error] = "Error confirming email.";
                    return RedirectToAction("Login", "Account");
                }
                            

                successor.Status = SuccessorStatus.Declined;
                successor.UpdateOn = DateTime.UtcNow;

                _db.Successors.Update(successor);
                await _db.SaveChangesAsync();

                TempData[SD.Success] = "Successor nomination declined.";
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "Error declining successor.";
                return RedirectToAction("Login", "Account");
            }
        }


        public async Task<IActionResult> TakeOverAccount(int successorId, CancellationToken ct)
        {
            var successor = await _db.Successors.FindAsync(successorId);
            if (successor == null || !successor.IsTakeOver)
            {
                TempData[SD.Error] = "Invalid or unconfirmed successor.";
                return RedirectToAction("Index", "Home");
            }

            // Perform account transfer tasks
            successor.TakeOverDate = DateTime.UtcNow;
            successor.Status = "Active";

            // Update user record to reflect successor ownership if necessary
            // ...

            _db.Successors.Update(successor);
            await _db.SaveChangesAsync(ct);

            TempData[SD.Success] = "Successor has successfully taken over the account.";
            return RedirectToAction("Index", "Home");
        }

    }
}


