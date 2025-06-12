namespace WebOptimus.Controllers
{
    using AutoMapper;
    using DocumentFormat.OpenXml.Spreadsheet;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Web;
    using WebOptimus.Configuration;
    using WebOptimus.Controllers;
    using WebOptimus.Data;
    using WebOptimus.Helpers;
    using WebOptimus.Migrations;
    using WebOptimus.Models;
    using WebOptimus.Models.ViewModel;
    using WebOptimus.Models.ViewModel.UserVM;
    using WebOptimus.Services;
    using WebOptimus.StaticVariables;


    public class AccountController : BaseController
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


        public AccountController(IMapper mapper, UserManager<User> userManager,
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
        public async Task<IActionResult> Login()
        {
            try
            {
                ViewBag.HasPublicAnnouncements = await _db.Announcements.AnyAsync(a => a.IsActiveToPublic == true);

                var isAdminUser = HttpContext.Session.GetString("adminuser");
                var email = HttpContext.Session.GetString("loginEmail");
                if (email != null)
                {
                    if (isAdminUser == RoleList.GeneralAdmin)
                    {

                        return RedirectToAction("Index", "Admin");

                    }
                    else
                    {

                        return RedirectToAction("Index", "Account");

                    }
                }
                else
                {
                    await RecordAuditAsync("Navigation", "User Navigated to Login page");
                    return View();

                }

            }
            catch
            {
                HttpContext.Session.Clear();
                return View();

            }




        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel viewModel, CancellationToken ct)
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.Email == viewModel.Email && a.IsActive == true);

                    if (currentUser == null)
                    {
                        await RecordAuditAsync(null, viewModel.Email, _requestIpHelper.GetRequestIp(), "Login", "User Account", "User login failed because user could not be found in database.", ct);
                        TempData[SD.Error] = "The login details you entered are incorrect. Please try again.";
                        return RedirectToAction(nameof(Login));
                    }
                    else if (currentUser.EmailConfirmed == false)
                    {
                        await RecordAuditAsync(null, viewModel.Email, _requestIpHelper.GetRequestIp(), "Login", "User with email: " + viewModel.Email + " tried to login but their Email address is not yet verified", ct);
                        TempData[SD.Error] = "Please verify your email address to proceed.";
                        return RedirectToAction(nameof(Login));
                    }
                    else if (currentUser.IsActive == false)
                    {
                        await RecordAuditAsync(null, viewModel.Email, _requestIpHelper.GetRequestIp(), "Login", "User with email: " + viewModel.Email + " tried to login but the account is deactivated.", ct);
                        TempData[SD.Error] = "Your account has been temporarily suspendend. Please contact us for further assistance.";

                        return RedirectToAction(nameof(Login));
                    }
                    var result = await _signInManager.PasswordSignInAsync(viewModel.Email, viewModel.Password, viewModel.RememberMe, lockoutOnFailure: true);

                    if (result.Succeeded)
                    {
                        if (currentUser.IsDeceased == true)
                        {
                            await RecordAuditAsync(null, viewModel.Email, _requestIpHelper.GetRequestIp(), "Login", "User with email: " + viewModel.Email + " tried to login but they are marked as deceased.", ct);
                            TempData[SD.Error] = "Sorry, you cannot login because this account user has been reported as deceased. If this is not the case, please raised it with Admin as soon as possible.";
                            return RedirectToAction(nameof(Login));
                        }

                        // Original logic: notifications and sessions
                        var activeNotifications = await _db.PopUpNotification.Where(n => n.IsActive).ToListAsync(ct);
                        foreach (var activeNotification in activeNotifications)
                        {
                            var userNotification = await _db.UserNotification
                                .FirstOrDefaultAsync(un => un.UserId == currentUser.UserId && un.NotificationId == activeNotification.Id, ct);

                            if (userNotification == null || userNotification.ViewCount < 3)
                            {
                                if (userNotification == null)
                                {
                                    userNotification = new UserNotification
                                    {
                                        UserId = currentUser.UserId,
                                        NotificationId = activeNotification.Id,
                                        UserEmail = currentUser.Email,
                                        ViewCount = 1
                                    };
                                    _db.UserNotification.Add(userNotification);
                                }
                                else
                                {
                                    userNotification.ViewCount += 1;
                                    _db.UserNotification.Update(userNotification);
                                }
                                await _db.SaveChangesAsync(ct);
                                TempData[$"ActiveNotification_{activeNotification.Id}"] = activeNotification.Description;
                            }
                        }

                        // Set session details
                        var firstNameOnly = currentUser.FirstName.Split(' ')[0];
                        var currentLoginUserRole = await _userManager.GetRolesAsync(currentUser);
                        HttpContext.Session.SetString("adminuser", currentLoginUserRole[0].ToString());
                        HttpContext.Session.SetString("userId", currentUser.UserId.ToString());
                        HttpContext.Session.SetString("loginEmail", viewModel.Email);
                        HttpContext.Session.SetString("userFirstName", firstNameOnly);
                        HttpContext.Session.SetString("isGeorge", currentUser.Email);

                        if (currentUser.ForcePasswordChange == true)
                        {
                            var encryptedReg = protector.Protect(currentUser.PersonRegNumber);                         
                           
                            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Redirect", "Redirected to ForcePasswordChange page: "+ encryptedReg, ct);
                            return RedirectToAction(nameof(ForcePasswordChange), "Account", new { reg = encryptedReg });

                        }

                        await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Login", "User login successfully", ct);

                        var userRole = HttpContext.Session.GetString("adminuser");
                        if (userRole == RoleList.GeneralAdmin || userRole == RoleList.LocalAdmin || userRole == RoleList.RegionalAdmin)
                        {
                            return RedirectToAction("Index", "Admin");
                        }
                        else
                        {
                            return RedirectToAction("Dashboard", "Home");
                        }
                    }
                    else if (result.IsNotAllowed)
                    {
                        await RecordAuditAsync("Login", "User Account", "Email address not verified", ct);
                        TempData[SD.Error] = "Please verify your email address to proceed.";
                        return View();
                    }
                    else if (result.IsLockedOut)
                    {
                        currentUser.LockoutEnd = DateTime.Now.AddYears(1000);
                        currentUser.LockoutEnabled = false;
                        await _db.SaveChangesAsync(ct);
                        await RecordAuditAsync("Login", "User Account", "Max login attempts, user account " + currentUser.Email + " has been locked.", ct);
                        TempData[SD.Error] = "Max login attempts, your account has been locked.";
                        return View();
                    }
                    else
                    {
                        await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Login", "User Account", "User login failed - invalid credentials.", ct);
                        TempData[SD.Error] = "The login details you entered are incorrect. Please try again.";
                        return View();
                    }
                }
                catch (SqlException exception)
                {
                    await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Login", "User Account", "User login failed - because: " + exception.Message, ct);
                    TempData[SD.Error] = "The login details you entered are incorrect. Please try again.";
                    return View();
                }
            }
            return RedirectToAction("Index", "Home");
        }




        [HttpPost]
        [Route("ValidatePassword")]
        public async Task<IActionResult> ValidatePassword([FromBody] LoginViewModel viewModel, CancellationToken ct)
        {


            var currentUser = await _userManager.FindByEmailAsync(viewModel.Email);
            if (currentUser == null)
            {
                await RecordAuditAsync(null, viewModel.Email, _requestIpHelper.GetRequestIp(), "API Login", "User Account", "User login failed because user could not be found in database.", ct);
                return Unauthorized("Invalid login details.");
            }

            //if (!currentUser.EmailConfirmed)
            //{
            //    await RecordAuditAsync(null, viewModel.Email, _requestIpHelper.GetRequestIp(), "API Login", "User with email:  " + viewModel.Email + " tried to login but their Email address is not yet verified", ct);
            //    return Unauthorized("Please verify your email address.");
            //}

            var result = await _signInManager.CheckPasswordSignInAsync(currentUser, viewModel.Password, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Login", "User login successfully", ct);
                return Ok("Password is valid.");
            }
            //else if (result.IsLockedOut)
            //{
            //    await RecordAuditAsync("Login", "User Account", "Max login attempts, user account " + currentUser.Email + " has been locked.", ct);
            //    return Unauthorized("Max login attempts, your account has been locked.");
            //}
            //else if (result.IsNotAllowed)
            //{
            //    return Unauthorized("Please verify your email address.");
            //}
            else
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Login", "User Account", "User login failed - invalid credentials.", ct);
                return Unauthorized("Invalid login details.");
            }
        }
        [HttpGet]
        public async Task<IActionResult> Register(CancellationToken ct)
        {
            ViewBag.HasPublicAnnouncements = await _db.Announcements.AnyAsync(a => a.IsActiveToPublic == true);

            var viewModel = new UserRegisterViewModel();

            await PopulateDropdowns(viewModel);


            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Register Page", "User navigated to registration page.", ct);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserRegisterViewModel model, CancellationToken ct)
        {
            try
            {
                await PopulateDropdowns(model);
                // Remove the validation errors for DependentPhoneNumber and DependentEmail

                // Custom logic to remove validation errors for optional fields
                foreach (var dependent in model.Dependents)
                {
                    if (string.IsNullOrWhiteSpace(dependent.DependentPhoneNumber))
                    {
                        ModelState.Remove($"Dependents[{model.Dependents.IndexOf(dependent)}].DependentPhoneNumber");
                    }

                    if (string.IsNullOrWhiteSpace(dependent.DependentEmail))
                    {
                        ModelState.Remove($"Dependents[{model.Dependents.IndexOf(dependent)}].DependentEmail");
                    }
                }

                // Validate that the user is at least 18 years old

                var userAge = CalculateAge(model.PersonYearOfBirth.ToString());
                if (userAge < 18)
                {
                    TempData[SD.Error] = "You must be at least 18 years old to register.";
                    return View(model);
                }

                if (ModelState.IsValid)
                {

                    //  Check for similar user in Dependants table
                    var mainAlreadyExists = await _db.Dependants.AnyAsync(d =>
                        (
                            d.PersonName == (model.FirstName + " " + model.Surname).Trim() &&
                            d.PersonYearOfBirth == model.PersonYearOfBirth &&
                            d.OutwardPostcode == model.OutwardPostcode
                        )
                        || (!string.IsNullOrWhiteSpace(model.Email) && d.Email == model.Email)
                        || (!string.IsNullOrWhiteSpace(model.PhoneNumber) && d.Telephone == model.PhoneNumber),
                        ct);

                    if (mainAlreadyExists)
                    {
                        TempData[SD.Error] = "Account already exists. If this is you, please log in or contact us for support.";
                        await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "New Member",
                            $"Registration blocked — duplicate main account found for {model.Email}", ct);
                        return View(model);
                    }

                    // Check if a user with the given email already exists
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null)
                    {
                        TempData[SD.Error] = "User already exists. Please Login";
                        await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "RegisterUser", "ERROR registering new account for: " + model.Email + " but user already exists.");
                        return View(model);
                    }

                    Random _random = new Random();
                    var num = _random.Next(0, 9999).ToString("D4");
                    var user = new User
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FirstName = model.FirstName,
                        Surname = model.Surname,
                        DateCreated = DateTime.UtcNow,
                        RegionId = model.RegionId,
                        CityId = model.CityId,
                        Title = model.Title,
                        PersonRegNumber = "U" + num + model.FirstName.Substring(0, 1).ToUpper() + model.Surname.Substring(0, 1).ToUpper(),
                        PersonYearOfBirth = model.PersonYearOfBirth,
                        PhoneNumber = model.PhoneNumber,
                        IsConsent = true,
                        SponsorsMemberName = model.RefereeMemberName,
                        SponsorsMemberNumber = model.RefereeMemberNumber,
                        SponsorLocalAdminName = model.RefereeLocalAdminName,
                        SponsorLocalAdminNumber = model.RefereeLocalAdminNumber,
                        OutwardPostcode = model.OutwardPostcode,
                        ApplicationStatus = Status.AwaitingApproval,
                        CreatedBy = model.Email,
                        IsActive = true,
                        LastPasswordChangedDate = DateTime.UtcNow,
                        UpdateOn = DateTime.UtcNow.AddHours(24)
                    };

                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        if (!await _roleManager.RoleExistsAsync(RoleList.GeneralAdmin))
                        {
                            await _roleManager.CreateAsync(new IdentityRole(RoleList.GeneralAdmin));
                            await _roleManager.CreateAsync(new IdentityRole(RoleList.LocalAdmin));
                            await _roleManager.CreateAsync(new IdentityRole(RoleList.RegionalAdmin));
                            await _roleManager.CreateAsync(new IdentityRole(RoleList.Member));
                            await _roleManager.CreateAsync(new IdentityRole(RoleList.Dependent));
                        }
                        if (!await _roleManager.RoleExistsAsync(RoleList.RegionalAdmin))
                        {
                            await _roleManager.CreateAsync(new IdentityRole(RoleList.RegionalAdmin));

                        }
                        if (!await _roleManager.RoleExistsAsync(RoleList.Dependent))
                        {
                            await _roleManager.CreateAsync(new IdentityRole(RoleList.Dependent));

                        }
                        await _userManager.AddToRoleAsync(user, RoleList.Member);
                        await RecordAuditAsync(null, model.Email, _requestIpHelper.GetRequestIp(), "New Account", "Success", " account created successfully.", ct);
                        await _db.SaveChangesAsync();

                        // Add to dependents
                        int dependentCount = 1;
                        var newDependents = new List<Dependant>();
                        foreach (var dependent in model.Dependents)
                        {
                            dependentCount++;
                            string fullName = dependent.PersonName?.Trim() ?? "";
                            string name3 = "";
                            string lasName3 = "";

                            if (!string.IsNullOrWhiteSpace(fullName))
                            {
                                string[] names3 = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                name3 = names3.FirstOrDefault() ?? "";
                                lasName3 = names3.LastOrDefault() ?? "";
                            }

                            var exists = await _db.Dependants.AnyAsync(d =>
                            (
                               d.PersonName == fullName &&
                               d.PersonYearOfBirth == dependent.PersonYearOfBirth &&
                               d.OutwardPostcode == user.OutwardPostcode
                            )
                               || (!string.IsNullOrWhiteSpace(dependent.DependentEmail) && d.Email == dependent.DependentEmail)
                               || (!string.IsNullOrWhiteSpace(dependent.DependentPhoneNumber) && d.Telephone == dependent.DependentPhoneNumber),
                               ct);

                            if (exists)
                            {
                                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "New Member",
                                    $"Skipped duplicate dependent {fullName} for: {model.Email}", ct);
                                continue;
                            }


                            var num2 = _random.Next(0, 9999).ToString("D4");


                            var newDependent = new Dependant
                            {
                                UserId = user.UserId,
                                PersonName = dependent.PersonName,
                                CreatedBy = user.Email,
                                IsActive = true,
                                OutwardPostcode = user.OutwardPostcode,
                                CityId = user.CityId,
                                RegionId = user.RegionId,
                                Gender = dependent.DependentGender,  // Use the DependentGender from the model
                                PersonYearOfBirth = dependent.PersonYearOfBirth,
                                PersonRegNumber = "U" + num2 + (name3.Length > 0 ? name3.Substring(0, 1) : "X") + (lasName3.Length > 0 ? lasName3.Substring(0, 1) : "X"),
                                DateCreated = DateTime.UtcNow,
                                Email = dependent.DependentEmail,         // Add DependentEmail
                                Telephone = dependent.DependentPhoneNumber, // Add DependentPhoneNumber
                                Title = dependent.DependentTitle          // Add DependentTitle
                            };
                            _db.Dependants.Add(newDependent);
                        }

                        Dependant dep = new Dependant
                        {
                            UserId = user.UserId,
                            PersonName = model.FirstName + " " + model.Surname ?? "",
                            Gender = model.Gender,
                            Title = model.Title,
                            Email = model.Email,
                            OutwardPostcode = user.OutwardPostcode,
                            CityId = user.CityId,
                            RegionId = user.RegionId,
                            Telephone = model.PhoneNumber,
                            PersonRegNumber = "U" + num + model.FirstName.Trim().Substring(0, 1) + model.Surname.Trim().Substring(0, 1),
                            PersonYearOfBirth = model.PersonYearOfBirth,
                            DateCreated = DateTime.UtcNow,
                            CreatedBy = model.Email,
                            UpdateOn = DateTime.UtcNow,
                            IsActive = true
                        };

                        _db.Dependants.Add(dep);
                        await _db.SaveChangesAsync();


                        await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "New Member", "Dependents added successfully for: " + model.Email);


                        // Check for potential matches in User table
                        //var possibleMatches = await _db.Users.Where(u =>
                        //    EF.Functions.Like(u.FirstName, $"%{model.FirstName}%") &&
                        //    EF.Functions.Like(u.Surname, $"%{model.Surname}%") &&
                        //    u.PersonYearOfBirth == model.PersonYearOfBirth &&
                        //    u.OutwardPostcode == model.OutwardPostcode
                        //).ToListAsync(ct);

                        //if (possibleMatches.Any())
                        //{
                        //    // Flag the new user as awaiting approval                          

                        //    TempData[SD.Success] = "Your account is under review, we'll email you with futher instructions.";
                        //    HttpContext.Session.SetString("isEmail", model.Email);
                        //    HttpContext.Session.SetString("Person1RegNumber", user.PersonRegNumber);
                        //    await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "New Member", "Email sent for: " + model.Email);
                        //    return RedirectToAction(nameof(UnderReview));
                        //}
                        // Sending email
                        var userId = await _userManager.GetUserIdAsync(user);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        const string pathToFile = @"EmailTemplate/Registration.html";
                        const string subject = "Verify your account";

                        code = HttpUtility.UrlEncode(code);
                        var expirationTime = DateTime.UtcNow.AddHours(24); // Set expiration time to 24 hours
                        var callbackUrl = Url.Action("AccountVerified",
                                "Account", new
                                {
                                    userId,
                                    code
                                },
                                protocol: Request.Scheme);

                        string htmlBody = "";

                        using (StreamReader reader = System.IO.File.OpenText(pathToFile))
                        {
                            htmlBody = await reader.ReadToEndAsync(ct);
                            htmlBody = htmlBody.Replace("{{userName}}", user.FirstName + " " + user.Surname)
                                .Replace("{{currentYear}}", DateTime.UtcNow.Year.ToString())
                                .Replace("{{UmojaWetu Portal Verification}}", "<a href=\"" + callbackUrl + "\">" + "Verify Your Account" + "</a>");
                        }

                        var message = new PostmarkMessage
                        {
                            To = model.Email,
                            Subject = subject,
                            HtmlBody = htmlBody,
                            From = "info@umojawetu.com"
                        };

                        var emailSent = await _postmark.SendMessageAsync(message, ct).ConfigureAwait(false);

                        // Store verification token with expiration time
                        var emailVerification = new EmailVerification
                        {
                            UserId = user.UserId,
                            Token = code,
                            ExpirationTime = expirationTime,
                            PersonRegNumber = dep.PersonRegNumber,
                            Used = false,
                            DateCreated = DateTime.UtcNow,
                            CreatedBy = model.Email,
                            UpdatedOn = DateTime.UtcNow,
                        };
                        _db.EmailVerifications.Add(emailVerification);
                        await _db.SaveChangesAsync();

                        TempData[SD.Success] = "Account created. Please check your email to verify your email.";
                        HttpContext.Session.SetString("isEmail", model.Email);
                        HttpContext.Session.SetString("Person1RegNumber", user.PersonRegNumber);
                        await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "New Member", "Email sent for: " + model.Email);

                        return RedirectToAction(nameof(UnderReview));
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    TempData[SD.Error] = "Please check form for invalid field(s).";
                    await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "New Member", "ERROR registering new account for: " + model.Email + " because of invalid data.");

                    return View(model);
                }

                TempData[SD.Error] = "Please check form for invalid field(s).";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "New Member", "ERROR registering new account for: " + model.Email + " because of invalid data.");

                return View(model);
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "An error occurred while registering the account. Please try again.";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "New Member", "ERROR registering new account for: " + model.Email + " due to exception: " + ex.Message);
                return View();
            }
        }
        private int CalculateAge(string dateOfBirth)
        {
            DateTime dob;
            var formats = new[] { "d/M/yyyy", "yyyy" };
            if (DateTime.TryParseExact(dateOfBirth, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dob))
            {
                var today = DateTime.Today;
                var age = today.Year - dob.Year;
                if (dob.Date > today.AddYears(-age)) age--;
                return age;
            }
            return -1; // or handle the error accordingly
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactiveMembership(string personRegNumber, string reason, CancellationToken ct)
        {
            try
            {
                // Fetch the user
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PersonRegNumber == personRegNumber && u.IsActive == true, ct);
                if (user == null)
                {
                    await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Cancel Membership", $"ERROR: User with REG {personRegNumber} not found in database.", ct);
                    TempData[SD.Error] = "Please check form for invalid field(s).";
                    return RedirectToAction("Admin", "Users");
                }

                // Deactivate membership
                user.IsActive = false;
                user.DeactivationReason = reason;
                user.DeactivationDate = DateTime.UtcNow;

                // Save changes to the user
                var result = await _userManager.UpdateAsync(user);
                await _db.SaveChangesAsync();
                if (!result.Succeeded)
                {
                    await RecordAuditAsync(user, _requestIpHelper.GetRequestIp(), "DeactiveMembership", $"ERROR: Failed to update user {user.Email}.", ct);
                    TempData[SD.Error] = "Failed to deactivate membership. Please try again.";
                    return RedirectToAction("Admin", "Users");
                }

                var dep = await _db.Dependants.Where(a => a.UserId == user.UserId && a.IsActive == true).ToListAsync();

                foreach (var i in dep)
                {
                    i.IsActive = false;
                    i.DeactivationReason = reason;

                    _db.Dependants.Update(i);
                    await _db.SaveChangesAsync();

                }


                // Prepare email content
                const string pathToFile = @"EmailTemplate/DeactiveMembership.html";
                const string subject = "Membership Deactivation Notification";

                string htmlBody;
                using (StreamReader reader = System.IO.File.OpenText(pathToFile))
                {
                    htmlBody = await reader.ReadToEndAsync(ct);
                }

                // Replace placeholders in the email template
                htmlBody = htmlBody
                    .Replace("{{userName}}", $"{user.FirstName} {user.Surname}")
                    .Replace("{{currentYear}}", DateTime.UtcNow.Year.ToString());

                // Create and send email
                var message = new PostmarkMessage
                {
                    To = user.Email,
                    Subject = subject,
                    HtmlBody = htmlBody,
                    From = "info@umojawetu.com"
                };

                // var emailSent = await _postmark.SendMessageAsync(message, ct);                
                await RecordAuditAsync(user, _requestIpHelper.GetRequestIp(), "Cancel Membership", $"Membership deactivated, and email notification sent to {user.Email}.", ct);
                TempData[SD.Success] = "Membership successfully deactivated, and notification email sent.";



                return RedirectToAction("Admin", "Users");
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Cancel Membership", $"ERROR: {ex.Message}", ct);
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Admin", "Users");
            }
        }

        private async Task GetPopulateDropdowns(UserRegisterViewModel viewModel)
        {

            var mtitle = await _db.Title.OrderBy(a => a.Name).ToListAsync();
            List<Title> gtitle = new();
            gtitle = mtitle;

            ViewBag.Ttile = gtitle;

            //Gender
            var mgender = await _db.Gender.OrderBy(a => a.GenderName).ToListAsync();
            List<Gender> gender = new();
            gender = mgender;

            ViewBag.Gender = gender;

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

            ViewBag.RegionId = await _db.Region.OrderBy(a => a.Name).ToListAsync();
            //ViewBag.Ttile = await _db.Title.OrderBy(a => a.Name).ToListAsync();
            ViewBag.CityId = await _db.City.OrderBy(a => a.Name).ToListAsync();
        }


        private async Task PopulateDropdowns(UserRegisterViewModel viewModel)
        {
            // Populate Titles for users and dependents
            ViewBag.Ttile = await _db.Title.OrderBy(a => a.Name).ToListAsync();
            ViewBag.DTtile = ViewBag.Ttile; // Reuse Titles for dependents

            // Populate Genders for users and dependents
            ViewBag.Gender = await _db.Gender.OrderBy(a => a.GenderName).ToListAsync();
            ViewBag.DGender = ViewBag.Gender; // Reuse Genders for dependents

            // Populate Regions
            ViewBag.RegionId = await _db.Region.OrderBy(a => a.Name).ToListAsync();

            // Populate Cities based on the selected Region
            if (viewModel.RegionId != null) // Check if RegionId has a value
            {
                ViewBag.CityId = await _db.City
                    .Where(city => city.RegionId == viewModel.RegionId)
                    .OrderBy(city => city.Name)
                    .ToListAsync();
            }
            else
            {
                ViewBag.CityId = new List<City>(); // Empty list if no Region is selected
            }
        }

        [ActionName("GetCityByRegion")]
        public async Task<IActionResult> GetCityByRegion(int id)
        {

            List<City> cities = new List<City>();

            cities = _db.City.Where(a => a.RegionId == id).OrderBy(a => a.Name).ToList();
            SelectList citiesList = new SelectList(cities, "Id", "Name");
            return Json(citiesList);
        }

        [HttpGet]
        public async Task<JsonResult> GetCitiesByRegionId(int regionId)
        {
            var cities = await _db.City
                .Where(c => c.RegionId == regionId)
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            return Json(cities);
        }


        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                if (string.IsNullOrEmpty(email))
                {
                    // Clear session and redirect to login
                    HttpContext.Session.Clear();
                    await _signInManager.SignOutAsync();
                    TempData[SD.Error] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                var currentUser = await _userManager.FindByEmailAsync(email);
                if (currentUser != null)
                {
                    // Update user fields in the database                   
                    await _userManager.UpdateAsync(currentUser);

                    // Record audit log
                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Logout", "User logged out successfully.", ct);
                }

                // Clear session and sign out
                HttpContext.Session.Clear();
                await _signInManager.SignOutAsync();

                TempData[SD.Success] = "Logged out successfully.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                // Handle any exceptions during logout
                HttpContext.Session.Clear();
                await _signInManager.SignOutAsync();
                TempData[SD.Error] = "An error occurred during logout. Please try again.";
                return RedirectToAction(nameof(Login));
            }
        }


        [HttpGet]
        public async Task<IActionResult> UnderReview(CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("isEmail");
                var regN = HttpContext.Session.GetString("Person1RegNumber");
                await RecordAuditAsync(null, email, _requestIpHelper.GetRequestIp(), "Navigation", "New Account", "User navigated to UnderReview page.", ct);


                ViewBag.reg = regN;
                return View();
            }
            catch
            {
                return View();
            }



        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmSuccessor(string userId, string code, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            {
                TempData[SD.Error] = "Invalid confirmation request.";
                return RedirectToAction(nameof(Login));
            }

            var successor = await _db.Successors.FirstOrDefaultAsync(s => s.Id.ToString() == userId, ct);
            if (successor == null)
            {
                TempData[SD.Error] = "Successor not found.";
                return RedirectToAction(nameof(Login));
            }

            var getUser = await _db.Users.FirstOrDefaultAsync(a => a.UserId == successor.UserId);
            if (getUser == null)
            {
                TempData[SD.Error] = "User not found.";
                return RedirectToAction(nameof(Login));
            }

            // Decode the custom token
            var decodedToken = HttpUtility.UrlDecode(code);
            var tokenParts = Encoding.UTF8.GetString(Convert.FromBase64String(decodedToken)).Split(':');

            if (tokenParts.Length != 3 || tokenParts[0] != userId || tokenParts[1] != getUser.UserId.ToString())
            {
                TempData[SD.Error] = "Invalid or expired confirmation code.";
                return RedirectToAction(nameof(Login));
            }

            // Update the status to confirmed
            successor.Status = SuccessorStatus.Approved;

            await _db.SaveChangesAsync(ct);

            //update the userId
            getUser.SuccessorId = successor.Id;

            _db.Users.Update(getUser);
            await _db.SaveChangesAsync();

            TempData[SD.Success] = "Thank you. Nomination confirmed successfully.";
            return RedirectToAction(nameof(Login));
        }




        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> DeclineSuccessor(string userId, string code, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            {
                TempData[SD.Error] = "Invalid confirmation request.";
                return RedirectToAction(nameof(Login));
            }

            var successor = await _db.Successors.FirstOrDefaultAsync(s => s.Id.ToString() == userId, ct);
            if (successor == null)
            {
                TempData[SD.Error] = "Successor not found.";
                return RedirectToAction(nameof(Login));
            }


            // Find the user associated with this successor
            var getUser = await _db.Users.FirstOrDefaultAsync(a => a.UserId == successor.UserId && a.SuccessorId == successor.Id);
            if (getUser == null)
            {
                TempData[SD.Error] = "User not found.";
                return RedirectToAction(nameof(Login));
            }

            // Verify the code
            var codeHtmlDecoded = HttpUtility.UrlDecode(code);


            var isTokenValid = await _userManager.VerifyUserTokenAsync(
                getUser,
                _userManager.Options.Tokens.EmailConfirmationTokenProvider,
                "EmailConfirmation", codeHtmlDecoded);

            if (!isTokenValid)
            {
                TempData[SD.Error] = "Invalid or expired decline code.";
                return RedirectToAction(nameof(Login));
            }

            // Update the status to declined
            successor.Status = SuccessorStatus.Declined;
            await _db.SaveChangesAsync(ct);

            TempData[SD.Success] = "Thank you. Nomination declined successfully.";
            return RedirectToAction(nameof(Login));
        }



        [HttpGet]
        public async Task<IActionResult> AccountVerified(string userId, string code)
        {
            try
            {
                string myIP = _requestIpHelper.GetRequestIp();
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
                {
                    TempData[SD.Error] = "Activation Link expired. Please contact Local Admin.";
                    await RecordAuditAsync(null, myIP, "AccountVerification", "Error activating user account, Link has expired after 24hrs please resend another link.");
                    return RedirectToAction(nameof(Login));
                }

                var getUser = await _db.Users.Where(a => a.Id == userId).FirstOrDefaultAsync();

                if (getUser == null)
                {
                    TempData[SD.Error] = "Error activating your account. Please contact Admin.";
                    await RecordAuditAsync(null, myIP, "AccountVerified", "Error activating user account, userId has been deleted from database.");
                    return RedirectToAction(nameof(Login));
                }

                var emailVerification = await _db.EmailVerifications
                    .Where(ev => ev.PersonRegNumber == getUser.PersonRegNumber && ev.Token == code)
                    .FirstOrDefaultAsync();

                if (emailVerification == null || emailVerification.ExpirationTime < DateTime.UtcNow || emailVerification.Used)
                {
                    TempData[SD.Error] = "The verification link has expired.";
                    await RecordAuditAsync(getUser, myIP, "AccountVerified", "Error: The verification link has been used.");
                    return RedirectToAction(nameof(Login));
                }

                var codeHtmlDecoded = HttpUtility.UrlDecode(code);
                var result = await _userManager.ConfirmEmailAsync(getUser, codeHtmlDecoded);

                if (result.Succeeded)
                {
                    // Mark the token as used
                    emailVerification.Used = true;
                    emailVerification.UpdatedOn = DateTime.UtcNow;
                    _db.EmailVerifications.Update(emailVerification);
                    await _db.SaveChangesAsync();

                    await RecordAuditAsync(getUser, myIP, "AccountVerified", "Account verified successfully for: " + getUser.Email);
                    TempData[SD.Success] = "Your account is activated! You can now login.";
                    return RedirectToAction("Login", "Account");
                }
                else
                {
                    TempData[SD.Error] = "Link expired. Please contact Admin.";
                    await RecordAuditAsync(null, myIP, "AccountVerified", "ERROR: Could not retrieve user details for new userId: " + userId + " while trying to activate account.");
                    return RedirectToAction("Login", "Account");
                }
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "AccountVerified", "Error: when trying to activate account " + ex.Message.ToString());
                return RedirectToAction("Login", "Account");
            }
        }

        public async Task<IActionResult> ResendVerificationLink(string userId, string returnUrl, CancellationToken ct)
        {
            try
            {
                var currentUserEmail = HttpContext.Session.GetString("loginEmail");

                if (currentUserEmail == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
                string myIP = _requestIpHelper.GetRequestIp();

                var getUser = await _db.Users.Where(a => a.Id == userId).FirstOrDefaultAsync();
                if (getUser == null)
                {
                    TempData[SD.Error] = "Error user not found.";
                    await RecordAuditAsync(null, myIP, "Database", "Error re-sending verification link, userId is null in ResendVerificationLink action");
                    return RedirectToAction("Users", "Admin");
                }

                var emailVerification = await _db.EmailVerifications
                    .Where(ev => ev.PersonRegNumber == getUser.PersonRegNumber && !ev.Used && ev.ExpirationTime > DateTime.UtcNow)
                    .FirstOrDefaultAsync();

                if (emailVerification != null)
                {
                    TempData[SD.Error] = "Previous activation link is still valid. Please try again later.";
                    await RecordAuditAsync(currentUser, myIP, "Account", "Activation link for user " + getUser.Email + " is still active.");
                    return RedirectToAction("Users", "Admin");
                }

                var code = await _userManager.GenerateEmailConfirmationTokenAsync(getUser);
                const string pathToFile = @"EmailTemplate/Registration.html";
                const string subject = "Verify your account";
                code = HttpUtility.UrlEncode(code);
                var callbackUrl = Url.Action("AccountVerified",
                    "Account", new
                    {
                        userId,
                        code,
                        returnUrl
                    },
                    protocol: Request.Scheme);

                string htmlBody = "";

                using (StreamReader reader = System.IO.File.OpenText(pathToFile))
                {
                    htmlBody = await reader.ReadToEndAsync(ct);
                    htmlBody = htmlBody.Replace("{{userName}}", getUser.FirstName + " " + getUser.Surname)
                        .Replace("{{currentYear}}", DateTime.UtcNow.Year.ToString())
                        .Replace("{{UmojaWetu Portal Verification}}", "<a href=\"" + callbackUrl + "\">" + "UmojaWetu Portal Verification" + "</a>");
                }

                var message = new PostmarkMessage
                {
                    To = getUser.Email,
                    Subject = subject,
                    HtmlBody = htmlBody,
                    From = "info@umojawetu.com"
                };

                var emailSent = await _postmark.SendMessageAsync(message, ct).ConfigureAwait(false);
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Verification Link", "Re-send verification link for user account " + getUser.Email + " was successful.", ct);

                TempData[SD.Success] = "Verification link sent successfully.";

                // Invalidate previous tokens
                var previousTokens = await _db.EmailVerifications
                    .Where(ev => ev.PersonRegNumber == getUser.PersonRegNumber && !ev.Used)
                    .ToListAsync();
                foreach (var token in previousTokens)
                {
                    token.Used = true;
                }
                _db.EmailVerifications.UpdateRange(previousTokens);

                // Store new token with expiration time
                var expirationTime = DateTime.UtcNow.AddHours(24); // Set expiration time to 24 hours
                var newEmailVerification = new EmailVerification
                {
                    UserId = getUser.UserId,
                    PersonRegNumber = getUser.PersonRegNumber,
                    Token = code,
                    ExpirationTime = expirationTime,
                    DateCreated = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow,
                    CreatedBy = currentUser.Email,
                    Used = false
                };
                _db.EmailVerifications.Add(newEmailVerification);

                getUser.UpdateOn = DateTime.UtcNow.AddHours(24);
                _db.Update(getUser);
                await _db.SaveChangesAsync();

                return RedirectToAction("Users", "Admin");
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Navigation", "Error resending verification link because of: " + ex.Message);
                return RedirectToAction("Users", "Admin");
            }
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> LockUser()
        {
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");
            if (currentUserEmail == null)
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {

                var userList = await _db.Users.ToListAsync();
                var userRole = await _db.UserRoles.ToListAsync();
                var roles = await _db.Roles.ToListAsync();

                var currentRole = HttpContext.Session.GetString("Adminuser");
                var currentuserCompanyId = HttpContext.Session.GetInt32("currentuserCompanyId");
                foreach (var user in userList)
                {
                    var role = userRole.FirstOrDefault(u => u.UserId == user.Id);
                    if (role == null)
                    {
                        user.Role = "None";
                    }
                    else
                    {
                        user.Role = roles.FirstOrDefault(u => u.Id == role.RoleId).Name;
                    }

                    if (currentRole == RoleList.GeneralAdmin)
                    {
                        var filtered = userList.Where(a => a.LockoutEnabled == false).ToList();

                        var filteredAdminRole = filtered.Where(a => a.Role != RoleList.GeneralAdmin).ToList();
                        ViewData["MyData"] = filteredAdminRole;
                    }
                    else
                    {
                        var filtered = userList.Where(a => a.LockoutEnabled == false).ToList();
                        ViewData["MyData"] = filtered;
                    }
                }

                await RecordAuditAsync("Navigation", "User navigated to View Lock Users page.");

                return View();
            }
            catch
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Navigation", "Error navigating View Lock User page- unable to fetch data from db");

                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> TwoFA()
        {
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");
            if (currentUserEmail == null)
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {

                var userList = await _db.Users.ToListAsync();
                var userRole = await _db.UserRoles.ToListAsync();
                var roles = await _db.Roles.ToListAsync();

                var currentRole = HttpContext.Session.GetString("Adminuser");
                var currentuserCompanyId = HttpContext.Session.GetInt32("currentuserCompanyId");
                foreach (var user in userList)
                {
                    var role = userRole.FirstOrDefault(u => u.UserId == user.Id);
                    if (role == null)
                    {
                        user.Role = "None";
                    }
                    else
                    {
                        user.Role = roles.FirstOrDefault(u => u.Id == role.RoleId).Name;
                    }

                    if (currentRole == RoleList.GeneralAdmin)
                    {
                        var filtered = userList.Where(a => a.TwoFactorEnabled == true).ToList();

                        var filteredAdminRole = filtered.Where(a => a.Role != RoleList.GeneralAdmin).ToList();
                        ViewData["MyData"] = filteredAdminRole;
                    }
                    else
                    {
                        var filtered = userList.Where(a => a.TwoFactorEnabled == true).ToList();
                        ViewData["MyData"] = filtered;
                    }
                }

                await RecordAuditAsync("Navigation", "User navigated to View users who actived 2fa page.");

                return View();
            }
            catch
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Navigation", "Error navigating View Lock User page- unable to fetch data from db");

                return RedirectToAction("Index", "Home");
            }
        }


        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> UnverifiedUser()
        {
            try
            {
                var currentUserEmail = HttpContext.Session.GetString("loginEmail");

                if (currentUserEmail == null)
                {
                    return RedirectToAction("Login", "Account");
                }
                var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);


                var userList = await _db.Users.ToListAsync();
                var userRole = await _db.UserRoles.ToListAsync();
                var roles = await _db.Roles.ToListAsync();

                var currentRole = HttpContext.Session.GetString("Adminuser");
                if (currentUserEmail == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                var currentuserCompanyId = HttpContext.Session.GetInt32("currentuserCompanyId");
                foreach (var user in userList)
                {
                    var role = userRole.FirstOrDefault(u => u.UserId == user.Id);
                    if (role == null)
                    {
                        user.Role = "None";
                    }
                    else
                    {
                        user.Role = roles.FirstOrDefault(u => u.Id == role.RoleId).Name;
                    }

                    if (currentRole == RoleList.LocalAdmin)
                    {
                        var filtered = userList.Where(a => a.EmailConfirmed == false).ToList();

                        var filteredAdminRole = filtered.Where(a => a.Role != RoleList.GeneralAdmin).ToList();
                        ViewData["MyData"] = filteredAdminRole;
                    }
                    else
                    {
                        var filtered = userList.Where(a => a.EmailConfirmed == false).ToList();
                        ViewData["MyData"] = filtered;

                    }
                }

                await RecordAuditAsync("Navigation", "User navigated to View unconfirmed page.");
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Navigation", "user navigated to View Unverified Users");

                return View(userList);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Navigation", "Error navigating to View Unverified Users because of: " + ex.Message.ToString());

                return RedirectToAction("Users", "Admin");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnverifiedUser(string id, CancellationToken ct)
        {
            try
            {
                string myIP = _requestIpHelper.GetRequestIp();
                var currentUserEmail = HttpContext.Session.GetString("loginEmail");

                if (currentUserEmail == null)
                {
                    return RedirectToAction("Login", "Account");
                }
                var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);


                var objFromDb = await _db.Users.Where(u => u.Id == id).FirstOrDefaultAsync();
                if (objFromDb == null)
                {
                    TempData[SD.Error] = "Error getting your account details.";
                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Verify User", "ERROR: verifying user account because user does not exist.", ct);

                    return RedirectToAction("Users", "Admin");
                }


                objFromDb.EmailConfirmed = true;
                objFromDb.AccessFailedCount = 0;
                objFromDb.LockoutEnabled = true;
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Verify User", "Verified account details for " + objFromDb.Email, ct);

                TempData[SD.Success] = "Account verified successfully.";
                return RedirectToAction("Users", "Admin");
            }
            catch (Exception ex)
            {
                var currentUserEmail = HttpContext.Session.GetString("loginEmail");

                if (currentUserEmail == null)
                {
                    return RedirectToAction("Login", "Account");
                }
                var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);


                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Verify User", "ERROR: could not Verified account details for because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Account Not Verified. Please see Logs.";
                return RedirectToAction("Users", "Admin");
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUnlock(string id, CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                var finduser = await _userManager.FindByEmailAsync(email);

                string myIP = _requestIpHelper.GetRequestIp();
                var objFromDb = await _db.Users.Where(u => u.Id == id).FirstOrDefaultAsync();
                if (objFromDb == null)
                {
                    TempData[SD.Error] = "Error getting your account details. Please see logs.";
                    await RecordAuditAsync(finduser, _requestIpHelper.GetRequestIp(), "Lock User Account", "ERROR: locking account because user does not exist.", ct);

                    return RedirectToAction("Users", "Admin");
                }

                if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
                {
                    //user is locked and will remain locked until lockout end time
                    //clicking on this action will unlock them
                    objFromDb.LockoutEnd = DateTime.Now;
                    objFromDb.LockoutEnabled = true;
                    objFromDb.AccessFailedCount = 0;
                    _db.Users.Update(objFromDb);
                    await _db.SaveChangesAsync();

                    await RecordAuditAsync(finduser, _requestIpHelper.GetRequestIp(), "Unlock User", "Unlocked account details for: " + objFromDb.Email, ct);

                    TempData[SD.Success] = "Account unlocked successfully.";
                    return RedirectToAction("Users", "Admin");
                }
                else
                {
                    //user is not locked, and we want to lock the user
                    if (email == objFromDb.Email)
                    {
                        TempData[SD.Error] = "Unable to lock your own account while logged in.";

                        await RecordAuditAsync(finduser, _requestIpHelper.GetRequestIp(), "Lock User", "User tried to lock their own account while logged in: ", ct);

                        return RedirectToAction("Users", "Admin");
                    }
                    else
                    {
                        objFromDb.LockoutEnd = DateTime.Now.AddYears(1000);
                        objFromDb.LockoutEnabled = false;
                        objFromDb.AccessFailedCount = 1;
                        _db.Users.Update(objFromDb);
                        await _db.SaveChangesAsync();

                        await RecordAuditAsync(finduser, _requestIpHelper.GetRequestIp(), "Lock User", "User locked account details for: " + objFromDb.Email, ct);

                        TempData[SD.Success] = "Account locked successfully.";
                        return RedirectToAction("Users", "Admin");
                    }
                }
            }
            catch (Exception ex)
            {
                var currentUserEmail = HttpContext.Session.GetString("loginEmail");

                if (currentUserEmail == null)
                {
                    return RedirectToAction("Login", "Account");
                }
                var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);


                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "LockUnlcok", "ERROR: could not Lock/Unlock account details for because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Account Not Lock/Unlock. Please see Logs.";
                return RedirectToAction("Users", "Admin");
            }

        }

        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> Edit()
        {

            try
            {
                if (TempData["saveData"] is string s)
                {


                    var newUser = JsonSerializer.Deserialize<User>(s);
                    var newUserEdit = new EditViewModel()
                    {
                        Id = newUser.Id,
                        Email = newUser.Email,
                        FirstName = newUser.FirstName,
                        PhoneNumber = newUser.PhoneNumber,
                        RoleId = newUser.RoleId,
                        RoleList = newUser.RoleList,
                        LastName = newUser.Surname,
                        OldEmail = newUser.Email
                    };

                    return View(newUserEdit);
                }


                var email = HttpContext.Session.GetString("loginEmail");
                if (email == null)
                {
                    TempData[SD.Error] = "Session Expires - Please login.";
                    return RedirectToAction("Login", "Account");
                }
                EditViewModel editViewModel = new();
                var currentUser = await _userManager.FindByEmailAsync(email);

                if (currentUser == null)
                {
                    ViewData["TwoFactorEnabled"] = false;
                    return RedirectToAction("Search", "Home");
                }
                else
                {
                    ViewData["TwoFactorEnabled"] = currentUser.TwoFactorEnabled;
                }
                var userIDtoEncrypt = protector.Protect(currentUser.UserId.ToString());
                ViewBag.userID = userIDtoEncrypt;

                var userRole = _db.UserRoles.ToList();
                var roles = _db.Roles.ToList();
                var role = userRole.FirstOrDefault(u => u.UserId == currentUser.Id);
                if (role != null)
                {
                    currentUser.RoleId = roles.FirstOrDefault(u => u.Id == role.RoleId).Id;
                }
                currentUser.RoleList = _db.Roles.Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id
                });
                var user = _mapper.Map<EditViewModel>(currentUser);

                await RecordAuditAsync("Navigation", "User navigated to User Edit page");

                ////var Companies = _db.Companies.ToList();
                ////List<Company> li = new();
                //li = Companies;
                //ViewBag.CompanyUId = li;
                //user.OldEmail = user.Email;

                return View(user);
            }
            catch
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Navigation", "Error navigating to User Edit page - unable to fetch data from db");
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditViewModel user)
        {
            try
            {

                string myIP = _requestIpHelper.GetRequestIp();
                var email = HttpContext.Session.GetString("loginEmail");

                var currentUser = await _userManager.FindByEmailAsync(email);

                if (currentUser == null)
                {
                    return RedirectToAction("Search", "Home");
                }

                var userIDtoEncrypt = protector.Protect(currentUser.UserId.ToString());
                ViewBag.userID = userIDtoEncrypt;
                if (ModelState.IsValid)
                {
                    if (user.OldEmail != user.Email)
                    {
                        var doesuserExists = await _userManager.FindByEmailAsync(user.Email);
                        if (doesuserExists != null)
                        {
                            await RecordAuditAsync("Error", "User Account", "User tried to Edit an account for " + currentUser.Email + " but the email entered: " + user.Email + " already exists. ");

                            TempData[SD.Error] = "Email already exists";
                            var AdminuserRole = _db.UserRoles.ToList();
                            var getroles = _db.Roles.ToList();
                            var getrole = AdminuserRole.FirstOrDefault(u => u.UserId == user.Id);

                            if (getrole != null)
                            {
                                user.RoleId = getroles.FirstOrDefault(u => u.Id == getrole.RoleId).Id;
                            }
                            user.RoleList = _db.Roles.Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                            {
                                Text = u.Name,
                                Value = u.Id
                            });
                            user.OldEmail = user.Email;

                            return View(user);
                        }
                    }
                    currentUser.Surname = user.LastName;
                    currentUser.FirstName = user.FirstName;
                    currentUser.Email = user.Email;
                    currentUser.PhoneNumber = user.PhoneNumber;
                    currentUser.CreatedBy = email;
                    currentUser.UserName = user.Email;
                    currentUser.NormalizedUserName = user.Email.ToUpper();
                    currentUser.NormalizedEmail = user.Email.ToUpper();

                    HttpContext.Session.SetString("loginEmail", user.Email);
                    _db.Users.Update(currentUser);
                    _db.SaveChanges();

                    var firstNameOnly = user.FirstName.Split(' ')[0];
                    HttpContext.Session.SetString("userFirstName", firstNameOnly);

                    if (user.RoleId == "")
                    {

                    }
                    else
                    {
                        //update role
                        var userRole = await _db.UserRoles.FirstOrDefaultAsync(u => u.UserId == currentUser.Id);
                        if (userRole != null)
                        {
                            var previousRoleName = await _db.Roles.Where(u => u.Id == userRole.RoleId).Select(e => e.Name).FirstOrDefaultAsync();
                            //removing the old role
                            await _userManager.RemoveFromRoleAsync(currentUser, previousRoleName);

                        }

                        //add new role
                        await _userManager.AddToRoleAsync(currentUser, _db.Roles.FirstOrDefault(u => u.Id == user.RoleId).Name);
                        currentUser.FirstName = user.FirstName;
                        _db.SaveChanges();

                        currentUser.RoleList = _db.Roles.Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                        {
                            Text = u.Name,
                            Value = u.Id
                        });

                    }
                    var finduser = await _userManager.FindByEmailAsync(email);

                    await RecordAuditAsync(finduser, myIP, "Database", "Updated user details for " + user.Email);

                    TempData[SD.Success] = "Details updated successfully.";
                    var userobj = _mapper.Map<EditViewModel>(currentUser);

                    //var Companies = _db.Companies.ToList();
                    //List<Company> li = new();
                    //li = Companies;
                    //ViewBag.CompanyUId = li;
                    userobj.OldEmail = user.Email;
                    return View(userobj);
                }


                user.RoleList = _db.Roles.Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id
                });

                //ViewBag.CompanyUId = _db.Companies.ToList();
                user.OldEmail = user.Email;
                return View(user);
            }
            catch
            {
                user.RoleList = _db.Roles.Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id
                });

                //var Companies2 = _db.Companies.ToList();
                //List<Company> les = new();
                //les = Companies2;
                //ViewBag.CompanyUId = les;
                //user.OldEmail = user.Email;
                return View(user);
            }
        }

        [HttpGet]
        public async Task<IActionResult> AwaitingApproval(CancellationToken ct)
        {
            try
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "AwaitingApproval", "User navigated to Awaiting Approval page ", ct);

                return View();
            }
            catch (Exception ex)
            {

                await RecordAuditAsync("AwaitingApproval", "ERROR: navigating to Awaiting Approval page: " + ex.Message.ToString(), ct);
                return View();
            }


        }

        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> Details(string userId)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                TempData[SD.Error] = "Session Expires - Please login.";
                return RedirectToAction("Login", "Account");
            }

            try
            {

                var currentRole = HttpContext.Session.GetString("Adminuser");
                var currentuserCompanyId = HttpContext.Session.GetInt32("currentuserCompanyId");
                string myIP = _requestIpHelper.GetRequestIp();
                var getuser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

                var currentUserrole = await _userManager.GetRolesAsync(getuser);
                var userToEditRole = currentUserrole[0].ToString();

                ViewBag.userRole = userToEditRole;
                if (getuser == null)
                {
                    await RecordAuditAsync("Navigation", "Error trying to fetch user id" + userId + " from database");
                    HttpContext.Session.Clear();
                    TempData[SD.Error] = "Internal server error - Error processing request";
                    return RedirectToAction("Index", "Home");
                }

                if (currentRole == RoleList.GeneralAdmin)
                {
                    await RecordAuditAsync("Navigation", "Admin user tried to edit " + getuser.Email + " account but has different Company id");

                    HttpContext.Session.Clear();
                    TempData[SD.Error] = "Unauthorised request.";
                    return RedirectToAction("Index", "Home", new { returnUrl = "Unauthorised" });
                }

                else if (currentRole == RoleList.GeneralAdmin && userToEditRole == RoleList.GeneralAdmin)
                {
                    //Admin cannot edit Admin user's account
                    await RecordAuditAsync("Navigation", "Admin tried to edit an Admin user's account for " + getuser.Email);

                    HttpContext.Session.Clear();
                    TempData[SD.Error] = "Unauthorised request.";
                    return RedirectToAction("Index", "Home", new { returnUrl = "Unauthorised" });
                }
                var AdminuserRole = _db.UserRoles.ToList();
                var getroles = _db.Roles.ToList();
                var getrole = AdminuserRole.FirstOrDefault(u => u.UserId == getuser.Id);

                if (getrole != null)
                {
                    getuser.RoleId = getroles.FirstOrDefault(u => u.Id == getrole.RoleId).Id;
                }
                getuser.RoleList = _db.Roles.Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id
                });

                var mappeduser = _mapper.Map<EditViewModel>(getuser);
                await RecordAuditAsync("Navigation", "User navigated to Details page. Read account details for " + getuser.Email);

                if (getuser.Email == email.ToString())
                {
                    ViewBag.user = "true";
                }
                else
                {
                    ViewBag.user = "false";
                }
                //ViewBag.CompanyUId = _db.Companies.ToList();
                //var varCompanyName = _db.Companies.Where(a => a.CompanyId == getuser.CompanyId).Select(a => a.Name);
                //ViewBag.defaultValue = varCompanyName;
                ViewBag.userName = getuser.FirstName + " " + getuser.Surname;
                mappeduser.OldEmail = mappeduser.Email;
                return View(mappeduser);
            }
            catch
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Navigation", "Error navigating to Admin Edit page - unable to fetch data from db");

                return RedirectToAction("Index", "Home");
            }

        }


        [Route("/NotFound")]

        public async Task<IActionResult> PageNotFound(CancellationToken ct)
        {

            HttpContext.Session.SetString("pageNotFound", "true");
            TempData[SD.Error] = "Unauthorised request. Please Login";
            return RedirectToAction(nameof(Login));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid userId, CancellationToken ct)
        {
            try
            {

                var email = HttpContext.Session.GetString("loginEmail");
                var currentUser = await _userManager.FindByEmailAsync(email);

                string myIP = _requestIpHelper.GetRequestIp();
                var objFromDb = await _db.Users.Where(u => u.UserId == userId).FirstOrDefaultAsync();
                if (objFromDb == null)
                {
                    TempData[SD.Error] = "Error getting your account details. Please see logs.";
                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Lock User Account", "ERROR: locking account because user does not exist.", ct);

                    return RedirectToAction("Users", "Admin");
                }


                if (email == objFromDb.Email)
                {
                    TempData[SD.Error] = "Unable to delete your own account while logged in.";
                    await RecordAuditAsync(currentUser, myIP, "Account Delete", "User tried to delete their own account from database while logged in", ct);

                    return RedirectToAction("Users", "Admin");
                }

                //remove their dependents 

                var getDeps = await _db.Dependants.Where(a => a.UserId == userId).ToListAsync();

                foreach (var i in getDeps)
                {
                    _db.Dependants.Remove(i);
                    await _db.SaveChangesAsync(ct);
                }
                //remove user

                _db.Users.Remove(objFromDb);
                await _db.SaveChangesAsync(ct);




                await RecordAuditAsync(currentUser, myIP, "Account Delete", "User deleted account: " + objFromDb.Email + " from db", ct);
                TempData[SD.Success] = "Account deleted successfully.";
                return RedirectToAction("Users", "Admin");
            }
            catch (Exception ex)
            {
                var currentUserEmail = HttpContext.Session.GetString("loginEmail");

                if (currentUserEmail == null)
                {
                    return RedirectToAction("Login", "Account");
                }
                var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);


                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Delete Account", "ERROR: could not Delete account because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Account Not Deleted. Please see Logs.";
                return RedirectToAction("Users", "Admin");
            }

        }


        [HttpGet]
        public async Task<IActionResult> ChangePassword(string reg)
        {
            try
            {
                var decryptedReg = protector.Unprotect(reg);
                var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.PersonRegNumber == decryptedReg);

                if (currentUser == null)
                {
                    TempData[SD.Error] = "User not found.";
                    return RedirectToAction("Login", "Account");
                }

                var model = new ChangePasswordViewModel
                {
                    PersonRegNumber = reg,
                };

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Navigation", "User navigated to Change Password page");
                return View(model);
            }
            catch
            {
                TempData[SD.Error] = "Invalid password change link.";
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken ct)
        {
            try
            {
                var decryptedReg = protector.Unprotect(model.PersonRegNumber);
                var user = await _db.Users.FirstOrDefaultAsync(u => u.PersonRegNumber == decryptedReg);

                if (user == null)
                {
                    TempData[SD.Error] = "User not found.";
                    return RedirectToAction("Login", "Account");
                }

                if (await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    TempData[SD.Error] = "New password cannot be the same as your current password.";
                    return View(model);
                }

                var validation = await passwordValidator.ValidateAsync(_userManager, user, model.Password);
                if (!validation.Succeeded)
                {
                    foreach (var error in validation.Errors)
                        ModelState.AddModelError("", error.Description);

                    return View(model);
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.Password);

                if (result.Succeeded)
                {
                    user.ForcePasswordChange = false;
                    user.UserName = user.Email;
                    user.NormalizedUserName = user.Email.ToUpper();                        
                    user.LastPasswordChangedDate = DateTime.UtcNow;
                    user.UpdateOn = DateTime.UtcNow;
                    _db.Users.Update(user);
                    await _db.SaveChangesAsync();

                    await RecordAuditAsync(user, _requestIpHelper.GetRequestIp(), "ChangePassword", "User changed password successfully.");
                    TempData[SD.Success] = "Password changed successfully.";

                    HttpContext.Session.Clear();
                    await _signInManager.SignOutAsync();
                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "Unexpected error occurred.";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "ChangePassword", "Error: " + ex.Message, ct);
                return RedirectToAction("Login", "Account");
            }
        }



        public async Task<IActionResult> ForcePasswordChange(string reg, CancellationToken ct)
        {
            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Redirect", "Visited Change Password page with reg: ",reg, ct);

            if (string.IsNullOrWhiteSpace(reg))
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Redirect", "Invalid link or expired session. ", ct);

                TempData[SD.Error] = "Invalid link or expired session.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var decryptedReg = protector.Unprotect(reg);
                var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.PersonRegNumber == decryptedReg);

                if (currentUser == null)
                {
                    await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Redirect", "User not found", ct);
                    TempData[SD.Error] = "User not found.";
                    return RedirectToAction("Login", "Account");
                }

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Redirect", "User navigated to Change Password page with reg: " + decryptedReg, ct);

                var model = new ForcePasswordChangeViewModel();
                model.PersonRegNumber = reg;
                return View(model);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Redirect", "ERROR: " + ex.Message, ct);
                TempData[SD.Error] = "Something went wrong.";
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForcePasswordChange(ForcePasswordChangeViewModel model, CancellationToken ct)
        {
            try
            {
                var decryptedReg = protector.Unprotect(model.PersonRegNumber);
                var user = await _db.Users.FirstOrDefaultAsync(u => u.PersonRegNumber == decryptedReg);

                if (user == null)
                {
                    TempData[SD.Error] = "User not found.";
                    return RedirectToAction("Login", "Account");
                }

                if (await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    TempData[SD.Error] = "New password cannot be the same as your current password.";
                    return View(model);
                }

                var validation = await passwordValidator.ValidateAsync(_userManager, user, model.Password);
                if (!validation.Succeeded)
                {
                    foreach (var error in validation.Errors)
                        ModelState.AddModelError("", error.Description);

                    return View(model);
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.Password);

                if (result.Succeeded)
                {
                    user.ForcePasswordChange = false;
                    user.UserName = user.Email;
                    user.NormalizedUserName = user.Email.ToUpper();
                    user.LastPasswordChangedDate = DateTime.UtcNow;
                    user.UpdateOn = DateTime.UtcNow;
                    _db.Users.Update(user);
                    await _db.SaveChangesAsync();

                    await RecordAuditAsync(user, _requestIpHelper.GetRequestIp(), "ChangePassword", "User changed password successfully.");
                    TempData[SD.Success] = "Password changed successfully.";

                    HttpContext.Session.Clear();
                    await _signInManager.SignOutAsync();
                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "Unexpected error occurred.";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "ChangePassword", "Error: " + ex.Message, ct);
                return RedirectToAction("Login", "Account");
            }
        }


        [HttpGet]
        public async Task<IActionResult> ForgetPassword(CancellationToken ct)
        {

            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "ForgetPassword", "User navigated to Forget Password page.", ct);
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordViewModel viewmodel, CancellationToken ct)
        {
            var doesEmailExist = _db.Users.Any(a => a.Email == viewmodel.Email);
            try
            {
                if (doesEmailExist)
                {
                    var user = await _db.Users.Where(a => a.Email == viewmodel.Email).FirstOrDefaultAsync();
                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    const string pathToFile = @"EmailTemplate/ForgetPassword.html";
                    const string subject = "Reset Your Password";
                    var personRegNumber = user.PersonRegNumber;

                    code = HttpUtility.UrlEncode(code);
                    var expirationTime = DateTime.UtcNow.AddHours(24); // Set expiration time to 24 hours
                    var callbackUrl = Url.Action("ResetPassword",
                            "Account", new
                            {
                                personRegNumber = user.PersonRegNumber,
                                code
                            },
                            protocol: Request.Scheme);

                    string htmlBody = "";

                    using (StreamReader reader = System.IO.File.OpenText(pathToFile))
                    {
                        htmlBody = await reader.ReadToEndAsync(ct);
                        htmlBody = htmlBody.Replace("{{userName}}", user.FirstName + " " + user.Surname)
                        .Replace("{{currentYear}}", DateTime.UtcNow.Year.ToString())
                        .Replace("{{Reset Password}}", "<a href=\"" + callbackUrl + "\">" + "Reset Password" + "</a>");
                    }

                    var message = new PostmarkMessage
                    {
                        To = viewmodel.Email,
                        Subject = subject,
                        HtmlBody = htmlBody,
                        From = "info@umojawetu.com"
                    };

                    var emailSent = await _postmark.SendMessageAsync(message, ct).ConfigureAwait(false);

                    // Store token with expiration time
                    var passwordReset = new PasswordReset
                    {
                        personRegNumber = user.PersonRegNumber,
                        Token = code,
                        ExpirationTime = expirationTime,
                        Used = false,
                        DateCreated = DateTime.UtcNow,
                        UpdatedOn = DateTime.UtcNow,
                        CreatedBy = user.Email
                    };
                    _db.PasswordResets.Add(passwordReset);
                    await _db.SaveChangesAsync(ct);

                    await RecordAuditAsync(user, _requestIpHelper.GetRequestIp(), "Forgot Password", "User requested a password reset link.");
                    TempData[SD.Success] = "A password reset link has been sent. Please check your email, including your spam or junk folder.";
                    return View();
                }
                else
                {
                    await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Forgot Password", "User requested a password reset link but the email provided: " + viewmodel.Email + " is not registered.");
                    TempData[SD.Success] = "If the email address you entered is valid, a password reset link has been sent to your inbox.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Forgot Password", "User requested a password reset link for email provided: " + viewmodel.Email + " but had the following error: " + ex.Message.ToString());
                TempData[SD.Error] = "ERROR: Please contact admin quoting ERROR: #RP001 ";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string personRegNumber, string code)
        {
            try
            {
                string myIP = _requestIpHelper.GetRequestIp();

                if (string.IsNullOrEmpty(code))
                {
                    TempData[SD.Error] = "Invalid password reset link. Please contact Admin.";
                    await RecordAuditAsync(null, myIP, "Reset Password", "Invalid reset password link.");
                    return RedirectToAction(nameof(Login));
                }

                var getUser = await _db.Users.Where(a => a.PersonRegNumber == personRegNumber).FirstOrDefaultAsync();
                if (getUser == null)
                {
                    TempData[SD.Error] = "Error resetting your account. Please contact Admin.";
                    await RecordAuditAsync(null, myIP, "Reset Password", "Error: User ID not found in the database.");
                    return RedirectToAction(nameof(Login));
                }

                var passwordReset = await _db.PasswordResets
                    .Where(pr => pr.personRegNumber == getUser.PersonRegNumber && pr.Token == code)
                    .FirstOrDefaultAsync();

                if (passwordReset == null)
                {
                    TempData[SD.Error] = "The password reset link is invalid.";
                    await RecordAuditAsync(getUser, myIP, "Reset Password", "Error: The password reset link is invalid.");
                    return RedirectToAction(nameof(Login));
                }

                if (passwordReset.ExpirationTime < DateTime.UtcNow)
                {
                    TempData[SD.Error] = "The password reset link has expired.";
                    await RecordAuditAsync(getUser, myIP, "Reset Password", "Error: The password reset link has expired.");
                    return RedirectToAction(nameof(Login));
                }

                if (passwordReset.Used)
                {
                    TempData[SD.Error] = "The password reset link has already been used.";
                    await RecordAuditAsync(getUser, myIP, "Reset Password", "Error: The password reset link has already been used.");
                    return RedirectToAction(nameof(Login));
                }

                var model = new ResetPasswordViewModel { code = code, personRegNumber = personRegNumber.ToString() };

                await RecordAuditAsync(getUser, myIP, "Reset Password", "User navigated to Reset Password page.");
                return View(model);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Reset Password", "Error navigating to Reset Password page: " + ex.Message.ToString());

                TempData[SD.Error] = "Error: Please contact Admin.";
                return RedirectToAction(nameof(Login));
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]

        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> PageNavigationLogs()
        {
            var logs = await _db.PageNavigationLogs.OrderByDescending(log => log.Timestamp).ToListAsync();
            return View(logs);
        }
         
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            try
            {
                string myIP = _requestIpHelper.GetRequestIp();

                if (string.IsNullOrEmpty(model.personRegNumber) || string.IsNullOrEmpty(model.code))
                {
                    TempData[SD.Error] = "Password Reset Link expired. Please contact Local Admin.";
                    await RecordAuditAsync(null, myIP, "ResetPassword", "Error resetting user account, Link has expired or invalid.");
                    return RedirectToAction(nameof(Login));
                }

                //Guid userId;
                //if (!Guid.TryParse(model.userId, out userId))
                //{
                //    TempData[SD.Error] = "Invalid User ID. Please contact Admin.";
                //    await RecordAuditAsync(null, myIP, "ResetPassword", "Error: Invalid User ID format.");
                //    return RedirectToAction(nameof(Login));
                //}

                var getUser = await _db.Users.Where(a => a.PersonRegNumber == model.personRegNumber).FirstOrDefaultAsync();
                if (getUser == null)
                {
                    TempData[SD.Error] = "Error resetting your account. Please contact Admin.";
                    await RecordAuditAsync(null, myIP, "ResetPassword", "Error resetting user password, userId has been deleted from database.");
                    return RedirectToAction(nameof(Login));
                }

                var passwordReset = await _db.PasswordResets
                    .Where(pr => pr.personRegNumber == getUser.PersonRegNumber && pr.Token == model.code)
                    .FirstOrDefaultAsync();

                if (passwordReset == null || passwordReset.ExpirationTime < DateTime.UtcNow || passwordReset.Used)
                {
                    TempData[SD.Error] = "The password reset link is invalid or expired.";
                    await RecordAuditAsync(getUser, myIP, "ResetPassword", "Error: The password reset link is invalid or expired.");
                    return RedirectToAction(nameof(Login));
                }

                var codeHtmlDecoded = HttpUtility.UrlDecode(model.code);
                var result = await _userManager.ResetPasswordAsync(getUser, codeHtmlDecoded, model.Password);

                if (result.Succeeded)
                {
                    // Mark the token as used
                    passwordReset.Used = true;
                    passwordReset.UpdatedOn = DateTime.UtcNow;

                    _db.PasswordResets.Update(passwordReset);
                    await _db.SaveChangesAsync();

                    await RecordAuditAsync(getUser, myIP, "ResetPassword", "User changed their password successfully.");
                    TempData[SD.Success] = "Password changed successfully. Please login.";
                    return RedirectToAction(nameof(Login));
                }
                else
                {
                    await RecordAuditAsync(getUser, myIP, "ResetPassword", "ERROR: Could not reset user password.");
                    TempData[SD.Error] = "Error resetting your password. Please try again.";
                    return RedirectToAction(nameof(Login));
                }
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "ResetPassword", "Error: when trying to reset password: " + ex.Message.ToString());
                TempData[SD.Error] = "An error occurred while resetting your password. Please try again later.";
                return RedirectToAction(nameof(Login));
            }
        }


        //[Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> Impersonate(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var adminUserId = Guid.Parse(_userManager.GetUserId(User));
            var adminEmail = User.Identity.Name;

            // Store the current admin ID in session to revert back
            HttpContext.Session.SetString("OriginalUserId", adminUserId.ToString());
            HttpContext.Session.SetString("loginEmail", user.Email);

            var firstNameOnly = user.FirstName.Split(' ')[0];
            HttpContext.Session.SetString("userFirstName", firstNameOnly);

            if (adminEmail != "seakou2@yahoo.com")
            {
                // Log the impersonation
                var log = new ImpersonationLog
                {
                    UserId = adminUserId,
                    AdminEmail = adminEmail,
                    ImpersonatedUserId = user.Id,
                    ImpersonatedEmail = user.Email,
                    Timestamp = DateTime.UtcNow
                };

                _db.ImpersonationLogs.Add(log);
                await _db.SaveChangesAsync();
            }


            // Sign in the impersonated user
            await _signInManager.SignOutAsync();
            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Dashboard", "Home"); // Redirect to user's home page or another page
        }



        public async Task<IActionResult> StopImpersonation()
        {
            var originalUserId = HttpContext.Session.GetString("OriginalUserId");
            if (originalUserId != null)
            {
                var originalUser = await _userManager.FindByIdAsync(originalUserId);
                if (originalUser != null)
                {
                    await _signInManager.SignOutAsync();
                    await _signInManager.SignInAsync(originalUser, isPersistent: false);
                }
                HttpContext.Session.Remove("OriginalUserId");
                HttpContext.Session.SetString("loginEmail", originalUser.Email);

                var firstNameOnly = originalUser.FirstName.Split(' ')[0];
                HttpContext.Session.SetString("userFirstName", firstNameOnly);

            }

            return RedirectToAction("Users", "Admin"); // Redirect to admin's home page or another page
        }


        [HttpGet]
        [Authorize]

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            try
            {
                string myIP = _requestIpHelper.GetRequestIp();
                var email = HttpContext.Session.GetString("loginEmail");

                if (string.IsNullOrEmpty(email))
                {
                    TempData[SD.Error] = "Session Expired - Please login.";
                    return RedirectToAction("Login", "Account");
                }

                var loggedInUser = await _db.Users.FirstOrDefaultAsync(a => a.Email == email && a.IsActive == true);

                if (loggedInUser == null)
                {
                    TempData[SD.Error] = "User not found.";
                    return RedirectToAction("Login", "Account");
                }
               
                // Ensure user ViewModel is initialized
                var user = _mapper.Map<AccountViewModel>(loggedInUser) ?? new AccountViewModel();

                // Ensure DeactivateAccountViewModel is initialized
                user.DeactivateAccountViewModel = new DeactivateAccountViewModel
                {
                    PersonRegNumber = loggedInUser.PersonRegNumber,
                    UserId = loggedInUser.UserId,
                    DeactivationReason = ""
                };


                // Encrypt email for password change security
                ViewBag.EmailAddress = protector.Protect(loggedInUser.Email);
                ViewBag.UserID = protector.Protect(loggedInUser.Email);

                await RecordAuditAsync(loggedInUser, myIP, "Account", "User navigated to Account page");

                return View(user);
            }
            catch (Exception ex)
            {
                string myIP = _requestIpHelper.GetRequestIp();
                var email = HttpContext.Session.GetString("loginEmail");

                var loggedInUser = await _userManager.FindByEmailAsync(email);
                if (email == null)
                {
                    TempData[SD.Error] = "Session Expired - Please login.";
                    return RedirectToAction("Login", "Account");
                }
                await RecordAuditAsync(loggedInUser, myIP, "Account", $"Error navigating to Account page: {ex.Message}");

                return RedirectToAction("Index", "Home");
            }
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> NewPassword(string personRegNumber)
        {
            try
            {
                string myIP = _requestIpHelper.GetRequestIp();
                var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.PersonRegNumber == personRegNumber && a.IsActive == true);
                var email = HttpContext.Session.GetString("loginEmail");

                var loggedInUser = await _userManager.FindByEmailAsync(email);
                if (email == null)
                {
                    TempData[SD.Error] = "Session Expires - Please login.";
                    return RedirectToAction("Login", "Account");
                }

                string emailtoencryty = "";
                emailtoencryty = protector.Protect(currentUser.Email.ToString());
                ViewBag.EmailAddress = emailtoencryty;
                ViewBag.userID = emailtoencryty;

                await RecordAuditAsync(currentUser, myIP, "Navigation", "User navigated to Change Password page on first log on");

                return View();
            }
            catch
            {

                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Navigation", "Error navigating to New Password page - unable to fetch data from db");

                return RedirectToAction("Index", "Home");
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewPassword(ResetPasswordViewModel newPassword, CancellationToken ct)
        {
            try
            {
                string myIP = _requestIpHelper.GetRequestIp();
                var emailTodecrypt = protector.Unprotect(newPassword.EmailAddress);
                var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == emailTodecrypt, ct);
                if (user == null)
                {
                    TempData[SD.Error] = "Error: Please contact Admin.";
                    await RecordAuditAsync(user, myIP, "ChangePassword", "User not found in database.");
                    return RedirectToAction("Index", "Home");
                }

                var currentLoginUserEmail = HttpContext.Session.GetString("loginEmail");
                var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.Email == currentLoginUserEmail, ct);

                if (!await _userManager.CheckPasswordAsync(user, newPassword.OldPassword))
                {
                    TempData[SD.Error] = "Current password does not match.";
                    ViewBag.EmailAddress = protector.Protect(currentUser.Email);
                    ViewBag.userID = protector.Protect(currentUser.Email);
                    await RecordAuditAsync(user, myIP, "NewPassword", "User tried to change their password but the current password entered does not match.");
                    return View();
                }

                if (newPassword.OldPassword == newPassword.Password)
                {
                    TempData[SD.Error] = "New Password cannot be the same as your current password.";
                    ViewBag.EmailAddress = protector.Protect(currentUser.Email);
                    ViewBag.userID = protector.Protect(currentUser.Email);
                    await RecordAuditAsync(user, myIP, "ChangePassword", "User entered a new password that is the same as their current password.");
                    return View();
                }

                var result = await passwordValidator.ValidateAsync(_userManager, user, newPassword.Password);
                if (!result.Succeeded)
                {
                    TempData[SD.Error] = "New password does not meet the password requirements.";
                    ViewBag.EmailAddress = protector.Protect(currentUser.Email);
                    ViewBag.userID = protector.Protect(currentUser.Email);
                    await RecordAuditAsync(user, myIP, "ChangePassword", "User tried changing their password but provided a password that did not meet the password requirements.");
                    return View();
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, newPassword.Password);
                if (resetResult.Succeeded)
                {
                    user.PasswordHash = passwordHasher.HashPassword(user, newPassword.Password);
                    user.ForcePasswordChange = false;
                    user.UserName = newPassword.EmailAddress;
                    user.NormalizedUserName = newPassword.EmailAddress.ToUpper();
                    user.UpdateOn = DateTime.UtcNow;
                    user.LastPasswordChangedDate = DateTime.UtcNow;
                    _db.Users.Update(user);
                    await _db.SaveChangesAsync(ct);

                    await RecordAuditAsync(user, myIP, "Change Password", "User changed their password successfully.");
                    TempData[SD.Success] = "Password changed successfully.";
                    return RedirectToAction("Login", "Account");
                }
                else
                {
                    foreach (var error in resetResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }

                TempData[SD.Error] = "Error: Please contact Admin.";
                await RecordAuditAsync(user, myIP, "ChangePassword", "Error occurred during password reset.");
                return View();
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "Error: Please contact Admin.";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "ChangePassword", $"User tried changing their password on first logon but had the following error: {ex.Message}", ct);
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> UnverifiedUserDetails(string personRegNumber, CancellationToken ct)
        {
            // Fetch the user by ID
            var user = await _db.Users
                .Where(u => u.PersonRegNumber == personRegNumber && u.ApplicationStatus == Status.AwaitingApproval)
                .FirstOrDefaultAsync(ct);

            if (user == null)
            {
                TempData["Error"] = "The user does not exist or is not awaiting approval.";
                return RedirectToAction("UnverifiedUsers"); // Redirect to the list of unverified users
            }

            // Fetch possible matches for the user
            var possibleMatches = await _db.Users
                .Where(u =>
                    EF.Functions.Like(u.FirstName, $"%{user.FirstName}%") &&
                    EF.Functions.Like(u.Surname, $"%{user.Surname}%") &&
                    u.PersonYearOfBirth == user.PersonYearOfBirth &&
                    u.OutwardPostcode == user.OutwardPostcode &&
                    u.PersonRegNumber != personRegNumber // Exclude the user themselves
                )
                .ToListAsync(ct);

            // Create a view model to pass user and matches
            var viewModel = new UnverifiedUserDetailsViewModel
            {
                User = user,
                PossibleMatches = possibleMatches
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(AccountViewModel newPassword, CancellationToken ct)
        {
            try
            {
                string myIP = _requestIpHelper.GetRequestIp();
                var emailToDecrypt = protector.Unprotect(newPassword.Email);
                var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == emailToDecrypt, ct);

                if (user == null)
                {
                    await RecordAuditAsync(user, myIP, "ChangePassword", "User not found in database.");
                    return Json(new { success = false, message = "Error updating your password: Please contact Admin." });
                }

                if (!await _userManager.CheckPasswordAsync(user, newPassword.OldPassword))
                {
                    await RecordAuditAsync(user, myIP, "ChangePassword", "Incorrect current password.");
                    return Json(new { success = false, message = "Current password does not match." });
                }

                if (newPassword.OldPassword == newPassword.Password)
                {
                    await RecordAuditAsync(user, myIP, "ChangePassword", "New password same as current password.");
                    return Json(new { success = false, message = "New password cannot be the same as the current password." });
                }

                var result = await passwordValidator.ValidateAsync(_userManager, user, newPassword.Password);
                if (!result.Succeeded)
                {
                    return Json(new { success = false, message = "New password does not meet requirements." });
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, newPassword.Password);
                if (resetResult.Succeeded)
                {
                    user.PasswordHash = passwordHasher.HashPassword(user, newPassword.Password);
                    user.ForcePasswordChange = false;
                    user.UpdateOn = DateTime.UtcNow;
                    user.UserName = newPassword.Email;
                    user.NormalizedUserName = newPassword.Email.ToUpper();
                    user.LastPasswordChangedDate = DateTime.UtcNow;
                    _db.Users.Update(user);
                    await _db.SaveChangesAsync(ct);

                    await RecordAuditAsync(user, myIP, "ChangePassword", "Password changed successfully.");
                    return Json(new { success = true, message = "Password changed successfully!" });
                }

                return Json(new { success = false, message = "Error: Please contact Admin." });
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "ChangePassword", $"Error: {ex.Message}", ct);
                return Json(new { success = false, message = "Unexpected error occurred." });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateAccount(DeactivateAccountViewModel model, CancellationToken ct)
        {
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");

            var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.Email == currentUserEmail && a.IsActive == true);
            if (currentUser == null)
            {
                TempData["Error"] = "Error getting account details.";
                return RedirectToAction("Login", "Account");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.PersonRegNumber == model.PersonRegNumber && u.IsActive == true, ct);
            var dependents = await _db.Dependants.Where(d => d.UserId == model.UserId && d.IsActive == true).ToListAsync(ct);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index", "Account");
            }
            //Deactivate the main user
            user.IsActive = false;
            user.DeactivationDate = DateTime.UtcNow;
            user.DeactivationReason = model.DeactivationReason;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            if (model.DeactivateWithDependents)
            {
                foreach (var dependent in dependents)
                {
                    dependent.IsActive = false;
                    dependent.DeactivationReason = model.DeactivationReason;
                    _db.Dependants.Update(dependent);

                    // Log deactivation note for each dependent
                    var dependentNote = new NoteHistory
                    {
                        NoteTypeId = 1,
                        PersonRegNumber = dependent.PersonRegNumber,
                        Description = $"Dependent {dependent.PersonName} (Reg#: {dependent.PersonRegNumber}) was deactivated along with the main account ({user.FirstName + " " + user.Surname}).",
                        CreatedBy = "Portal@umojawetu.com",
                        CreatedByName = "Portal",
                        DateCreated = DateTime.UtcNow
                    };
                    await _db.NoteHistory.AddAsync(dependentNote);
                    await _db.SaveChangesAsync(ct);
                }
            }
            // Log deactivation note for the main user
            var deactivationNote = new NoteHistory
            {
                NoteTypeId = 1, // System log
                PersonRegNumber = model.PersonRegNumber,
                Description = $"User {user.FirstName} {user.Surname} (Reg#: {model.PersonRegNumber}) has deactivated their account. Reason: {model.DeactivationReason}",
                CreatedBy = "Portal@umojawetu.com",
                CreatedByName = "Portal",
                DateCreated = DateTime.UtcNow
            };
            await _db.NoteHistory.AddAsync(deactivationNote);

            // Save all changes in one call for better performance
            await _db.SaveChangesAsync(ct);
            // Record Audit Log
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Account Deactivation",
                $"User deactivated their account. Reg#: {model.PersonRegNumber}. Reason: {model.DeactivationReason}. Dependents deactivated: {model.DeactivateWithDependents}", ct);



            // Clear session and sign out
            HttpContext.Session.Clear();
            await _signInManager.SignOutAsync();

            TempData["Success"] = "Your account has been successfully deactivated.";
            return RedirectToAction(nameof(Login));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestToCancelMembership(DeactivateAccountViewModel model, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(model.PersonRegNumber))
            {
                TempData["Error"] = "Invalid request.";
                return RedirectToAction("Index", "Account");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.PersonRegNumber == model.PersonRegNumber && u.IsActive == true, ct);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index", "Account");
            }
            //  Check if a request already exists
            var existingRequest = await _db.RequestToCancelMembership
                .FirstOrDefaultAsync(r => r.PersonRegNumber == model.PersonRegNumber && r.Status == "Pending", ct);

            if (existingRequest != null)
            {
                TempData["Error"] = "You already have a pending cancellation request.";
                return RedirectToAction("Index", "Account");
            }
            // Save cancellation request
            var request = new RequestToCancelMembership
            {
                PersonRegNumber = model.PersonRegNumber,
                UserId = model.UserId,
                CancellationReason = model.DeactivationReason,
                CancelWithFamilyMembers = model.DeactivateWithDependents,
                DateRequested = DateTime.UtcNow,
                Status = "Pending"
            };

            await _db.RequestToCancelMembership.AddAsync(request);
            await _db.SaveChangesAsync(ct);

            // Log request
            var requestNote = new NoteHistory
            {
                NoteTypeId = 1, 
                PersonRegNumber = model.PersonRegNumber,
                Description = $"User {user.FirstName} {user.Surname} (Reg#: {model.PersonRegNumber}) requested membership cancellation. Reason: {model.DeactivationReason}",
                CreatedBy = "Portal@umojawetu.com",
                CreatedByName = "Portal",
                DateCreated = DateTime.UtcNow
            };

            await _db.NoteHistory.AddAsync(requestNote);
            await _db.SaveChangesAsync(ct);


            const string pathToFile = @"EmailTemplate/CancelRequest.html";
            string emailBody = await System.IO.File.ReadAllTextAsync(pathToFile, ct);

            //  Replace placeholders in the email template
            emailBody = emailBody.Replace("{{UserName}}", $"{user.FirstName} {user.Surname}")
                                 .Replace("{{PersonRegNumber}}", model.PersonRegNumber)
                                 .Replace("{{Reason}}", model.DeactivationReason)
                                 .Replace("{{DateRequested}}", DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm"));

            //  Send email notification only to `info@umojawetu.com`
            var emailMessage = new PostmarkMessage
            {
                To = "info@umojawetu.com",
                Subject = $"Membership Cancellation Request - {user.FirstName} {user.Surname}",
                HtmlBody = emailBody,
                From = "info@umojawetu.com",
               
            };

            await _postmark.SendMessageAsync(emailMessage, ct);
            TempData["Success"] = "Thank you. your request has been submitted. A member of the team will review it soon and notify you accordinly.";

            return RedirectToAction("Index", "Account");
        }


        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> ApproveCancellation(int requestId, string approvalReason, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.Email == email && a.IsActive == true, ct);

            if (currentUser == null)
            {
                TempData[SD.Error] = "Current user not found. Please login.";
                return RedirectToAction(nameof(Login));
            }

            var request = await _db.RequestToCancelMembership.FirstOrDefaultAsync(r => r.Id == requestId && r.Status == "Pending", ct);
            await LoadUserStats(ct);
            if (request == null)
            {
                TempData["Error"] = "Invalid request or already processed.";

               
                return RedirectToAction(nameof(CancellationRequests));
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.PersonRegNumber == request.PersonRegNumber && u.IsActive == true, ct);

            if (user != null)
            {

                // Save note history
                var note = new NoteHistory
                {
                    NoteTypeId = 1,
                    PersonRegNumber = request.PersonRegNumber,
                    Description = $"Membership cancellation approved by Admin: {currentUser.Email}. Reason: {approvalReason}",
                    CreatedBy = "Portal@umojawetu.com",
                    CreatedByName = "Portal",
                    DateCreated = DateTime.UtcNow
                };

                await _db.NoteHistory.AddAsync(note, ct);
                await _db.SaveChangesAsync(ct);

                // Move user to DeletedUser table
                var deletedUser = new DeletedUsers
                {
                    UserId = user.UserId,
                    DependentId = user.DependentId,
                    FirstName = user.FirstName,
                    Surname = user.Surname,
                    Email = user.Email,
                    Title = user.Title,
                    ApplicationStatus = user.ApplicationStatus,
                    SuccessorId = user.SuccessorId,
                    Note = user.Note,
                    NoteDate = user.NoteDate,
                    ApprovalDeclinerEmail = user.ApprovalDeclinerEmail,
                    ApprovalDeclinerName = user.ApprovalDeclinerName,
                    IsConsent = user.IsConsent,
                    DateDeleted = DateTime.UtcNow,
                    DateCreated = user.DateCreated,
                    SponsorsMemberName = user.SponsorsMemberName,
                    SponsorsMemberNumber = user.SponsorsMemberNumber,
                    SponsorLocalAdminName = user.SponsorLocalAdminName,
                    SponsorLocalAdminNumber = user.SponsorLocalAdminNumber,
                    PersonYearOfBirth = user.PersonYearOfBirth,
                    PersonRegNumber = user.PersonRegNumber,
                    RegionId = user.RegionId,
                    IsDeleted = user.IsActive,
                    IsDeceased = user.IsDeceased,
                    CityId = user.CityId,
                    OutwardPostcode = user.OutwardPostcode,
                    Reason = request.CancellationReason
                };

                await _db.DeletedUser.AddAsync(deletedUser);
                await _db.SaveChangesAsync(ct);

                // Remove user from Users table
                _db.Users.Remove(user);
                await _db.SaveChangesAsync(ct);
            }

            //dependants details
            var dep = await _db.Dependants.FirstOrDefaultAsync(d => d.PersonRegNumber == request.PersonRegNumber && d.IsActive == true);
            if (dep != null)
            {
                var nameParts = dep.PersonName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                string firstName = nameParts.Length > 0 ? nameParts[0] : "Unknown"; // Always take the first word as FirstName
                string surname = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "Unknown"; // Join remaining words as Surname

                var deletedDependent = new DeletedUsers
                {
                    UserId = dep.UserId,
                    DependentId = dep.Id,
                    FirstName = firstName,
                    Surname = surname,
                    Email = dep.Email,
                    Title = dep.Title ?? 0,                   
                    ApprovalDeclinerEmail = "",
                    ApprovalDeclinerName = "",
                    IsConsent = true,
                    DateDeleted = DateTime.UtcNow,
                    DateCreated = dep.DateCreated,                 
                    PersonYearOfBirth = dep.PersonYearOfBirth,
                    PersonRegNumber = dep.PersonRegNumber,
                    RegionId = dep.RegionId,
                    IsDeleted = dep.IsActive,                   
                    CityId = dep.CityId,
                    OutwardPostcode = dep.OutwardPostcode,
                    Reason = request.CancellationReason
                };

                await _db.DeletedUser.AddAsync(deletedDependent);
                _db.Dependants.Remove(dep);
            }
            await _db.SaveChangesAsync(ct);

            // If IsFamilyToCancel is true, move dependents as well
            if (request.CancelWithFamilyMembers)
            {
                var dependents = await _db.Dependants.Where(d => d.UserId == dep.UserId && d.IsActive == true).ToListAsync(ct);
              
                foreach (var dependent in dependents)
                {
                    var nameParts = dependent.PersonName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    string firstName = nameParts.Length > 0 ? nameParts[0] : "Unknown"; // Always take the first word as FirstName
                    string surname = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "Unknown"; // Join remaining words as Surname

                    var deletedDependent = new DeletedUsers
                    {
                        UserId = dependent.UserId,
                        DependentId = dependent.Id,
                        FirstName = firstName,
                        Surname = surname,
                        Email = dependent.Email,
                        Title = dependent.Title ?? 0,
                        ApplicationStatus = "",                 
                        IsConsent = true,
                        DateDeleted = DateTime.UtcNow,
                        DateCreated = dependent.DateCreated,                        
                        PersonYearOfBirth = dependent.PersonYearOfBirth,
                        PersonRegNumber = dependent.PersonRegNumber,
                        RegionId = dependent.RegionId,
                        IsDeleted = dependent.IsActive,                
                        CityId = dependent.CityId,
                        OutwardPostcode = dependent.OutwardPostcode,
                        Reason = request.CancellationReason
                    };

                    await _db.DeletedUser.AddAsync(deletedDependent);
                    _db.Dependants.Remove(dependent);
                }
                await _db.SaveChangesAsync(ct);
            }

            // Update request status
            request.Status = "Approved";
            request.AdminApprovalDate = DateTime.UtcNow;
            request.AdminApprovalNote = "Approved by admin:"+ currentUser.FirstName + " " + currentUser.Surname + " ( " + currentUser.Email + ") Approval reason:"+  approvalReason;            
            _db.RequestToCancelMembership.Update(request);
            await _db.SaveChangesAsync(ct);

            //send email 
            const string pathToFile = @"EmailTemplate/CancelConfirmation.html";
            string emailBody = await System.IO.File.ReadAllTextAsync(pathToFile, ct);

            //  Replace placeholders in the email template
            emailBody = emailBody.Replace("{{UserName}}", $"{user.FirstName} {user.Surname}")
                                 .Replace("{{PersonRegNumber}}", request.PersonRegNumber)
                                 .Replace("{{Reason}}", request.CancellationReason)
                                 .Replace("{{DateRequested}}", request.DateRequested.ToString("dd-MM-yyyy HH:mm"));

            //  Send email notification to user
            var emailMessageUser = new PostmarkMessage
            {
                To = user.Email,
                Subject = $"Membership Cancellation Confirmation - Umoja Wetu",
                HtmlBody = emailBody,
                From = "info@umojawetu.com"
            };
            
          //  await _postmark.SendMessageAsync(emailMessageUser, ct);
           

            TempData["Success"] = "Membership cancelled successfully and email sent to the member to confirmed.";

            return RedirectToAction(nameof(CancellationRequests));
        }

        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> DeclineCancellation(int requestId, string declineReason, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.Email == email && a.IsActive == true, ct);

            if (currentUser == null)
            {
                TempData[SD.Error] = "Current user not found. Please login.";
                return RedirectToAction(nameof(Login));
            }

            var request = await _db.RequestToCancelMembership.FirstOrDefaultAsync(r => r.Id == requestId && r.Status == "Pending", ct);

            if (request == null)
            {
                TempData["Error"] = "Invalid request.";
                return RedirectToAction("CancellationRequests", "Admin");
            }
            var user = await _db.Dependants.FirstOrDefaultAsync(u => u.PersonRegNumber == request.PersonRegNumber && u.IsActive == true, ct);

            if(user != null)
            {
                // Save note history
                var note = new NoteHistory
                {
                    NoteTypeId = 1,
                    PersonRegNumber = request.PersonRegNumber,
                    Description = $"Cancellation request declined by Admin: {currentUser.Email}. Reason: {declineReason}",
                    CreatedBy = "Portal@umojawetu.com",
                    CreatedByName = "Portal",
                    DateCreated = DateTime.UtcNow
                };

                await _db.NoteHistory.AddAsync(note, ct);
                await _db.SaveChangesAsync(ct);

                request.Status = "Rejected";
                request.AdminApprovalDate = DateTime.UtcNow;
                request.AdminApprovalNote = "Declined by admin:" + currentUser.FirstName + " " + currentUser.Surname + " ( " + currentUser.Email + ") Decliner reason:" + declineReason;
                _db.RequestToCancelMembership.Update(request);
                await _db.SaveChangesAsync(ct);

                //send email
                const string pathToFile = @"EmailTemplate/DeclinedConfirmation.html";
                string emailBody = await System.IO.File.ReadAllTextAsync(pathToFile, ct);

                //  Replace placeholders in the email template
                emailBody = emailBody.Replace("{{UserName}}", $"{user.PersonName}")
                                     .Replace("{{PersonRegNumber}}", request.PersonRegNumber)
                                     .Replace("{{Reason}}", request.CancellationReason)
                                     .Replace("{{DateRequested}}", request.DateRequested.ToString("dd-MM-yyyy HH:mm"));

                //  Send email notification to user
                var emailMessageUser = new PostmarkMessage
                {
                    To = user.Email,
                    Subject = $"Membership Cancellation Request Declined - Umoja Wetu",
                    HtmlBody = emailBody,
                    From = "info@umojawetu.com"
                };

                //await _postmark.SendMessageAsync(emailMessageUser, ct);
                TempData["Success"] = "Cancellation request declined.";

                return RedirectToAction(nameof(CancellationRequests));
            }
            else
            {

                //await _postmark.SendMessageAsync(emailMessageUser, ct);
                TempData["Error"] = "Cancellation request failed user not found.";

                return RedirectToAction(nameof(CancellationRequests));
            }
        }


                    [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> CancellationRequests(CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.Email == email && a.IsActive == true, ct);

            if (currentUser == null)
            {
                TempData[SD.Error] = "Current user not found. Please login.";
                return RedirectToAction(nameof(Login));
            }

            // Fetch all cancellation requests
            var cancelledUsers = await _db.RequestToCancelMembership
                .OrderByDescending(r => r.DateRequested)
                .ToListAsync(ct);

            // Get the User details for each request from Dependants table
            var userRegNumbers = cancelledUsers.Select(c => c.PersonRegNumber).ToList();
            var users = await _db.Dependants
                .Where(u => userRegNumbers.Contains(u.PersonRegNumber))
                .ToDictionaryAsync(u => u.PersonRegNumber, ct);

            // Fetch related dependants
            var relatedDependants = await _db.Dependants
                .Where(d => cancelledUsers.Select(c => c.UserId).Contains(d.UserId) && d.IsActive == true)
                .ToListAsync(ct);

            // Map users with their dependents
            var viewModel = cancelledUsers.Select(request =>
            {
                var user = users.GetValueOrDefault(request.PersonRegNumber); // Fetch user once
                return new CancelledUserViewModel
                {
                    User = request,
                    FullName = user?.PersonName ?? "Unknown",
                    YearOfBirth = user?.PersonYearOfBirth ?? "Unknown",
                    Phone = user?.Telephone ?? "Unknown",
                    DateJoined = user?.DateCreated ?? DateTime.MinValue,
                    Email = user?.Email ?? "Unknown",
                    OutwardPostcode = user?.OutwardPostcode ?? "Unknown",
                    FamilyMembers = relatedDependants.Where(d => d.UserId == request.UserId).ToList()
                };
            }).ToList();


            await LoadUserStats(ct);
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "CancellationRequests", "User navigated to Cancelled Requests page", ct);

            return View(viewModel);
        }

        [Authorize(Roles = RoleList.GeneralAdmin)]
        [HttpGet]
        public async Task<IActionResult> CancelledMembership(CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.Email == email && a.IsActive == true);

            if (currentUser == null)
            {
                TempData[SD.Error] = "Current user not found. Please login.";
                return RedirectToAction(nameof(Login));
            }
            var requests = await _db.DeletedUser                
                .OrderByDescending(r => r.DateCreated)
                .ToListAsync(ct);
            await LoadUserStats(ct);
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "CancellationRequests", "User navigated to CancellationRequests page", ct);

            return View(requests);
        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreMembership(string PersonRegNumber, string RestoreReason, bool RestoreWithFamily, CancellationToken ct)
        {
            try
            {
                var currentUserEmail = HttpContext.Session.GetString("loginEmail");
                var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.Email == currentUserEmail && a.IsActive == true, ct);

                if (currentUser == null)
                {
                    TempData["Error"] = "Current user not found. Please login.";
                    return RedirectToAction(nameof(Login));
                }

                if (string.IsNullOrEmpty(PersonRegNumber) || string.IsNullOrEmpty(RestoreReason))
                {
                    TempData["Error"] = "Invalid request. Please provide a valid reason.";
                    return RedirectToAction("CancelledMembership");
                }



                var user = await _db.DeletedUser.FirstOrDefaultAsync(u => u.PersonRegNumber == PersonRegNumber, ct);
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("CancelledMembership");
                }

                var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == user.Email, ct);
               
                if (existingUser == null)
                {
                    // Generate a random password
                    var randomPassword = Guid.NewGuid().ToString("N").Substring(0, 12);
                    var passwordHash = _userManager.PasswordHasher.HashPassword(null, randomPassword);

                    // Restore User to AspNetUsers
                    var restoredUser = new User
                    {
                        UserId = user.UserId,
                        FirstName = user.FirstName,
                        Surname = user.Surname,
                        Title = user.Title,
                        
                        Note = user.Note,
                        NoteDate = user.NoteDate,

                        ApprovalDeclinerName = user.ApprovalDeclinerName,
                        ApprovalDeclinerEmail = user.ApprovalDeclinerEmail,
                        IsConsent = user.IsConsent,
                        DateCreated = user.DateCreated,
                        CreatedBy = "System Restored",
                        UpdateOn = DateTime.UtcNow,
                        ForcePasswordChange = true,
                        SponsorLocalAdminName = user.SponsorLocalAdminName,
                        SponsorLocalAdminNumber = user.SponsorLocalAdminNumber,
                        SponsorsMemberName = user.SponsorsMemberName,
                        SponsorsMemberNumber   = user.SponsorsMemberNumber,
                        RegionId = user.RegionId,
                        CityId = user.CityId,
                        OutwardPostcode = user.OutwardPostcode,
                        UserName = user.Email,
                        Email = user.Email,
                        NormalizedEmail = user.Email.ToUpper(),                        
                        NormalizedUserName = user.Email.ToUpper(),
                        ApplicationStatus = user.ApplicationStatus,
                        EmailConfirmed = true,
                        PersonYearOfBirth = user.PersonYearOfBirth,
                        PersonRegNumber = user.PersonRegNumber,
                        IsActive = true,                        
                        PasswordHash = passwordHash,
                        SecurityStamp = Guid.NewGuid().ToString(),                      
                        LockoutEnabled = false,
                        AccessFailedCount = 0
                    };

                    await _db.Users.AddAsync(restoredUser, ct);
                    await _db.SaveChangesAsync(ct); //  Save first so the user ID is generated

                    //  Now that the user exists in the database, assign the role
                    await _userManager.AddToRoleAsync(restoredUser, RoleList.Member);


                }

                //  Restore or create Dependant record for the restored user
                var existingDependant = await _db.Dependants.FirstOrDefaultAsync(d => d.PersonRegNumber == user.PersonRegNumber, ct);
                if (existingDependant == null)
                {
                    var newDependant = new Dependant
                    {
                        UserId = user.UserId,
                        PersonName = $"{user.FirstName} {user.Surname}",
                        PersonYearOfBirth = user.PersonYearOfBirth,
                        PersonRegNumber = user.PersonRegNumber,
                        DateCreated = user.DateCreated,
                        CreatedBy = "System Restored",
                        IsActive = true,
                        HasChangedFamily = false,
                        Email = user.Email,
                        Gender = 1,
                        Telephone = "", 
                        Title = user.Title,                    
                        CityId = user.CityId,
                        RegionId = user.RegionId,
                        OutwardPostcode = user.OutwardPostcode,                       
                        DeactivationReason = "",
                    };

                    await _db.Dependants.AddAsync(newDependant, ct);
                    await _db.SaveChangesAsync();
                }

                //  Log the restore action in NoteHistory
                var restoreNote = new NoteHistory
                {
                    NoteTypeId = 1, 
                    PersonRegNumber = PersonRegNumber,
                    Description = $"User {user.FirstName} {user.Surname} (Reg#: {PersonRegNumber}) was restored by {currentUser.FirstName} {currentUser.Surname} {currentUser.Email}. Reason: {RestoreReason}. " +
                                  (RestoreWithFamily ? "All family members were restored." : "Only this member was restored."),
                    CreatedBy = currentUser.Email,
                    CreatedByName = $"{currentUser.FirstName} {currentUser.Surname}",
                    DateCreated = DateTime.UtcNow
                };

                await _db.NoteHistory.AddAsync(restoreNote, ct);
                await _db.SaveChangesAsync(ct);

                //  Restore Family Members if selected
                if (RestoreWithFamily)
                {
                    var familyMembers = await _db.DeletedUser.Where(u => u.UserId == user.UserId && u.PersonRegNumber != user.PersonRegNumber).ToListAsync(ct);

                    foreach (var member in familyMembers)
                    {
                        var existingDependent = await _db.Users.FirstOrDefaultAsync(u => u.Email == member.Email, ct);

                        if (existingDependent == null)
                        {
                            var randomPassword = Guid.NewGuid().ToString("N").Substring(0, 12);
                            var passwordHash = _userManager.PasswordHasher.HashPassword(null, randomPassword);

                            var restoredDependent = new User
                            {
                                UserId = member.UserId,
                                FirstName = member.FirstName,
                                Surname = member.Surname,
                                Title = member.Title,

                                Note = member.Note,
                                NoteDate = member.NoteDate,

                                ApprovalDeclinerName = member.ApprovalDeclinerName,
                                ApprovalDeclinerEmail = member.ApprovalDeclinerEmail,
                                IsConsent = member.IsConsent,
                                DateCreated = member.DateCreated,
                                CreatedBy = "System Restored",
                                UpdateOn = DateTime.UtcNow,
                                ForcePasswordChange = true,
                                SponsorLocalAdminName = member.SponsorLocalAdminName,
                                SponsorLocalAdminNumber = member.SponsorLocalAdminNumber,
                                SponsorsMemberName = member.SponsorsMemberName,
                                SponsorsMemberNumber = member.SponsorsMemberNumber,
                                RegionId = member.RegionId,
                                CityId = member.CityId,
                                OutwardPostcode = member.OutwardPostcode,
                                UserName = member.Email,
                                Email = member.Email,
                                NormalizedEmail = member.Email.ToUpper(),
                                NormalizedUserName = member.Email.ToUpper(),
                                ApplicationStatus = member.ApplicationStatus,
                                EmailConfirmed = true,
                                PersonYearOfBirth = member.PersonYearOfBirth,
                                PersonRegNumber = member.PersonRegNumber,
                                IsActive = true,
                                PasswordHash = passwordHash,
                                SecurityStamp = Guid.NewGuid().ToString(),
                                LockoutEnabled = false,
                                AccessFailedCount = 0
                            };

                            await _db.Users.AddAsync(restoredDependent, ct);
                        }

                        //  Restore Dependant record for the restored dependent
                        var existingDependantMember = await _db.Dependants.FirstOrDefaultAsync(d => d.PersonRegNumber == member.PersonRegNumber, ct);
                        if (existingDependantMember == null)
                        {
                            var newDependent = new Dependant
                            {
                                UserId = member.UserId,
                                PersonName = $"{member.FirstName} {member.Surname}",
                                PersonYearOfBirth = member.PersonYearOfBirth,
                                PersonRegNumber = member.PersonRegNumber,
                                DateCreated = member.DateCreated,
                                CreatedBy = "System Restored",
                                IsActive = true,
                                HasChangedFamily = false,
                                Email = member.Email,
                                Gender = 1,
                                Telephone = "",
                                Title = member.Title,
                                CityId = member.CityId,
                                RegionId = member.RegionId,
                                OutwardPostcode = member.OutwardPostcode,
                                DeactivationReason = "",
                            };

                            await _db.Dependants.AddAsync(newDependent, ct);
                            await _db.SaveChangesAsync();
                        }
                        //  Log the restore action in NoteHistory
                        var restoreNote2 = new NoteHistory
                        {
                            NoteTypeId = 2, // System log
                            PersonRegNumber = member.PersonRegNumber,
                            Description = $"User {member.FirstName} {member.Surname} (Reg#: {member.PersonRegNumber}) has been restored by {currentUser.FirstName} {currentUser.Surname}. Reason: {RestoreReason}. " +
                                          (RestoreWithFamily ? "All family members were restored." : "Only this member was restored."),
                            CreatedBy = currentUser.Email,
                            CreatedByName = $"{currentUser.FirstName} {currentUser.Surname}",
                            DateCreated = DateTime.UtcNow
                        };

                        await _db.NoteHistory.AddAsync(restoreNote2, ct);
                        await _db.SaveChangesAsync(ct);
                    }
                }

                //  Remove from DeletedUser table
                _db.DeletedUser.Remove(user);
                await _db.SaveChangesAsync(ct);

        

                //  Record Audit Log
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Account Restoration",
                    $"Admin {currentUser.FirstName} {currentUser.Surname} restored user {user.FirstName} {user.Surname} (Reg#: {PersonRegNumber}). " +
                    (RestoreWithFamily ? "All related family members were restored." : "Only the individual was restored."),
                    ct);

                TempData["Success"] = RestoreWithFamily
                    ? "User and all related family members have been successfully restored."
                    : "User has been successfully restored.";

                return RedirectToAction(nameof(CancelledMembership));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while restoring the membership.";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "RestoreMembership Error",
                    $"Error restoring user {PersonRegNumber}: {ex.Message}", ct);

                return RedirectToAction(nameof(CancelledMembership));
            }
        }

        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCustomPayment(CustomPayment model, CancellationToken ct)
        {
            try
            {
                ModelState.Remove(nameof(model.UserId));
                ModelState.Remove(nameof(model.CreatedBy));
                ModelState.Remove(nameof(model.CreatedByName));
                ModelState.Remove(nameof(model.DateCreated));

                var email = HttpContext.Session.GetString("loginEmail");
                if (email == null)
                {
                    TempData[SD.Error] = "Session expired. Please log in again.";
                    return RedirectToAction("Index", "Home");
                }

                var currentUser = await _userManager.FindByEmailAsync(email);
                if (currentUser == null)
                {
                    TempData[SD.Error] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if the person exists in the system
                var dependent = await _db.Dependants.FirstOrDefaultAsync(d => d.PersonRegNumber == model.PersonRegNumber, ct);
                if (dependent == null)
                {
                    TempData[SD.Error] = "Dependent not found.";

                    return RedirectToAction("Details", "Admin", new { personRegNumber = model.PersonRegNumber });
                }

                // Check if the same reduction already exists
                var existingPayment = await _db.CustomPayment
                    .FirstOrDefaultAsync(cp => cp.PersonRegNumber == model.PersonRegNumber && cp.CauseCampaignpRef == model.CauseCampaignpRef, ct);

                if (existingPayment != null)
                {
                    TempData[SD.Error] = "A reduction for this cause already exists.";

                    return RedirectToAction("Details", "Admin", new { personRegNumber = model.PersonRegNumber });
                }

                // Fetch cause details for reference
                var cause = await _db.Cause.FirstOrDefaultAsync(rd => rd.CauseCampaignpRef == model.CauseCampaignpRef, ct);
                if (cause == null)
                {
                    TempData[SD.Error] = "Selected cause does not exist.";

                    return RedirectToAction("Details", "Admin", new { personRegNumber = model.PersonRegNumber });
                }

                //  Create a new Custom Payment Record
                var newCustomPayment = new CustomPayment
                {
                    PersonRegNumber = model.PersonRegNumber,
                    UserId = dependent.UserId,                    
                    CauseCampaignpRef = model.CauseCampaignpRef,
                    ReduceFees = model.ReduceFees,
                    Reason = model.Reason,
                    CreatedBy = currentUser.Email,
                    CreatedByName = $"{currentUser.FirstName} {currentUser.Surname}",
                    DateCreated = DateTime.UtcNow
                };

                _db.CustomPayment.Add(newCustomPayment);
                await _db.SaveChangesAsync(ct);

                //  Add an audit log
                
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "AddCustomPayment",
                    $"Custom payment reduction added for {dependent.PersonName} (Reg#: {dependent.PersonRegNumber}) - " +
                    $"Cause: {cause.CauseCampaignpRef}, Amount: £{model.ReduceFees}, Reason: {model.Reason}. ", ct);
                //  Add a Note to the Dependent's Records
                var customPaymentNote = new NoteHistory
                {
                    NoteTypeId = 3, // Assuming 2 is the ID for Custom Payment Notes
                    PersonRegNumber = model.PersonRegNumber,
                    Description = $"Custom payment reduction of £{model.ReduceFees} applied for {dependent.PersonName} (Reg#: {dependent.PersonRegNumber}) by {currentUser.FirstName} {currentUser.Surname}. " +
                                  $"Cause: {model.CauseCampaignpRef}. Reason: {model.Reason}.",
                    CreatedBy = currentUser.Email,
                    CreatedByName = $"{currentUser.FirstName} {currentUser.Surname}",
                    DateCreated = DateTime.UtcNow
                };

                await _db.NoteHistory.AddAsync(customPaymentNote, ct);
                await _db.SaveChangesAsync(ct);

                TempData[SD.Success] = "Custom payment reduction added successfully.";
                return RedirectToAction("Details", "Admin", new { personRegNumber = model.PersonRegNumber });

            }
            catch (Exception ex)
            {                
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "AddCustomPayment_Error",
                    $"Error adding custom payment for Reg#: {model.PersonRegNumber}. Error: {ex.Message}", ct);
                TempData[SD.Error] = "An error occurred while processing the request. Please see logs.";
                return RedirectToAction("Details", "Admin", new { personRegNumber = model.PersonRegNumber });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetCauseCampaignRef(int deathId, CancellationToken ct)
        {
            try
            {
                // Find cause based on DeathId
                var cause = await _db.Cause.FirstOrDefaultAsync(c => c.DeathId == deathId, ct);

                if (cause == null)
                {
                    return Json(new { success = false, message = "Cause not found for the selected death." });
                }

                return Json(new { success = true, causeCampaignpRef = cause.CauseCampaignpRef });
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "GetCauseCampaignRef_Error",
                    $"Error fetching CauseCampaignpRef for DeathId: {deathId}. Error: {ex.Message}", ct);

                return Json(new { success = false, message = "Error retrieving data." });
            }
        }

        private async Task LoadUserStats(CancellationToken ct)
        {
            ViewBag.deactivatedUserCount = await _db.Dependants.CountAsync(u => u.IsActive == false, ct);
            ViewBag.cancelledUserCount = await _db.DeletedUser.CountAsync(ct);
            ViewBag.pendingCancellationCount = await _db.RequestToCancelMembership.CountAsync(r => r.Status == "Pending", ct);
            ViewBag.unverifyUser = await _db.Users.CountAsync(u => !u.EmailConfirmed, ct);
        }

    }



}


