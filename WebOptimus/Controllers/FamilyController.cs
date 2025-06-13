using AutoMapper;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyModel;
using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Web;
using WebOptimus.Data;
using WebOptimus.Helpers;
using WebOptimus.Models;
using WebOptimus.Models.ViewModel;
using WebOptimus.Models.ViewModel.Admin;
using WebOptimus.Services;
using WebOptimus.StaticVariables;


namespace WebOptimus.Controllers
{
   
    public class FamilyController : BaseController
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
        public FamilyController(IMapper mapper, UserManager<User> userManager,
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

        //[HttpGet]
        // [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        //public async Task<IActionResult> Index(int pageIndex = 1, int pageSize = 10)
        //{
        //    var email = HttpContext.Session.GetString("loginEmail");
        //    if (email == null)
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }

        //    var currentUser = await _userManager.FindByEmailAsync(email);

        //    try
        //    {
        //        // Fetch the total counts
        //        int totalUsers = await _db.Users.Where(a => a.Email != "seakou2@yahoo.com").CountAsync();
        //         int totalDependents = await _db.Dependants.CountAsync();
        //        int totalunder18Dependents = await _db.Dependants
        //            .ToListAsync()
        //            .ContinueWith(t => t.Result.Count(d => CalculateAge(d.PersonYearOfBirth) < 25));

        //        var users = await _db.Users.ToListAsync();
        //        var dependents = await _db.Dependants.ToListAsync();

        //        // Calculate dependents count and under 25 dependents count for each user
        //        var usersWithDependentsCount = users.Select(user => new
        //        {
        //            User = user,
        //            DependentsCount = dependents.Count(d => d.UserId == user.UserId),
        //            under18DependentsCount = dependents.Count(d => d.UserId == user.UserId && CalculateAge(d.PersonYearOfBirth) < 25)
        //        })
        //        .OrderByDescending(u => u.DependentsCount)
        //        .ToList();

        //        // Paginate the users
        //        var paginatedUsers = usersWithDependentsCount
        //            .Skip((pageIndex - 1) * pageSize)
        //            .Take(pageSize)
        //            .Select(u => u.User)
        //            .ToList();

        //        var userDependents = paginatedUsers.Select(user => new UserDependentsViewModel
        //        {
        //            UserId = user.UserId,
        //            FirstName = user.FirstName,
        //            LastName = user.Surname,
        //            Dependents = dependents.Where(d => d.UserId == user.UserId).ToList(),
        //            TotalUsers = totalUsers,
        //            TotalDependents = totalDependents,
        //            under18DependentsCount = usersWithDependentsCount.FirstOrDefault(u => u.User.UserId == user.UserId)?.under18DependentsCount ?? 0
        //        }).ToList();

        //        var viewModel = new PaginatedList<UserDependentsViewModel>(
        //            userDependents,
        //            totalUsers,
        //            pageIndex,
        //            pageSize);

        //        ViewData["Totalunder18Dependents"] = totalunder18Dependents;

        //        await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Title", "User navigated to Title ", CancellationToken.None);
                
        //        return View(viewModel);
        //    }
        //    catch (Exception ex)
        //    {
        //        await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Title", "ERROR: Navigating to Title page because of: " + ex.Message, CancellationToken.None);
        //        return RedirectToAction("Index", "Admin");
        //    }
        //}
        
        
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
        [HttpGet]
         [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> AddFamily()
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);


            var users = await _db.Users.Select(u => new SelectListItem
            {
                Value = u.UserId.ToString(),
                Text = u.FirstName + " " + u.Surname
            }).ToListAsync();

            var viewModel = new NewDependentViewModel
            {
                Users = users
            };
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Add FamilY", "User navigated to Add Family Page ", CancellationToken.None);

            return View(viewModel);
        }

        [HttpPost]
         [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFamily(NewDependentViewModel model)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);


            try
            {
                Random _random = new Random();
                var num = _random.Next(0, 9999).ToString("D4");

                string fullName = model.PersonName;
                string[] names = fullName.Split(' ');
                string name = names.First();
                string lasName = names.Last();
                var num2 = _random.Next(0, 9999).ToString("D4");

                

                var newDependent = new Dependant
                {
                    UserId = model.UserId,
                    PersonName = model.PersonName,
                    PersonYearOfBirth = model.PersonYearOfBirth,
                    PersonRegNumber = "U" + num2 + name.Substring(0, 1) + lasName.Substring(0, 1),
                    DateCreated = DateTime.UtcNow,
                    CreatedBy = currentUser.Email
                };

                _db.Dependants.Add(newDependent);
                await _db.SaveChangesAsync();

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Add FamilY", "User added new family: " + model.PersonName , CancellationToken.None);
                TempData[SD.Success] = "Added Successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {

                var users = await _db.Users.Select(u => new SelectListItem
                {
                    Value = u.UserId.ToString(),
                    Text = u.FirstName + " " + u.Surname
                }).ToListAsync();

                model.Users = users;

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Add FamilY", "Error adding family because of: " + ex.Message.ToString(), CancellationToken.None);
                TempData[SD.Success] = "Error: Please see logs...";
                return View(model);
            }
        }



        // GET Edit
        [HttpGet]

         [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
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

                var user = await _db.Users.Where(a=>a.UserId == id).FirstOrDefaultAsync();
                if (user == null)
                {
                    TempData[SD.Error] = "User not found...";
                    return RedirectToAction(nameof(Index));
                }

                var dependents = await _db.Dependants.Where(d => d.UserId == user.UserId).ToListAsync();

                var viewModel = new UserDependentsViewModel
                {
                    UserId = user.UserId,
                    FirstName = user.FirstName,
                    LastName = user.Surname,
                    Dependents = dependents
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Family", "ERROR: Navigating to Edit Family page because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Internal Error saving..";
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserDependentsViewModel model, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.FindByEmailAsync(email);


            try
            {
                var user = await _db.Users.Where(a => a.UserId == model.UserId).FirstOrDefaultAsync();
                if (user == null)
                {
                    return NotFound();
                }
                user.FirstName = model.FirstName;
                user.Surname = model.LastName;

                _db.Users.Update(user);

                foreach (var dependent in model.Dependents)
                {
                    var existingDependent = await _db.Dependants.FindAsync(dependent.Id);
                    if (existingDependent != null)
                    {
                        existingDependent.PersonName = dependent.PersonName;
                        existingDependent.PersonYearOfBirth = dependent.PersonYearOfBirth;
                        existingDependent.PersonRegNumber = dependent.PersonRegNumber;
                        _db.Dependants.Update(existingDependent);
                    }
                }

                await _db.SaveChangesAsync();

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Family", "Edit Family name for: " + model.FirstName + "  " + model.LastName, ct);

                TempData[SD.Success] = "Dependents updated successfully";
                return View(model);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Family", "ERROR: Saving Edited Family because of: " + ex.Message.ToString(), ct);

                TempData[SD.Error] = "Internal Error saving..";
                return View(model);
            }
        }


        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> AddDeath(CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                if (email == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                var currentUser = await _userManager.FindByEmailAsync(email);
                if (currentUser == null)
                {
                    TempData["Error"] = $"User not found: {email}";
                    return RedirectToAction("Index", "Home");
                }

                var viewModel = new ReportedDeathViewModel();
                await PopulateDependentsAsync(viewModel, ct);

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Reported Death", "Navigated to Report Death page", ct);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Reported Death", "ERROR: " + ex.Message.ToString(), ct);

                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> AddDeath(ReportedDeathViewModel model, CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                if (email == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                var currentUser = await _userManager.FindByEmailAsync(email);

                // Remove model state errors for properties that are not required
                ModelState.Remove(nameof(model.Deps));
                ModelState.Remove(nameof(model.Cities));
                ModelState.Remove(nameof(model.Regions));
                ModelState.Remove(nameof(model.Dependents));
                ModelState.Remove(nameof(model.DeceasedPhoto));

                if (ModelState.IsValid)
                {
                    // Handle file upload with validation
                    string relativePhotoPath = null;
                    if (model.DeceasedPhoto != null && model.DeceasedPhoto.Length > 0)
                    {
                        var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

                        var fileExtension = Path.GetExtension(model.DeceasedPhoto.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(fileExtension) ||
                            !allowedMimeTypes.Contains(model.DeceasedPhoto.ContentType.ToLowerInvariant()))
                        {
                            ModelState.AddModelError("DeceasedPhoto", "Please upload a valid image file (JPG, JPEG, PNG).");
                            await PopulateDependentsAsync(model, ct);
                            return View(model);
                        }

                        var uploads = Path.Combine(_hostEnvironment.WebRootPath, "DeceasedPhotos");
                        if (!Directory.Exists(uploads))
                        {
                            Directory.CreateDirectory(uploads);
                        }

                        var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                        var fullPhotoPath = Path.Combine(uploads, uniqueFileName);

                        using (var stream = new FileStream(fullPhotoPath, FileMode.Create))
                        {
                            await model.DeceasedPhoto.CopyToAsync(stream);
                        }

                        relativePhotoPath = Path.Combine("DeceasedPhotos", uniqueFileName);
                    }

                    // Mark the dependent as deceased
                    var dependent = await _db.Dependants.FirstOrDefaultAsync(d => d.PersonRegNumber == model.PersonRegNumber && d.IsActive == true, ct);
                    if (dependent != null)
                    {
                        var deceasedNextOfKin = await _db.NextOfKins.FirstOrDefaultAsync(a => a.PersonRegNumber == dependent.PersonRegNumber);

                        var initialStatus = DeathStatus.Approved;

                        var roles = await _userManager.GetRolesAsync(currentUser);
                        bool isGeneralAdmin = roles.Contains(RoleList.GeneralAdmin);
                        bool isRegionalAdmin = roles.Contains(RoleList.RegionalAdmin);
                        bool isLocalAdmin = roles.Contains(RoleList.LocalAdmin);

                        if (isLocalAdmin)
                        {
                            initialStatus = DeathStatus.PendingRegionalApproval;
                        }
                        else if (isRegionalAdmin)
                        {
                            initialStatus = DeathStatus.PendingGeneralApproval;
                        }
                        else if (isGeneralAdmin)
                        {
                            initialStatus = DeathStatus.Approved;
                        }
                        // Check if the death has already been reported
                        var alreadyReported = await _db.ReportedDeath
                            .AnyAsync(r => r.PersonRegNumber == model.PersonRegNumber, ct);

                        if (alreadyReported)
                        {
                            TempData[SD.Error] = "This death has already been reported.";
                            await PopulateDependentsAsync(model, ct);
                            return View(model);
                        }

                        var reportedDeath = new ReportedDeath
                        {
                            UserId = dependent.UserId,
                            DependentId = model.DependentId,
                            DeceasedName = dependent.PersonName,
                            PlaceOfBurial = model.PlaceOfBurial,
                            DeceasedRegNumber = dependent.PersonRegNumber,
                            DeceasedYearOfBirth = dependent.PersonYearOfBirth,
                            DateOfDeath = model.DateOfDeath,
                            DeathLocation = model.DeathLocation,
                            IsApprovedByGeneralAdmin = false,
                            IsApprovedByRegionalAdmin = false,
                            IsRejectedByGeneralAdmin = false,
                            IsRejectedByRegionalAdmin = false,
                            ReportedBy = model.ReportedBy,
                            DateJoined = model.DateJoined,
                            RelationShipToDeceased = model.RelationShipToDeceased,
                            ReporterContactNumber = model.ReporterContactNumber,
                            ReportedOn = model.ReportedOn,
                            DateCreated = DateTime.UtcNow,
                            CreatedBy = currentUser.Email,
                            UpdatedOn = DateTime.UtcNow,
                            PersonRegNumber = dependent.PersonRegNumber,
                            DeceasedNextOfKinName = deceasedNextOfKin?.NextOfKinName ?? "",
                            DeceasedNextOfKinEmail = deceasedNextOfKin?.NextOfKinEmail ?? "",
                            DeceasedNextOfKinPhoneNumber = deceasedNextOfKin?.NextOfKinTel ?? "",
                            DeceasedNextOfKinRelationship = deceasedNextOfKin?.Relationship ?? "",
                            OtherRelevantInformation = model.OtherRelevantInformation ?? "",
                            CityId = model.CityId,
                            RegionId = model.RegionId,
                            DeceasedPhotoPath = relativePhotoPath,
                            Status = initialStatus
                        };

                        _db.ReportedDeath.Add(reportedDeath);
                        await _db.SaveChangesAsync(ct);


                        dependent.IsReportedDead = true;
                        dependent.UpdateOn = DateTime.UtcNow;

                        // Save changes directly
                        _db.Dependants.Update(dependent);
                        await _db.SaveChangesAsync(ct);

                        if (isGeneralAdmin)
                        {
                            // Suspend user if account holder
                            var userAccount = await _db.Users.FirstOrDefaultAsync(u => u.PersonRegNumber == dependent.PersonRegNumber, ct);
                            if (userAccount != null)
                            {
                                userAccount.IsDeceased = true;
                                userAccount.LockoutEnabled = true;
                                userAccount.LockoutEnd = DateTime.UtcNow.AddYears(1000);
                                _db.Users.Update(userAccount);
                                await _db.SaveChangesAsync(ct);
                            }

                            // Copy to ConfirmedDeath
                            var confirmedDeath = new ConfirmedDeath
                            {
                                UserId = dependent.UserId,
                                DeathId = reportedDeath.Id,
                                PersonName = dependent.PersonName,
                                PersonYearOfBirth = dependent.PersonYearOfBirth,
                                PersonRegNumber = dependent.PersonRegNumber,
                                IsConfirmedDead = true,
                                Title = dependent.Title,
                                Gender = dependent.Gender,
                                Telephone = dependent.Telephone,
                                Email = dependent.Email,
                                DateCreated = dependent.DateCreated,
                                RegionId = dependent.RegionId,
                                CityId = dependent.CityId,
                                CreatedBy = reportedDeath.CreatedBy,
                                UpdateOn = DateTime.UtcNow,
                            };

                            _db.ConfirmedDeath.Add(confirmedDeath);
                            await _db.SaveChangesAsync(ct);

                            _db.Dependants.Remove(dependent);
                            await _db.SaveChangesAsync(ct);

                            if (userAccount != null)
                            {
                                var deletedUser = new DeletedUsers
                                {
                                    UserId = userAccount.UserId,
                                    DependentId = userAccount.DependentId,
                                    FirstName = userAccount.FirstName,
                                    Surname = userAccount.Surname,
                                    Email = userAccount.Email,
                                    Title = userAccount.Title,
                                    ApplicationStatus = userAccount.ApplicationStatus,
                                    SuccessorId = userAccount.SuccessorId,
                                    Note = userAccount.Note,
                                    NoteDate = userAccount.NoteDate,
                                    ApprovalDeclinerEmail = userAccount.ApprovalDeclinerEmail,
                                    ApprovalDeclinerName = userAccount.ApprovalDeclinerName,
                                    IsConsent = userAccount.IsConsent,
                                    DateDeleted = DateTime.UtcNow,
                                    DateCreated = userAccount.DateCreated,
                                    SponsorsMemberName = userAccount.SponsorsMemberName,
                                    SponsorsMemberNumber = userAccount.SponsorsMemberNumber,
                                    SponsorLocalAdminName = userAccount.SponsorLocalAdminName,
                                    SponsorLocalAdminNumber = userAccount.SponsorLocalAdminNumber,
                                    PersonYearOfBirth = userAccount.PersonYearOfBirth,
                                    PersonRegNumber = userAccount.PersonRegNumber,
                                    RegionId = userAccount.RegionId,
                                    CityId = userAccount.CityId,
                                    OutwardPostcode = userAccount.OutwardPostcode,
                                    IsDeleted = true,
                                    IsDeceased = true,
                                    Reason = "User Confirmed Dead"
                                };

                                _db.DeletedUser.Add(deletedUser);
                                await _db.SaveChangesAsync(ct);
                                _db.Users.Remove(userAccount);
                                await _db.SaveChangesAsync(ct);
                            }

                            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Auto-Approval", "General Admin reported and approved death directly: " + dependent.PersonName + " #" + dependent.Id, ct);
                        }
                        var isTest = await _db.Settings
                            .Where(s => s.Name == "Is Test Environment")
                            .Select(s => s.IsActive)
                            .FirstOrDefaultAsync(ct);
                        if (!isTest)
                        {

                            await NotifyRegionalAdmins(reportedDeath, currentUser, ct);
                        }


                        await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "New Death", "Reported new death successfully: Deceased Name: " + reportedDeath.DeceasedName + " ID: " + reportedDeath.Id, ct);
                    }

                    TempData[SD.Success] = "Death Reported Successfully.";
                    return RedirectToAction("ReportedDeaths", "Family");
                }

                await PopulateDependentsAsync(model, ct);
                return View(model);
            }
            catch (Exception ex)
            {
                var email2 = HttpContext.Session.GetString("loginEmail");
                if (email2 == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                var currentUser2 = await _userManager.FindByEmailAsync(email2);

                await RecordAuditAsync(currentUser2, _requestIpHelper.GetRequestIp(), "New Death", "Error adding New Death because: " + ex.Message, ct);

                return View(model);
            }
        }

        private async Task NotifyRegionalAdmins(ReportedDeath reportedDeath, User currentUser, CancellationToken ct)
        {
            var userList = await _db.Users.ToListAsync(ct);
            var userRoles = await _db.UserRoles.ToListAsync(ct);
            var roles = await _db.Roles.ToListAsync(ct);

            var regionalAdminRoleId = roles.FirstOrDefault(r => r.Name == RoleList.RegionalAdmin)?.Id;
            if (regionalAdminRoleId == null) return;

            var regionalAdmins = userList
                .Where(u => userRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == regionalAdminRoleId) && u.RegionId == reportedDeath.RegionId)
                .ToList();

            if (!regionalAdmins.Any()) return;

           
            //const string templatePath = @"EmailTemplate/RegionalDeathNotification.html";
            const string templatePath = @"EmailTemplate/test.html"; // Use actual template
            string htmlBody = await System.IO.File.ReadAllTextAsync(templatePath, ct);

            //  Replace placeholders in email template
            htmlBody = htmlBody
                .Replace("{{deceasedName}}", reportedDeath.DeceasedName)
                .Replace("{{dateOfDeath}}", reportedDeath.DateOfDeath.ToString("dd/MM/yyyy") ?? "Unknown")
                .Replace("{{reportedBy}}", reportedDeath.ReportedBy)
                .Replace("{{contact}}", reportedDeath.ReporterContactNumber)
                .Replace("{{region}}", reportedDeath.RegionId.ToString())
                .Replace("{{user}}", currentUser.FirstName + " " + currentUser.Surname)
                .Replace("{{ref}}", "001");

            var reg = await _db.Region.FirstOrDefaultAsync(a => a.Id == reportedDeath.RegionId, ct);
            string regionName = reg?.Name ?? "Unknown";

            //  Prepare a single batch email message for all admins
            var emailMessages = regionalAdmins.Select(admin => new PostmarkMessage
            {
                To = admin.Email,
                Subject = $"Death Reported in Region {regionName}",
                HtmlBody = htmlBody,
                Cc = regionalAdmins.First().Email == admin.Email ? "info@umojawetu.com" : null, // Only add CC once
                From = "info@umojawetu.com",
                MessageStream = "broadcast",
            }).ToList();

            //  Send batch emails efficiently
            await _postmark.SendBatchMessagesAsync(emailMessages, ct);

            //  Log notification
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(),
                "Regional Death Notification Sent",
                $"Notified {regionalAdmins.Count} regional admins for region {regionName} about reported death {reportedDeath.DeceasedName}.", ct);
        }


        private async Task NotifyGeneralAdmins(ReportedDeath reportedDeath, User currentUser, CancellationToken ct)
        {
            var userList = await _db.Users.ToListAsync(ct);
            var userRoles = await _db.UserRoles.ToListAsync(ct);
            var roles = await _db.Roles.ToListAsync(ct);

            //  Get the Role ID for GeneralAdmin
            var generalAdminRoleId = roles.FirstOrDefault(r => r.Name == RoleList.GeneralAdmin)?.Id;
            if (generalAdminRoleId == null) return; // Exit if no GeneralAdmin role found

            //  Find all users with GeneralAdmin role (No Region Filter)
            var generalAdmins = userList
                .Where(u => userRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == generalAdminRoleId))
                .ToList();

            if (!generalAdmins.Any()) return; // Exit if no GeneralAdmins found

            //  Load email template
            const string templatePath = @"EmailTemplate/test.html";
            string htmlBody = await System.IO.File.ReadAllTextAsync(templatePath, ct);

            //  Replace placeholders in email template
            htmlBody = htmlBody
                .Replace("{{deceasedName}}", reportedDeath.DeceasedName)
                .Replace("{{dateOfDeath}}", reportedDeath.DateOfDeath.ToString("dd/MM/yyyy") ?? "Unknown")
                .Replace("{{reportedBy}}", reportedDeath.ReportedBy)
                .Replace("{{contact}}", reportedDeath.ReporterContactNumber)
                .Replace("{{region}}", reportedDeath.RegionId.ToString())
                .Replace("{{user}}", currentUser.FirstName + " " + currentUser.Surname)
                .Replace("{{ref}}", "002");

            var reg = await _db.Region.FirstOrDefaultAsync(a => a.Id == reportedDeath.RegionId, ct);
            string regionName = reg?.Name ?? "Unknown";

            //  Prepare a single batch email message for all admins
            var emailMessages = generalAdmins.Select(admin => new PostmarkMessage
            {
                To = admin.Email,
                Subject = $"Death Approved in Region {regionName}",
                HtmlBody = htmlBody,
                Cc = generalAdmins.First().Email == admin.Email ? "info@umojawetu.com" : null, //  Only add CC once
                From = "info@umojawetu.com",
                MessageStream = "broadcast",
            }).ToList();

            //  Send batch emails efficiently
            await _postmark.SendBatchMessagesAsync(emailMessages, ct);

            //  Log notification
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(),
                "General Death Notification Sent",
                $"Notified {generalAdmins.Count} general admins about reported death {reportedDeath.DeceasedName} in region {regionName}.", ct);
        }

   
      
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DependentPayments(CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get the dependents of the current user (the account holder)
            var dependents = await _db.Dependants
                .Where(d => d.UserId == currentUser.UserId)
                .ToListAsync(ct);

            // Fetch outstanding payments for these dependents
            var payments = await _db.Payment
                .Where(p => dependents.Select(d => d.PersonRegNumber).Contains(p.personRegNumber) && !p.HasPaid)
                .ToListAsync(ct);

            // Join Payment with PaymentSession using OurRef
            var paymentViewModels = await (from p in _db.Payment
                                           join ps in _db.PaymentSessions on p.OurRef equals ps.OurRef
                                           where dependents.Select(d => d.PersonRegNumber).Contains(p.personRegNumber) && !p.HasPaid
                                           select new DependentPaymentsViewModel
                                           {
                                               PaymentId = p.Id,
                                               DependentName = p.CreatedBy ?? "Unknown",
                                               Amount = p.Amount,
                                               TransactionFees = ps.TransactionFees,
                                               TotalAmount = ps.TotalAmount,
                                               PaymentDate = p.DateCreated
                                           }).ToListAsync(ct);

            var viewModel = new DependentPaymentsDashboardViewModel
            {
                TotalPayments = paymentViewModels.Count,
                TotalAmount = paymentViewModels.Sum(p => p.TotalAmount),
                Payments = paymentViewModels
            };

            return View(viewModel);
        }

        //private async Task NotifyDependents(Guid accountHolderUserId, CancellationToken ct)
        //{
        //    var dependents = await _db.Dependants
        //        .Where(d => d.UserId == accountHolderUserId)
        //        .ToListAsync(ct);

        //    foreach (var dependent in dependents)
        //    {
        //        // Logic to send notification to each dependent
        //        // This could be an email, SMS, or in-app notification
        //        var message = $"Account holder {dependent.PersonName} has passed away. Please log in to manage any outstanding obligations.";
        //        await _notificationService.SendNotification(dependent.Email, "Account Holder Deceased", message);
        //    }
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        //public async Task<IActionResult> AddDeath(ReportedDeathViewModel model, CancellationToken ct)
        //{
        //    try
        //    {
        //        var email = HttpContext.Session.GetString("loginEmail");
        //        if (email == null)
        //        {
        //            return RedirectToAction("Index", "Home");
        //        }

        //        var currentUser = await _userManager.FindByEmailAsync(email);

        //        // Remove model state errors for properties that are not required
        //        ModelState.Remove(nameof(model.Deps));
        //        ModelState.Remove(nameof(model.Cities));
        //        ModelState.Remove(nameof(model.Regions));
        //        ModelState.Remove(nameof(model.Dependents));

        //        if (ModelState.IsValid)
        //        {
        //            // Handle file upload with validation
        //            string relativePhotoPath = null;
        //            if (model.DeceasedPhoto != null && model.DeceasedPhoto.Length > 0)
        //            {
        //                // Define allowed MIME types and file extensions
        //                var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
        //                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

        //                // Check if the file is an image
        //                var fileExtension = Path.GetExtension(model.DeceasedPhoto.FileName).ToLowerInvariant();
        //                if (!allowedExtensions.Contains(fileExtension) ||
        //                    !allowedMimeTypes.Contains(model.DeceasedPhoto.ContentType.ToLowerInvariant()))
        //                {
        //                    ModelState.AddModelError("DeceasedPhoto", "Please upload a valid image file (JPG, JPEG, PNG).");
        //                    await PopulateDependentsAsync(model, ct);
        //                    return View(model);
        //                }

        //                // Save the file if it passes validation
        //                var uploads = Path.Combine(_hostEnvironment.WebRootPath, "DeceasedPhotos");
        //                if (!Directory.Exists(uploads))
        //                {
        //                    Directory.CreateDirectory(uploads);
        //                }

        //                // Generate a unique file name
        //                var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
        //                var fullPhotoPath = Path.Combine(uploads, uniqueFileName);

        //                using (var stream = new FileStream(fullPhotoPath, FileMode.Create))
        //                {
        //                    await model.DeceasedPhoto.CopyToAsync(stream);
        //                }

        //                // Store only the relative path in the database
        //                relativePhotoPath = Path.Combine("DeceasedPhotos", uniqueFileName);
        //            }

        //            // Mark the dependent as deceased
        //            var dependent = await _db.Dependants.FirstOrDefaultAsync(d => d.Id == model.DependentId, ct);
        //            if (dependent != null)
        //            {
        //                var deceasedNextOfKin = await _db.NextOfKins.FirstOrDefaultAsync(a => a.DependentId == dependent.Id);

        //                // Determine the initial status based on the user's role
        //                var initialStatus = DeathStatus.Approved; // Default for GeneralAdmin

        //                var roles = await _userManager.GetRolesAsync(currentUser);
        //                if (roles.Contains(RoleList.LocalAdmin))
        //                {
        //                    initialStatus = DeathStatus.PendingRegionalApproval;
        //                }
        //                else if (roles.Contains(RoleList.RegionalAdmin))
        //                {
        //                    initialStatus = DeathStatus.PendingGeneralApproval;
        //                }

        //                // Map the view model to the domain model and save to the database
        //                var reportedDeath = new ReportedDeath
        //                {
        //                    UserId = dependent.UserId,
        //                    DependentId = model.DependentId,
        //                    DeceasedName = dependent.PersonName,
        //                    DeceasedRegNumber = dependent.PersonRegNumber,
        //                    DeceasedYearOfBirth = dependent.PersonYearOfBirth,
        //                    DateOfDeath = model.DateOfDeath,
        //                    DeathLocation = model.DeathLocation,
        //                    IsApprovedByGeneralAdmin = false,
        //                    IsApprovedByRegionalAdmin = false,
        //                    IsRejectedByGeneralAdmin = false,
        //                    IsRejectedByRegionalAdmin = false,
        //                    ReportedBy = model.ReportedBy,
        //                    DateJoined = model.DateJoined,
        //                    RelationShipToDeceased = model.RelationShipToDeceased,
        //                    ReporterContactNumber = model.ReporterContactNumber,
        //                    ReportedOn = model.ReportedOn,
        //                    DateCreated = DateTime.UtcNow,
        //                    CreatedBy = currentUser.Email,
        //                    UpdatedOn = DateTime.UtcNow,
        //                    DeceasedNextOfKinName = deceasedNextOfKin?.NextOfKinName ?? "",
        //                    DeceasedNextOfKinEmail = deceasedNextOfKin?.NextOfKinEmail ?? "",
        //                    DeceasedNextOfKinPhoneNumber = deceasedNextOfKin?.NextOfKinTel ?? "",
        //                    DeceasedNextOfKinRelationship = deceasedNextOfKin?.Relationship ?? "",
        //                    OtherRelevantInformation = model.OtherRelevantInformation ?? "",
        //                    CityId = model.CityId,
        //                    RegionId = model.RegionId,                           
        //                    DeceasedPhotoPath = relativePhotoPath,
        //                    Status = initialStatus // Set the initial status based on role
        //                };

        //                _db.ReportedDeath.Add(reportedDeath);
        //                await _db.SaveChangesAsync(ct);

        //                var getAllDeps = await _db.Dependants.Where(a => a.UserId == model.UserId).ToListAsync();

        //                foreach (var i in getAllDeps)
        //                {
        //                    //if (i.Id == model.DependentId)
        //                    //{
        //                    //    i.IsDead = true;
        //                    //    i.NumberOfDependants--;
        //                    //    i.UpdateOn = DateTime.UtcNow;
        //                    //    _db.Dependants.Update(i);
        //                    //    await _db.SaveChangesAsync(ct);
        //                    //}
        //                    //else
        //                    //{
        //                    //    i.NumberOfDependants--;
        //                    //    _db.Dependants.Update(i);
        //                    //    await _db.SaveChangesAsync(ct);
        //                    //}
        //                    if (i.Id == model.DependentId)
        //                    {
        //                        i.IsReportedDead = true;
        //                        i.UpdateOn = DateTime.UtcNow;
        //                        _db.Dependants.Update(i);
        //                        await _db.SaveChangesAsync(ct);
        //                    }
        //                    else
        //                    {

        //                        _db.Dependants.Update(i);
        //                        await _db.SaveChangesAsync(ct);
        //                    }
        //                }
        //            }

        //            TempData["Success"] = "Death Reported Successfully.";
        //            return RedirectToAction("ReportedDeaths", "Family");
        //        }

        //        // Populate dropdowns in case of an error
        //        await PopulateDependentsAsync(model, ct);
        //        return View(model);
        //    }catch(Exception ex)
        //    {
        //        return View(model);
        //    }
        //}

        private async Task PopulateDependentsAsync(ReportedDeathViewModel model, CancellationToken ct)
            {
                var email = HttpContext.Session.GetString("loginEmail");
                var currentUser = await _userManager.FindByEmailAsync(email);

                // Get the roles of the current user
                var roles = await _userManager.GetRolesAsync(currentUser);

                // Fetch user details to determine filtering criteria
                var user = await _db.Users
                    .Where(u => u.Id == currentUser.Id)
                    .Select(u => new { u.CityId, u.RegionId })
                    .FirstOrDefaultAsync(ct);

                IQueryable<Dependant> query = _db.Dependants.Where(a=>a.IsActive == true);

                if (roles.Contains(RoleList.RegionalAdmin))
                {
                    // Filter dependents by region for RegionalAdmin
                    query = query.Where(d => _db.Users.Any(u => u.UserId == d.UserId && u.RegionId == user.RegionId));
                }
                else if (roles.Contains(RoleList.LocalAdmin))
                {
                    // Filter dependents by city for LocalAdmin
                    query = query.Where(d => _db.Users.Any(u => u.UserId == d.UserId && u.CityId == user.CityId));
                }

                model.Dependents = await query
                    .OrderBy(d => d.PersonName) // Order dependents by PersonName
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.PersonName
                    })
                    .ToListAsync(ct);
            
        }

      
        private async Task PopulateDropdowns(ReportedDeathViewModel model)
        {
            model.Deps = new SelectList(await _db.Dependants.OrderBy(d => d.PersonName).ToListAsync(), "Id", "PersonName");
            model.Cities = new SelectList(await _db.City.ToListAsync(), "Id", "Name");
            model.Regions = new SelectList(await _db.Region.ToListAsync(), "Id", "Name");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleList.RegionalAdmin + "," + RoleList.GeneralAdmin)]
        public async Task<IActionResult> ApproveSubmission(int id, string approvalNote, CancellationToken ct)
        {
            var reportedDeath = await _db.ReportedDeath.FindAsync(id);

            if (reportedDeath == null)
            {
                TempData["Error"] = "Submission not found.";
                return RedirectToAction("ReportedDeaths", "Family");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var currentRole = HttpContext.Session.GetString("adminuser");

            // Check if the dependent is also an account holder
            var accountHolder = await _db.Users
                .Where(u => u.PersonRegNumber == reportedDeath.PersonRegNumber)
                .FirstOrDefaultAsync(ct);

            if (accountHolder != null)
            {
                // Suspend the user's login if they are the account holder
                accountHolder.IsDeceased = true;  // Mark as deceased 
                accountHolder.LockoutEnabled = true;
                accountHolder.LockoutEnd = DateTime.UtcNow.AddYears(1000);  // Effectively lock the account forever
                _db.Users.Update(accountHolder);
                await _db.SaveChangesAsync(ct);
            }

            if (currentRole == RoleList.RegionalAdmin && reportedDeath.Status == DeathStatus.PendingRegionalApproval)
            {
                reportedDeath.Status = DeathStatus.PendingGeneralApproval;
                reportedDeath.DonationStatus = DonationStatus.PendingNewDonation;
                reportedDeath.RegionalAdminNote = approvalNote;
                reportedDeath.IsApprovedByRegionalAdmin = true;
                reportedDeath.RegionalAdminApprovalDate = DateTime.UtcNow;
                reportedDeath.ApprovedByRegionalAdmin = currentUser.Email;

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "ApproveSubmission", "Regional Admin approved reported death for: " + reportedDeath.DeceasedName + " ReportedDeath ID#" + reportedDeath.Id, ct);

                _db.ReportedDeath.Update(reportedDeath);
                await _db.SaveChangesAsync(ct);
                //send email to umojawetu.
                var isTest = await _db.Settings
                          .Where(s => s.Name == "Is Test Environment")
                          .Select(s => s.IsActive)
                          .FirstOrDefaultAsync(ct);
                if (!isTest)
                {
                    await NotifyGeneralAdmins(reportedDeath, currentUser, ct);
                }

               
                TempData["Success"] = "Submission approved successfully.";
                return RedirectToAction("ReportedDeaths", "Family");
            }
            else if (currentRole == RoleList.GeneralAdmin && reportedDeath.Status == DeathStatus.PendingGeneralApproval)
            {
                reportedDeath.Status = DeathStatus.Approved;
                reportedDeath.DonationStatus = DonationStatus.PendingNewDonation;
                reportedDeath.GeneralAdminNote = approvalNote;
                reportedDeath.IsApprovedByGeneralAdmin = true;
                reportedDeath.GeneralAdminApprovalDate = DateTime.UtcNow;
                reportedDeath.ApprovedByGeneralAdmin = currentUser.Email;

                // Copy the dependent to ConfirmedDeath table
                var dependent = await _db.Dependants.FirstOrDefaultAsync(d => d.PersonRegNumber == reportedDeath.PersonRegNumber, ct);
                if (dependent != null)
                {
                    var confirmedDeath = new ConfirmedDeath
                    {
                        UserId = dependent.UserId,
                        DeathId = reportedDeath.Id,
                        PersonName = dependent.PersonName,
                        PersonYearOfBirth = dependent.PersonYearOfBirth,
                        PersonRegNumber = dependent.PersonRegNumber,
                        IsConfirmedDead = true,
                        Title = dependent.Title,
                        Gender = dependent.Gender,
                        Telephone = dependent.Telephone,
                        Email = dependent.Email,
                        DateCreated = dependent.DateCreated,
                        RegionId = dependent.RegionId,
                        CityId = dependent.CityId,
                        CreatedBy = reportedDeath.CreatedBy,
                        UpdateOn = DateTime.UtcNow,
                    };

                    _db.ConfirmedDeath.Add(confirmedDeath);
                    await _db.SaveChangesAsync(ct);

                    _db.Dependants.Remove(dependent);
                    await _db.SaveChangesAsync(ct);
                    // Copy user account if exists
                    var userAccount = await _db.Users.FirstOrDefaultAsync(d => d.PersonRegNumber == reportedDeath.PersonRegNumber, ct);
                    if (userAccount != null)
                    {
                        var userData = new DeletedUsers
                        {
                            UserId = userAccount.UserId,
                            DependentId = userAccount.DependentId,
                            FirstName = userAccount.FirstName,
                            Surname = userAccount.Surname,
                            Email = userAccount.Email,
                            Title = userAccount.Title,
                            ApplicationStatus = userAccount.ApplicationStatus,
                            SuccessorId = userAccount.SuccessorId,
                            Note = userAccount.Note,
                            NoteDate = userAccount.NoteDate,
                            ApprovalDeclinerEmail = userAccount.ApprovalDeclinerEmail,
                            ApprovalDeclinerName = userAccount.ApprovalDeclinerName,
                            IsConsent = userAccount.IsConsent,
                            DateDeleted = userAccount.DateCreated,
                            DateCreated = userAccount.DateCreated,
                            SponsorsMemberName = userAccount.SponsorsMemberName,
                            SponsorsMemberNumber = userAccount.SponsorsMemberNumber,
                            SponsorLocalAdminName = userAccount.SponsorLocalAdminName,
                            SponsorLocalAdminNumber = userAccount.SponsorLocalAdminNumber,
                            PersonYearOfBirth = userAccount.PersonYearOfBirth,
                            PersonRegNumber = userAccount.PersonRegNumber,
                            RegionId = userAccount.RegionId,
                            IsDeleted = userAccount.IsActive,
                            IsDeceased = userAccount.IsDeceased,
                            CityId = userAccount.CityId,
                            OutwardPostcode = userAccount.OutwardPostcode,
                            Reason = "User Confirmed Dead"
                        };

                        _db.DeletedUser.Add(userData);
                        await _db.SaveChangesAsync(ct);

                        _db.Users.Remove(userAccount);
                        await _db.SaveChangesAsync(ct);
                    }

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "ApproveSubmission", "Dependent copied to ConfirmedDeath table: " + dependent.PersonName + " Dependent ID#" + dependent.Id, ct);
                }

                _db.ReportedDeath.Update(reportedDeath);
                await _db.SaveChangesAsync(ct);            

                TempData["Success"] = "Submission approved successfully.";
                return RedirectToAction("ReportedDeaths", "Family");
            }

            TempData["Error"] = "You do not have permission to approve this submission.";
            return RedirectToAction("ReportedDeaths", "Family");
        }


        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> ReportedDeaths(string filter = null, int pageIndex = 1, int pageSize = 10)
        {
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");

            if (currentUserEmail == null)
            {
                TempData["Error"] = "Session does not contain loginEmail.";
                return RedirectToAction("Logout", "Account");
            }

            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
            if (currentUser == null)
            {
                TempData["Error"] = $"User not found: {currentUserEmail}";
                return RedirectToAction("Logout", "Account");
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var currentUserRole = userRoles.FirstOrDefault();

            IQueryable<ReportedDeath> query = _db.ReportedDeath;

            if (currentUserRole == RoleList.GeneralAdmin)
            {
                if (filter == "Approved")
                {
                    query = query.Where(rd => rd.Status == DeathStatus.Approved);
                }
                else if (filter == "Pending")
                {
                    query = query.Where(rd => rd.Status == DeathStatus.PendingRegionalApproval || rd.Status == DeathStatus.PendingGeneralApproval);
                }
                else
                {
               
                    query = query.Where(rd =>
                        rd.Status == DeathStatus.Approved ||
                        rd.Status == DeathStatus.PendingRegionalApproval ||
                        rd.Status == DeathStatus.PendingGeneralApproval ||
                        rd.Status == DeathStatus.Rejected );
                }
            }

            else
            {
                // Local and Regional Admin filtering
                if (filter == "Approved")
                {
                    if (currentUserRole == RoleList.LocalAdmin)
                    {
                        query = query.Where(rd => rd.CityId == currentUser.CityId && rd.Status == DeathStatus.Approved);
                    }
                    else if (currentUserRole == RoleList.RegionalAdmin)
                    {
                        query = query.Where(rd => rd.RegionId == currentUser.RegionId && rd.Status == DeathStatus.Approved);
                    }
                }
                else
                {
                    if (currentUserRole == RoleList.LocalAdmin)
                    {
                        query = query.Where(rd => rd.CityId == currentUser.CityId &&
                            (rd.Status == DeathStatus.PendingRegionalApproval || rd.Status == DeathStatus.PendingGeneralApproval));
                    }
                    else if (currentUserRole == RoleList.RegionalAdmin)
                    {
                        query = query.Where(rd => rd.RegionId == currentUser.RegionId &&
                            (rd.Status == DeathStatus.PendingRegionalApproval || rd.Status == DeathStatus.PendingGeneralApproval));
                    }
                }
            }

            var reportedDeaths = await query
      .Join(_db.City, rd => rd.CityId, c => c.Id, (rd, c) => new { rd, CityName = c.Name })
      .Join(_db.Region, temp => temp.rd.RegionId, r => r.Id, (temp, r) => new { temp.rd, temp.CityName, RegionName = r.Name })
      .GroupJoin(_db.Cause, temp => temp.rd.Id, cause => cause.DeathId, (temp, causes) => new { temp.rd, temp.CityName, temp.RegionName, Cause = causes.FirstOrDefault() })
          .Select(x => new ReportedDeathViewModel
          {
              Id = x.rd.Id,
              UserId = x.rd.UserId,
              DependentId = x.rd.DependentId,
              DependentName = x.rd.DeceasedName,
              DateOfDeath = x.rd.DateOfDeath,
              DeathLocation = x.rd.DeathLocation,
              PlaceOfBurial = x.rd.PlaceOfBurial,
              RelationShipToDeceased = x.rd.RelationShipToDeceased,
              ReporterContactNumber = x.rd.ReporterContactNumber,
              ReportedBy = x.rd.ReportedBy,
              ReportedOn = x.rd.ReportedOn,
              CityName = x.CityName,
              RegionName = x.RegionName,
              Status = x.Cause != null
                  ? (x.Cause.IsClosed ? "Donation Ended"
                      : (x.Cause.IsActive ? "Ongoing Donation" : x.rd.Status))
                  : x.rd.Status
          })
      .ToListAsync();


            var paginatedReportedDeaths = reportedDeaths
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var totalCount = reportedDeaths.Count;

            var viewModel = new PaginatedList<ReportedDeathViewModel>(
                paginatedReportedDeaths,
                totalCount,
                pageIndex,
                pageSize);

            return View(viewModel);
        }


        //[HttpGet]
        //public async Task<IActionResult> GetDependentDetails(int id, CancellationToken ct)
        //{
        //    var dependent = await _db.Dependants
        //        .Where(d => d.Id == id)
        //        .Select(d => new
        //        {
        //            d.PersonRegNumber,
        //            d.PersonYearOfBirth,
        //            d.DateCreated

        //        })
        //        .FirstOrDefaultAsync(ct);

        //    if (dependent == null)
        //    {
        //        return NotFound();
        //    }

        //    return Json(dependent);
        //}

        [HttpGet]
        public async Task<IActionResult> GetDependentDetails(int id, CancellationToken ct)
        {
            // Fetch the dependent details based on the dependent ID
            var dependent = await _db.Dependants
                .Where(d => d.Id == id)
                .Select(d => new
                {
                    d.PersonRegNumber,
                    d.PersonYearOfBirth,
                    d.DateCreated,
                    d.UserId // Get the UserId to fetch user details separately
                })
                .FirstOrDefaultAsync(ct);

            if (dependent == null)
            {
                return NotFound();
            }

            // Fetch the user details using the UserId from the dependent
            var user = await _db.Users
                .Where(u => u.UserId == dependent.UserId) // Correct property for joining
                .Select(u => new
                {
                    u.CityId,
                    u.RegionId
                })
                .FirstOrDefaultAsync(ct);

            if (user == null)
            {
                return NotFound();
            }

            // Fetch the city and region names based on IDs
            var cityName = await _db.City
                .Where(c => c.Id == user.CityId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct);

            var regionName = await _db.Region
                .Where(r => r.Id == user.RegionId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(ct);

            // Combine the dependent and user details into a single object
            var result = new
            {
                dependent.PersonRegNumber,
                dependent.PersonYearOfBirth,
                dependent.DateCreated,
                CityId = user.CityId,
                RegionId = user.RegionId,
                CityName = cityName,
                RegionName = regionName
            };

            return Json(result);
        }


        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);
            if (currentUser == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index", "Home");
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isRegionalAdmin = userRoles.Contains(RoleList.RegionalAdmin);
            var isGeneralAdmin = userRoles.Contains(RoleList.GeneralAdmin);

            var currentRole = HttpContext.Session.GetString("adminuser");
            ViewBag.role = currentRole;
            var getDeath = await _db.ReportedDeath.FirstOrDefaultAsync(a => a.Id == id);
            if (getDeath == null)
            {
                return NotFound();
            }

            // Check if the deceased person is an account holder
        
            var isAccountHolder = await _db.Users.AnyAsync(u => u.PersonRegNumber == getDeath.PersonRegNumber && u.IsActive == true, ct);

            SuccessorViewModel successor = null;
            if (isAccountHolder)
            {
                successor = await _db.Successors
                    .Where(s => s.UserId == getDeath.UserId)
                    .OrderByDescending(s => s.DateCreated)
                    .Select(s => new SuccessorViewModel
                    {
                        Name = s.Name,
                        Relationship = s.Relationship,
                        Email = s.SuccessorEmail,
                        SuccessorTel = s.SuccessorTel,
                        Status = s.Status.ToString()
                    })
                    .FirstOrDefaultAsync(ct);
            }

            var viewModel = new ReportedDeathDetailsViewModel
            {
                Id = id,
                UserId = getDeath.UserId,
                DependentId = getDeath.DependentId,
                PersonName = getDeath.DeceasedName,
                RegisterNumber = getDeath.DeceasedRegNumber,
                OtherRelevantInformation = getDeath.OtherRelevantInformation,
                RegionalAdminApprovalNote = getDeath.RegionalAdminNote,
                ApprovedByGeneralAdmin = isGeneralAdmin ? currentUser.Email : null,
                ApprovedByRegionalAdmin = getDeath.ApprovedByRegionalAdmin,
                RegionalAdminApprovalDate = getDeath.RegionalAdminApprovalDate,
                GeneralAdminApprovalDate = isGeneralAdmin ? DateTime.UtcNow : null,             
                GeneralAdminApprovalNote = getDeath.GeneralAdminNote,
                IsApprovedByGeneralAdmin = getDeath.IsApprovedByGeneralAdmin,
                IsApprovedByRegionalAdmin = getDeath.IsApprovedByRegionalAdmin,
                Status = getDeath.Status,
                PlaceOfBurial = getDeath.PlaceOfBurial,
                YearOfBirth = getDeath.DeceasedYearOfBirth,
                DateJoined = getDeath.DateJoined.ToString("dd/MM/yyyy"),
                DeathLocation = getDeath.DeathLocation,
                DateOfDeath = getDeath.DateOfDeath.ToString("dd/MM/yyyy"),
                ReportedBy = getDeath.ReportedBy,
                RelationShipToDeceased = getDeath.RelationShipToDeceased ?? "",
                CreatedBy = getDeath.CreatedBy,
                ReportedOn = getDeath.ReportedOn.ToString("dd/MM/yyyy"),
                ReporterContactNumber = getDeath.ReporterContactNumber ?? "",
                DeceasedNextOfKinEmail = getDeath.DeceasedNextOfKinEmail ?? "",
                DeceasedNextOfKinRelationship = getDeath.DeceasedNextOfKinRelationship ?? "",
                DeceasedNextOfKinPhoneNumber = getDeath.DeceasedNextOfKinPhoneNumber ?? "",
                DeceasedNextOfKinName = getDeath.DeceasedNextOfKinName ?? "",
                DateCreated = getDeath.DateCreated.ToString("dd/MM/yyyy"),
                DeceasedPhotoPath = getDeath.DeceasedPhotoPath ?? "",
                IsRegionalAdmin = isRegionalAdmin,
                IsGeneralAdmin = isGeneralAdmin,
                IsAccountHolder = isAccountHolder, // Add this to the view model
                Successor = successor // Add the successor details to the view model
            };

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Reported Death", "Navigated to Report Death page", ct);

            viewModel.DepsList = await _db.Dependants.Where(a => a.UserId == getDeath.UserId).ToListAsync();
            viewModel.Deps = await _db.Dependants.Where(a => a.UserId == getDeath.UserId).FirstOrDefaultAsync();
            viewModel.isDependant = viewModel.DepsList?.Count() > 1;

            return View(viewModel);
        }

        // Helper method to strip HTML tags
        private string StripHtmlTags(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return Regex.Replace(input, "<.*?>", string.Empty);
        }

        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> EditDeath(int id, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

            var getDeath = await _db.ReportedDeath.FirstOrDefaultAsync(a => a.Id == id);
            if (getDeath == null)
            {
                return NotFound();
            }

            // Strip HTML tags from OtherRelevantInformation
            var plainTextOtherRelevantInformation = StripHtmlTags(getDeath.OtherRelevantInformation);

            var viewModel = new ReportedDeathDetailsViewModel
            {
                Id = id,
                UserId = getDeath.UserId,
                DependentId = getDeath.DependentId,
                PersonName = getDeath.DeceasedName,
                RegisterNumber = getDeath.DeceasedRegNumber,
                PlaceOfBurial = getDeath.PlaceOfBurial,
                OtherRelevantInformation = plainTextOtherRelevantInformation,
                YearOfBirth = getDeath.DeceasedYearOfBirth,
                DateJoined = getDeath.DateJoined.ToString("dd/MM/yyyy"),
                DeathLocation = getDeath.DeathLocation,
                DateOfDeath = getDeath.DateOfDeath.ToString("dd/MM/yyyy"),
                ReportedBy = getDeath.ReportedBy,
                RelationShipToDeceased = getDeath.RelationShipToDeceased ?? "",
                CreatedBy = getDeath.CreatedBy,
                ReportedOn = getDeath.ReportedOn.ToString("dd/MM/yyyy"),
                ReporterContactNumber = getDeath.ReporterContactNumber ?? "",
                DeceasedNextOfKinEmail = getDeath.DeceasedNextOfKinEmail ?? "",
                DeceasedNextOfKinRelationship = getDeath.DeceasedNextOfKinRelationship ?? "",
                DeceasedNextOfKinPhoneNumber = getDeath.DeceasedNextOfKinPhoneNumber ?? "",
                DeceasedNextOfKinName = getDeath.DeceasedNextOfKinName ?? "",
                DateCreated = getDeath.DateCreated.ToString("dd/MM/yyyy"),
                DeceasedPhotoPath = getDeath.DeceasedPhotoPath ?? "" // Set the photo path
            };

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Reported Death", "Navigated to Edit Death page", ct);

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> EditDeath(int id, ReportedDeathDetailsViewModel model, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);
            ModelState.Remove(nameof(model.Deps));
            ModelState.Remove(nameof(model.DepsList));
            ModelState.Remove(nameof(model.DateJoined));
            if (ModelState.IsValid)
            {
                var getDeath = await _db.ReportedDeath.FirstOrDefaultAsync(a => a.Id == id);
                if (getDeath == null)
                {
                    return NotFound();
                }

                // Handle file upload with validation
                string relativePhotoPath = getDeath.DeceasedPhotoPath;
                if (model.DeceasedPhoto != null && model.DeceasedPhoto.Length > 0)
                {
                    var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

                    var fileExtension = Path.GetExtension(model.DeceasedPhoto.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension) ||
                        !allowedMimeTypes.Contains(model.DeceasedPhoto.ContentType.ToLowerInvariant()))
                    {
                        ModelState.AddModelError("DeceasedPhoto", "Please upload a valid image file (JPG, JPEG, PNG).");
                        return View(model);
                    }

                    var uploads = Path.Combine(_hostEnvironment.WebRootPath, "DeceasedPhotos");
                    if (!Directory.Exists(uploads))
                    {
                        Directory.CreateDirectory(uploads);
                    }

                    // Generate a unique file name
                    var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                    var fullPhotoPath = Path.Combine(uploads, uniqueFileName);

                    using (var stream = new FileStream(fullPhotoPath, FileMode.Create))
                    {
                        await model.DeceasedPhoto.CopyToAsync(stream);
                    }

                    // Store only the relative path in the database
                    relativePhotoPath = Path.Combine("DeceasedPhotos", uniqueFileName);

                    // Delete old photo if a new one is uploaded
                    if (!string.IsNullOrEmpty(getDeath.DeceasedPhotoPath))
                    {
                        var oldPhotoPath = Path.Combine(_hostEnvironment.WebRootPath, getDeath.DeceasedPhotoPath);
                        if (System.IO.File.Exists(oldPhotoPath))
                        {
                            System.IO.File.Delete(oldPhotoPath);
                        }
                    }

                    // Log the change for the photo
                    LogChange(getDeath, nameof(getDeath.DeceasedPhotoPath), getDeath.DeceasedPhotoPath, relativePhotoPath, currentUser.Email);
                }

                // Log changes to other fields
                LogChange(getDeath, nameof(getDeath.PlaceOfBurial), getDeath.PlaceOfBurial, model.PlaceOfBurial ?? "", currentUser.Email);
                LogChange(getDeath, nameof(getDeath.OtherRelevantInformation), getDeath.OtherRelevantInformation, model.OtherRelevantInformation, currentUser.Email);
                LogChange(getDeath, nameof(getDeath.DeathLocation), getDeath.DeathLocation, model.DeathLocation, currentUser.Email);
                LogChange(getDeath, nameof(getDeath.DateOfDeath), getDeath.DateOfDeath.ToString("dd/MM/yyyy"), model.DateOfDeath, currentUser.Email);
                LogChange(getDeath, nameof(getDeath.ReportedBy), getDeath.ReportedBy, model.ReportedBy, currentUser.Email);
                LogChange(getDeath, nameof(getDeath.DeceasedNextOfKinRelationship), getDeath.DeceasedNextOfKinRelationship, model.RelationShipToDeceased, currentUser.Email);
                LogChange(getDeath, nameof(getDeath.ReporterContactNumber), getDeath.ReporterContactNumber, model.ReporterContactNumber, currentUser.Email);
                LogChange(getDeath, nameof(getDeath.DeceasedNextOfKinName), getDeath.DeceasedNextOfKinName, model.DeceasedNextOfKinName ?? "", currentUser.Email);
                LogChange(getDeath, nameof(getDeath.DeceasedNextOfKinEmail), getDeath.DeceasedNextOfKinEmail, model.DeceasedNextOfKinEmail ?? "", currentUser.Email);
                LogChange(getDeath, nameof(getDeath.DeceasedNextOfKinPhoneNumber), getDeath.DeceasedNextOfKinPhoneNumber, model.DeceasedNextOfKinPhoneNumber ?? "", currentUser.Email);

                // Update the reported death record
                getDeath.PlaceOfBurial = model.PlaceOfBurial ?? "";
                getDeath.DeathLocation = model.DeathLocation;
                getDeath.OtherRelevantInformation = model.OtherRelevantInformation;                

                getDeath.DateOfDeath = DateTime.Parse(model.DateOfDeath);
                getDeath.ReportedBy = model.ReportedBy;
                getDeath.DeceasedNextOfKinRelationship = model.RelationShipToDeceased;
                getDeath.ReporterContactNumber = model.ReporterContactNumber;
                getDeath.DeceasedNextOfKinName = model.DeceasedNextOfKinName ?? "";
                getDeath.DeceasedNextOfKinEmail = model.DeceasedNextOfKinEmail ?? "";
                getDeath.DeceasedNextOfKinPhoneNumber = model.DeceasedNextOfKinPhoneNumber ?? "";
                getDeath.DeceasedPhotoPath = relativePhotoPath; // Save relative path
                getDeath.UpdatedOn = DateTime.UtcNow;

                _db.ReportedDeath.Update(getDeath);
                await _db.SaveChangesAsync(ct);

                TempData["Success"] = "Death details updated successfully.";
                return RedirectToAction("ReportedDeaths", "Family");
            }

            return View(model);
        }
        private void LogChange(ReportedDeath entity, string fieldName, string oldValue, string newValue, string changedBy)
        {
            if (oldValue != newValue)
            {
                var changeLog = new ReportedDeathChangeLog
                {
                    UserId = entity.UserId,
                    ReportedDeathId = entity.Id,
                    FieldName = fieldName,
                    OldValue = oldValue,
                    NewValue = newValue,
                    ChangeDate = DateTime.UtcNow,
                    ChangedBy = changedBy
                };
                _db.ReportedDeathChangeLog.Add(changeLog);
            }
        }


        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> ManageDependents(CancellationToken ct)
        {
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // First, fetch all dependents without accounts
            var allDependentsWithoutAccounts = await _db.Dependants
                .Where(d => !_db.Users.Any(u => u.PersonRegNumber == d.PersonRegNumber && u.IsActive == true))
                .ToListAsync(ct);

            // Then, filter out those who are either reported dead or confirmed dead
            var dependentsWithoutAccounts = allDependentsWithoutAccounts
                .Where(d => !d.IsReportedDead)
                .Select(d => new ManageDependentsViewModel
                {
                    DependentId = d.Id,
                    PersonName = d.PersonName,
                    Email = d.Email,
                    PhoneNumber = d.Telephone,
                    YearOfBirth = d.PersonYearOfBirth,
                    RegNumber = d.PersonRegNumber,
                    HasAccount = false
                })
                .ToList();

            return View(dependentsWithoutAccounts);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> SeparateDependent(RequestAccountDetailsViewModel model, CancellationToken ct)
        {
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var dependent = await _db.Dependants.FirstOrDefaultAsync(d => d.Id == model.DependentId, ct);

            if (dependent == null)
            {
                TempData[SD.Error] = "Dependent not found.";
                return RedirectToAction(nameof(ManageDependents));
            }

            var originalUser = await _db.Users.FirstOrDefaultAsync(u => u.UserId == dependent.UserId, ct);

            if (originalUser == null)
            {
                TempData[SD.Error] = "Original account holder not found.";
                return RedirectToAction(nameof(ManageDependents));
            }

            // Check if a user with the given email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                TempData[SD.Error] = "An account with this email already exists. Please choose a different email.";
                return RedirectToAction(nameof(ManageDependents));
            }

            // Create a new user account for the dependent
            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                UserName = model.Email,
                Email = model.Email,
                FirstName = dependent.PersonName.Split(' ')[0],
                Surname = dependent.PersonName.Split(' ').Last(),
                PhoneNumber = dependent.Telephone,
                CityId = originalUser.CityId,
                RegionId = originalUser.RegionId,
                Title = dependent.Title ?? 1,
                ApplicationStatus = Status.Approved,
                IsConsent = true,
                UpdateOn = DateTime.UtcNow,
                OutwardPostcode = originalUser.OutwardPostcode,
                DependentId = dependent.Id,
                IsDeceased = false,
                ForcePasswordChange = true,
                PersonRegNumber = dependent.PersonRegNumber,
                PersonYearOfBirth = dependent.PersonYearOfBirth,
                DateCreated = dependent.DateCreated,
                CreatedBy = currentUser.Email,
                LastPasswordChangedDate = DateTime.UtcNow,
              
            };

            var result = await _userManager.CreateAsync(newUser, model.TemporaryPassword);
            if (!result.Succeeded)
            {
                TempData[SD.Error] = "Failed to create new account. " + string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(ManageDependents));
            }

            await _userManager.AddToRoleAsync(newUser, RoleList.Member);

            // Update the selected dependents to be associated with the new user
            foreach (var selectedDependent in model.Dependents.Where(d => d.IsSelected))
            {
                var dependentToUpdate = await _db.Dependants.FirstOrDefaultAsync(d => d.Id == selectedDependent.DependentId, ct);
                if (dependentToUpdate != null)
                {
                    dependentToUpdate.UserId = newUser.UserId;
                    _db.Dependants.Update(dependentToUpdate);
                }
            }

            // Log the separation in the SeparationHistory table
            var separationHistory = new SeparationHistory
            {
                OldUserId = originalUser.UserId,
                NewUserId = newUser.UserId,
                DependentId = dependent.Id,
                DependentName = dependent.PersonName,
                SeparationDate = DateTime.UtcNow,
                SeparatedBy = currentUser.Email,
                OldUserFirstName = originalUser.FirstName,
                OldUserSurname = originalUser.Surname,
                OldUserEmail = originalUser.Email,
                NewUserFirstName = newUser.FirstName,
                NewUserSurname = newUser.Surname,
                NewUserEmail = newUser.Email,
                NewNumberOfDependants = model.Dependents.Count(d => d.IsSelected),
                Notes = "Separated due to account creation for dependent."
            };

            _db.SeparationHistory.Add(separationHistory);

       
            // Send a confirmation email to the new user
            var userId = await _userManager.GetUserIdAsync(newUser);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
            code = HttpUtility.UrlEncode(code);

            // Invalidate previous tokens
            var previousTokens = await _db.EmailVerifications
                .Where(ev => ev.UserId == newUser.UserId && !ev.Used)
                .ToListAsync(ct);

            if (previousTokens.Any())
            {
                foreach (var token in previousTokens)
                {
                    token.Used = true;
                }
                _db.EmailVerifications.UpdateRange(previousTokens);

            }



            // Store new token with expiration time
            var expirationTime = DateTime.UtcNow.AddHours(24); // Set expiration time to 24 hours
            var newEmailVerification = new EmailVerification
            {
                UserId = newUser.UserId,
                Token = code,
                ExpirationTime = expirationTime,
                DateCreated = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
                CreatedBy = currentUser.Email,
                Used = false
            };
            _db.EmailVerifications.Add(newEmailVerification);

            const string pathToFile = @"EmailTemplate/Registration.html";
            const string subject = "Confirm Your Account";

            var callbackUrl = Url.Action("AccountVerified", "Account", new { userId, code }, protocol: Request.Scheme);

            string htmlBody;
            using (StreamReader reader = System.IO.File.OpenText(pathToFile))
            {
                htmlBody = await reader.ReadToEndAsync(ct);
                htmlBody = htmlBody.Replace("{{userName}}", newUser.FirstName + " " + newUser.Surname)
                                   .Replace("{{currentYear}}", DateTime.UtcNow.Year.ToString())
                                   .Replace("{{UmojaWetu Portal Verification}}", $"<a href=\"{callbackUrl}\">UmojaWetu Portal Verification</a>");
            }

            var message = new PostmarkMessage
            {
                To = newUser.Email,
                Subject = subject,
                HtmlBody = htmlBody,
                From = "info@umojawetu.com"
            };

            var emailSent = await _postmark.SendMessageAsync(message, ct).ConfigureAwait(false);

            TempData[SD.Success] = "Dependent separated, account created successfully, and verification email sent.";
            return RedirectToAction(nameof(ManageDependents));
        }

        //[HttpGet]
        //[Authorize(Roles = RoleList.GeneralAdmin)]
        //public async Task<IActionResult> EditDependent(int id, CancellationToken ct)
        //{
        //    var currentUserEmail = HttpContext.Session.GetString("loginEmail");
        //    var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

        //    if (currentUser == null)
        //    {
        //        return RedirectToAction("Login", "Account");
        //    }

        //    var dependent = await _db.Dependants.FirstOrDefaultAsync(d => d.Id == id, ct);

        //    if (dependent == null)
        //    {
        //        TempData[SD.Error] = "Dependent not found.";
        //        return RedirectToAction(nameof(ManageDependents));
        //    }

        //    // Fetch related family members
        //    var relatedFamilyMembers = await _db.Dependants
        //        .Where(d => d.UserId == dependent.UserId && d.Id != dependent.Id)
        //        .Select(d => new ManageDependentsViewModel
        //        {
        //            DependentId = d.Id,
        //            PersonName = d.PersonName,
        //            YearOfBirth = d.PersonYearOfBirth,
        //            RegNumber = d.PersonRegNumber
        //        })
        //        .ToListAsync(ct);

        //    var model = new EditDependentViewModel
        //    {
        //        Dependent = new ManageDependentsViewModel
        //        {
        //            DependentId = dependent.Id,
        //            PersonName = dependent.PersonName,
        //            Email = dependent.Email,
        //            PhoneNumber = dependent.Telephone,
        //            YearOfBirth = dependent.PersonYearOfBirth,
        //            RegNumber = dependent.PersonRegNumber,
        //            HasAccount = _db.Users.Any(u => u.DependentId == dependent.Id)
        //        },
        //        RelatedFamilyMembers = relatedFamilyMembers
        //    };

        //    return View(model);
        //}


        public async Task<IActionResult> EditDependent(int id, CancellationToken ct)
        {
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var dependent = await _db.Dependants.FirstOrDefaultAsync(d => d.Id == id, ct);

                if (dependent == null)
                {
                    TempData[SD.Error] = "Dependent not found.";
                    return RedirectToAction(nameof(ManageDependents));
                }

                // Fetch related family members
                var relatedFamilyMembers = await _db.Dependants
                    .Where(d => d.UserId == dependent.UserId && d.Id != dependent.Id)
                    .Select(d => new ManageDependentsViewModel
                    {
                        DependentId = d.Id,
                        PersonName = d.PersonName,
                        YearOfBirth = d.PersonYearOfBirth,
                        RegNumber = d.PersonRegNumber
                    })
                    .ToListAsync(ct);

                var model = new EditDependentViewModel
                {
                    Dependent = new ManageDependentsViewModel
                    {
                        DependentId = dependent.Id,
                        PersonName = dependent.PersonName,
                        Email = dependent.Email,
                        PhoneNumber = dependent.Telephone,
                        YearOfBirth = dependent.PersonYearOfBirth,
                        RegNumber = dependent.PersonRegNumber,
                        HasAccount = await _db.Users.AnyAsync(u => u.PersonRegNumber == dependent.PersonRegNumber && u.IsActive == true, ct)
                    },
                    RelatedFamilyMembers = relatedFamilyMembers
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "An error occurred while processing your request.";
                // Log the exception details here if needed
                return RedirectToAction(nameof(ManageDependents));
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> EditDependent(EditDependentViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                // Return the same view with the model to show validation errors
                return View(model);
            }

            var currentUserEmail = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var dependent = await _db.Dependants.FirstOrDefaultAsync(d => d.Id == model.Dependent.DependentId, ct);

            if (dependent == null)
            {
                TempData[SD.Error] = "Dependent not found.";
                return RedirectToAction(nameof(ManageDependents));
            }

            // Update dependent details
            dependent.PersonName = model.Dependent.PersonName;
            dependent.Email = model.Dependent.Email;
            dependent.Telephone = model.Dependent.PhoneNumber;
            dependent.PersonYearOfBirth = model.Dependent.YearOfBirth;
            dependent.PersonRegNumber = model.Dependent.RegNumber;

            _db.Dependants.Update(dependent);
            await _db.SaveChangesAsync(ct);

            TempData[SD.Success] = "Dependent details updated successfully.";
            return RedirectToAction(nameof(ManageDependents));
        }


        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDependent(string personRegNumber, CancellationToken ct)
        {
            try
            {
                var dependent =  _db.Dependants.FirstOrDefault(a => a.PersonRegNumber == personRegNumber);
                if (dependent == null)
                {
                    TempData[SD.Error] = "Dependent not found.";
                    return RedirectToAction(nameof(ManageDependents));
                }

             
              

                // Delete associated NextOfKin records
                var nextOfKins = await _db.NextOfKins.Where(n => n.PersonRegNumber == personRegNumber).ToListAsync(ct);
                if (nextOfKins.Any())
                {
                    _db.NextOfKins.RemoveRange(nextOfKins);
                }

                // Delete the dependent
                _db.Dependants.Remove(dependent);
                await _db.SaveChangesAsync(ct);

                TempData[SD.Success] = "Dependent and associated Next of Kin deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = $"Error deleting dependent: {ex.Message}";
            }

            return RedirectToAction(nameof(ManageDependents));
        }



        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> RequestAccountDetails(string personRegNumber, CancellationToken ct)
        {
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var dependent = await _db.Dependants.FirstOrDefaultAsync(d => d.PersonRegNumber == personRegNumber, ct);

            if (dependent == null)
            {
                TempData[SD.Error] = "Dependent not found.";
                return RedirectToAction(nameof(ManageDependents));
            }

            // Fetch the current account holder's name
            var accountHolder = await _db.Users.FirstOrDefaultAsync(u => u.UserId == dependent.UserId, ct);
            var accountHolderName = accountHolder != null ? $"{accountHolder.FirstName} {accountHolder.Surname}" : "Unknown";

            // Fetch all dependents under the same UserId, excluding the main account holder
            var dependentsUnderSameUser = await _db.Dependants
                .Where(d => d.UserId == dependent.UserId && !_db.Users.Any(u => u.PersonRegNumber == d.PersonRegNumber) && d.IsActive == true)
                .ToListAsync(ct);

            var model = new RequestAccountDetailsViewModel
            {
                DependentId = dependent.Id,
                DependentName = dependent.PersonName,
                Email = dependent.Email?? "",
                UserId = dependent.UserId,
                CurrentAccountHolderName = accountHolderName, 
                Dependents = dependentsUnderSameUser.Select(d => new DependentsViewModel
                {
                    DependentId = d.Id,
                    PersonName = d.PersonName,
                    IsSelected = false
                }).ToList()
            };

            return View(model);
        }


    }
}