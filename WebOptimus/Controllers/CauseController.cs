using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RotativaCore;
using System.Globalization;
using System.Text.Json;
using System.Text;
using WebOptimus.Data;
using WebOptimus.Helpers;
using WebOptimus.Models;
using WebOptimus.Models.ViewModel;
using WebOptimus.Services;
using WebOptimus.StaticVariables;


namespace WebOptimus.Controllers
{
   
    public class CauseController : BaseController
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
        private readonly IConfiguration _configuration;
        public CauseController(IMapper mapper, UserManager<User> userManager,
           SignInManager<User> signInManager, IWebHostEnvironment hostEnvironment, IConfiguration configuration, RoleManager<IdentityRole> roleManager, IUserStore<User> userStore,
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
            _configuration = configuration;
            _hostEnvironment = hostEnvironment;
        }



        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin + "," + RoleList.LocalAdmin + "," + RoleList.RegionalAdmin)]
        public async Task<IActionResult> ApprovedDeaths(CancellationToken ct, string donationStatus = null)
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

            //  Ensure filtering only "Approved" deaths
            IQueryable<ReportedDeath> deathQuery = _db.ReportedDeath
                .Where(rd => rd.Status != null && rd.Status.Trim().ToLower() == "approved");

            if (currentUserRole == RoleList.LocalAdmin)
            {
                deathQuery = deathQuery.Where(rd => rd.CityId == currentUser.CityId);
            }
            else if (currentUserRole == RoleList.RegionalAdmin)
            {
                deathQuery = deathQuery.Where(rd => rd.RegionId == currentUser.RegionId);
            }

            //  Apply donation status filter ONLY IF IT'S NOT EMPTY
            if (!string.IsNullOrEmpty(donationStatus))
            {
                deathQuery = deathQuery.Where(rd => rd.DonationStatus == donationStatus);
            }

            var deathRelatedCauses = await deathQuery
                .Join(_db.City, rd => rd.CityId, c => c.Id, (rd, c) => new { rd, CityName = c.Name })
                .Join(_db.Region, temp => temp.rd.RegionId, r => r.Id, (temp, r) => new { temp.rd, temp.CityName, RegionName = r.Name })
                .Select(x => new ReportedDeathViewModel
                {
                    Id = x.rd.Id,
                    DependentName = x.rd.DeceasedName,
                    DateOfDeath = x.rd.DateOfDeath,
                    DeathLocation = x.rd.DeathLocation,
                    PlaceOfBurial = x.rd.PlaceOfBurial,
                    ReporterContactNumber = x.rd.ReporterContactNumber,
                    ReportedBy = x.rd.ReportedBy,
                    ReportedOn = x.rd.ReportedOn,
                    CityName = x.CityName,
                    RegionName = x.RegionName,
                    DonationStatus = x.rd.DonationStatus,
                    Status = x.rd.Status
                })
                .ToListAsync(ct);

            //  Ensure only "Approved" general causes
            var generalCauses = await _db.Cause
                .Where(c => c.DeathId == null)
                .Select(c => new CauseViewModel
                {
                    Id = c.Id,
                    Summary = c.Summary,
                    Description = c.Description,
                    TargetAmount = c.TargetAmount,
                    IsActive = c.IsActive,
                    CauseCampaignpRef = c.CauseCampaignpRef,
                    DateCreated = c.DateCreated,
                    CreatedBy = c.CreatedBy
                })
                .ToListAsync(ct);

            ViewBag.DeathRelatedCauses = deathRelatedCauses;
            ViewBag.GeneralCauses = generalCauses;

            return View();
        }

      
        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> PaymentDashboard(string causeFilter, CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                if (email == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                var currentUser = await _userManager.FindByEmailAsync(email);

                // Fetch PaymentSessions
                var paymentSessions = await _db.PaymentSessions
                    .Where(ps => ps.IsPaid == true &&
                                 (string.IsNullOrEmpty(causeFilter) || ps.CauseCampaignpRef == causeFilter))
                    .ToListAsync(ct);

                // Fetch Payments
                var payments = await _db.Payment
                    .Where(p => string.IsNullOrEmpty(causeFilter) || p.CauseCampaignpRef == causeFilter)
                    .ToListAsync(ct);

                // Fetch dependents
                var dependents = await _db.Dependants             
                .ToListAsync();
                // Fetch closed causes
                var closedCauses = await _db.Cause
                     .Where(c => c.IsClosed && (!c.IsActive == true || !c.IsActive == true) &&
                                 (string.IsNullOrEmpty(causeFilter) || c.CauseCampaignpRef == causeFilter))
                     .ToListAsync(ct);

                // Populate ViewBag.CauseList
                var causeList = await _db.Cause
                    .Select(ps => ps.CauseCampaignpRef)
                    .Distinct()
                    .OrderBy(c => c)
                    .Select(c => new { Value = c, Text = c })
                    .ToListAsync(ct);
                ViewBag.CauseList = new SelectList(causeList, "Value", "Text");

                // Calculate totals directly from PaymentSessions
                var totalAmount = paymentSessions.Sum(ps => ps.TotalAmount);
                var totalTransactionFees = paymentSessions.Sum(ps => ps.TransactionFees);
                var netTotalAmount = totalAmount - totalTransactionFees;
                var displayedSessions = new HashSet<string>();
                // Match payments with their respective sessions
                var paymentDetails = payments.Select(p =>
                {
                    bool isFirstForSession = displayedSessions.Add(p.OurRef); // Adds & checks if already exists

                    var session = paymentSessions.FirstOrDefault(ps => ps.OurRef == p.OurRef);

                    return new PaymentDetailViewModel
                    {
                        Id = p.Id,
                        CauseCampaignpRef = p.CauseCampaignpRef,
                        Amount = p.Amount,
                        RegNumber = p.personRegNumber,
                        TransactionFees = isFirstForSession ? (session?.TransactionFees ?? 0m) : 0m, // Show only once
                        TotalAmount = isFirstForSession ? (session?.TotalAmount ?? 0m) : 0m,
                        GoodwillAmount = p.GoodwillAmount,                       
                        DateCreated = p.DateCreated,
                        DependentName = dependents.FirstOrDefault(d => d.PersonRegNumber == p.personRegNumber)?.PersonName ?? "Unknown",
                        OurRef = p.OurRef
                    };
                }).ToList();

                // Calculate missed payments
                var missedPayments = new List<AdminMissedPaymentViewModel>();
                foreach (var cause in closedCauses)
                {
                    var unpaidDependents = dependents
                    .Where(d =>
                        !payments.Any(p => p.personRegNumber == d.PersonRegNumber && p.CauseCampaignpRef == cause.CauseCampaignpRef) &&
                        CalculateAgeAtYear(d.PersonYearOfBirth.ToString(), cause.DateCreated) >= 25 &&
                        d.DateCreated <= cause.IsClosedDate // Exclude dependents created after the cause's EndDate
                    )
                    .ToList();

                    foreach (var dependent in unpaidDependents)
                    {
                        missedPayments.Add(new AdminMissedPaymentViewModel
                        {
                            DependentId = dependent.Id,
                            DependentName = dependent.PersonName,
                            UserId = dependent.UserId,
                            PhoneNumber = dependent.Telephone,
                            YearOfBirth = dependent.PersonYearOfBirth,
                            RegNumber = dependent.PersonRegNumber,
                            CauseCampaignpRef = cause.CauseCampaignpRef,
                            Amount = cause.FullMemberAmount,
                            IsClosedDate = cause.IsClosedDate
                        });
                    }
                }

                var totalMissedPayments = missedPayments.Sum(mp => mp.Amount);

                // Populate AdminPaymentViewModel
                var adminDashboardViewModel = new AdminPaymentViewModel
                {
                    Payments = paymentDetails,
                    MissedPayments = missedPayments,
                    CauseFilter = causeFilter,
                    TotalAmount = netTotalAmount, // Subtract transaction fees from total
                    TotalTransactionFees = totalTransactionFees,
                    TotalOverPaid = paymentDetails.Sum(p => p.GoodwillAmount)
                };

                ViewBag.TotalMissedPayments = totalMissedPayments;

                return View(adminDashboardViewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }


        // Helper function to calculate age
        private int CalculateAge(string yearOfBirth)
        {
            if (int.TryParse(yearOfBirth, out var birthYear))
            {
                return DateTime.Now.Year - birthYear;
            }
            return 0;
        }
        private int CalculateAgeAtYear(string personYearOfBirth, DateTime startDate)
        {
            if (int.TryParse(personYearOfBirth, out var birthYear))
            {
                return startDate.Year - birthYear;
            }
            return 0; // Return 0 if parsing fails
        }



        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> AddCause(int id, CancellationToken ct)
        {
            var model = new CauseViewModel();

            // Fetch the reported death
            var reportedDeath = await _db.ReportedDeath
                .Where(a=>a.Id == id && a.Status == "Approved")
                .FirstOrDefaultAsync(ct);

            if (reportedDeath == null)
            {
                TempData[SD.Error] = "Reported death not found or approved.";
                return RedirectToAction("ApprovedDeaths", "Cause");
            }

            model.Summary = "Remembering " + reportedDeath.DeceasedName + ": A Fund to Support Their Family";
            model.Description = reportedDeath.OtherRelevantInformation;

            model.ReportedDeathId = reportedDeath.Id;

            // Ensure StartDate and EndDate are correctly set from the Cause entity
            model.StartDate = DateTime.Now;  // Set default value if null
            model.EndDate =  DateTime.Now.AddDays(7);  // Set default to one month later if null
            model.IsActive = true;
            model.MissPaymentAmount = 10;
            model.DependentName = reportedDeath.DeceasedName;
           
          
                model.RegistrationNumber = reportedDeath.PersonRegNumber;
                model.YearOfBirth = reportedDeath.DeceasedYearOfBirth;
                model.DateJoinedAsString = reportedDeath.DateJoined.ToString("dd/MM/yyyy");  
            model.DateOfDeathAsString = reportedDeath.DateOfDeath.ToString("dd/MM/yyyy");

            // Calculate the age of all dependents and categorize them
            var allDependents = await _db.Dependants
               .Where(a => a.IsActive == true || a.IsActive == null)
               .ToListAsync();



            var under18Dependents = allDependents.Count(d => CalculateAge(d.PersonYearOfBirth) < 25);
            var over18Dependents = allDependents.Count(d => CalculateAge(d.PersonYearOfBirth) >= 25);
            // Set the counts
            model.over18DependentsCount = over18Dependents;
            model.under18DependentsCount = under18Dependents;
            model.totalmembers =  allDependents.Count();
            decimal targetAmount = 10000; // Set the initial target amount to £10,000
            model.TargetAmount = targetAmount;

            decimal fullMemberTotal = targetAmount;
            decimal underAgeTotal = 0;

            // Distribute the amounts evenly and format to 2 decimal places
            model.FullMemberAmount = over18Dependents > 0
                ? Math.Round(fullMemberTotal / over18Dependents, 2)
                : 0;

            model.UnderAgeAmount = under18Dependents > 0
                ? Math.Round(underAgeTotal / under18Dependents, 2)
                : 0;

            model.UnderAge = 25; // Set the UnderAge limit to 25

            return View(model);
        }


        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> GetReportedDeathDetails(int id, CancellationToken ct)
        {
            var reportedDeath = await _db.ReportedDeath
                .Where(rd => rd.Id == id)
                .Select(rd => new
                {
                    rd.DeceasedRegNumber,
                    rd.DeceasedYearOfBirth,
                    rd.DateJoined,
                    rd.DateOfDeath
                })
                .FirstOrDefaultAsync(ct);

            if (reportedDeath == null)
            {
                return NotFound();
            }

            return Json(reportedDeath);
        }

        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> AddCause(CauseViewModel model, CancellationToken ct)
        {
            Cause cause;
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                if (email == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                var currentUser = await _userManager.FindByEmailAsync(email);

                // Remove validation for fields not relevant for this context
                ModelState.Remove(nameof(model.ReportedDeaths));
                ModelState.Remove(nameof(model.DateJoinedAsString));
                ModelState.Remove(nameof(model.YearOfBirth));
                ModelState.Remove(nameof(model.DateOfDeathAsString));
                ModelState.Remove(nameof(model.DependentName));             
                ModelState.Remove(nameof(model.DateJoined));
                ModelState.Remove(nameof(model.DateOfDeath));
                ModelState.Remove(nameof(model.RegistrationNumber));
                ModelState.Remove(nameof(model.PersonRegNumber));
                ModelState.Remove(nameof(model.PersonYearOfBirth));
                ModelState.Remove(nameof(model.StartDate));
                ModelState.Remove(nameof(model.EndDate));
                ModelState.Remove(nameof(model.MissPaymentAmount));
                ModelState.Remove(nameof(model.IsActive));
                if (!ModelState.IsValid)
                {
                    // Fetch the list of reported deaths for dropdown if relevant
                    var reportedDeaths = await _db.ReportedDeath
                        .Select(rd => new SelectListItem
                        {
                            Value = rd.Id.ToString(),
                            Text = rd.DeceasedName
                        })
                        .ToListAsync(ct);

                    model.ReportedDeaths = reportedDeaths;
                    return View(model);
                }

              

                    // Death-related cause logic
                    var dec = await _db.ReportedDeath.FirstOrDefaultAsync(a => a.Id == model.ReportedDeathId);

                    if (dec == null)
                    {
                        TempData[SD.Error] = "Reported death details not found.";
                        return RedirectToAction("ApprovedDeaths", "Cause");
                    }

                    cause = new Cause
                    {
                        Summary = model.Summary,
                        Description = model.Description,
                        UserId = dec.UserId,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate,
                        MissPaymentAmount = model.MissPaymentAmount,
                        DependentId = dec.DependentId,
                        PersonRegNumber = currentUser.PersonRegNumber,
                        CauseCampaignpRef = "D" + dec.DeceasedRegNumber + "_" + dec.DateOfDeath.ToString("ddMMyyyy"),
                        TargetAmount = model.TargetAmount,
                        FullMemberAmount = model.FullMemberAmount,
                        UnderAge = model.UnderAge,
                        UnderAgeAmount = model.UnderAgeAmount,
                        IsActive =true,  
                        DeathId = model.ReportedDeathId,
                        DateCreated = DateTime.UtcNow,
                        IsClosed = false,                     
                        CreatedBy = User.Identity.Name
                    };
                var updateStatus = await _db.ReportedDeath.Where(a => a.Id == model.ReportedDeathId).FirstOrDefaultAsync();

               
                if (model.IsActive == true)
                {

                    if (dec != null)
                    {
                        dec.DonationStatus = DonationStatus.Live;
                        _db.ReportedDeath.Update(dec);
                    }
                }
                else
                {
                    if (dec != null)
                    {
                        dec.DonationStatus = DonationStatus.NotLive;
                        _db.ReportedDeath.Update(dec);
                    }

                }

                _db.Cause.Add(cause);
                await _db.SaveChangesAsync(ct);

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "AddCause",
                    $"User created a new campaign: {model.Summary}", ct);

                //send email
                await NotifyMembers(cause, ct);
                TempData[SD.Success] = "Cause created successfully.";
                return RedirectToAction("ApprovedDeaths", "Cause");
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "Falied to process because: "+ ex.Message;
                return RedirectToAction("ApprovedDeaths", "Cause");
            }
        }


        public async Task<IActionResult> SendPaymentReminder(Guid UserId, CancellationToken ct)
        {
            try
            {
                // Fetch the user by userId
                var user = await _db.Users.FirstOrDefaultAsync(a => a.UserId == UserId);
                if (user == null)
                {
                    TempData[SD.Error] = "User not found.";
                
                }
                             

                // Load email template and replace placeholders
                const string pathToFile = @"EmailTemplate/PaymentReminder.html";
                string htmlBody = await System.IO.File.ReadAllTextAsync(pathToFile, ct);
                htmlBody = htmlBody.Replace("{{user}}", user.FirstName + " " + user.Surname)
                                   .Replace("{{currentYear}}", DateTime.UtcNow.Year.ToString());


                // Create the email message
                var message = new PostmarkMessage
                {
                    To = user.Email,
                    Subject = "Payment Reminder: Action Required!",
                    HtmlBody = htmlBody,
                    From = "info@umojawetu.com"
                };

                // Send the email

                var emailSent = await _postmark.SendMessageAsync(message, ct).ConfigureAwait(false);
                await RecordAuditAsync(user, _requestIpHelper.GetRequestIp(), "Send Payment Reminder", "Send Payment Remindert sent to " + user.Email+ ct);

                TempData[SD.Success] = "Reminder email sent successfully.";
                return RedirectToAction(nameof(PaymentDashboard));
                //return Json(new { success = true, message = "Reminder email sent successfully." });
            }
            catch (Exception ex)
            {
                TempData[SD.Success] = "Error sending payment reminder.";
                // Log the error appropriately
                Console.WriteLine($"Error sending payment reminder: {ex.Message}");
                return RedirectToAction(nameof(PaymentDashboard));
            }
        }

      
        public async Task<IActionResult> EndCause (int id, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

            var updateStatus = await _db.ReportedDeath.Where(a => a.Id == id).FirstOrDefaultAsync();

            if (updateStatus != null)
            {

                updateStatus.DonationStatus = DonationStatus.DonationEnded;
                _db.ReportedDeath.Update(updateStatus);
                await _db.SaveChangesAsync();
            }

            //
            var cas = await _db.Cause.FirstOrDefaultAsync(a => a.DeathId == id);


            cas.IsActive = false;
            cas.IsClosed = true;
            cas.IsDisplayable = false;
            cas.IsClosedDate = DateTime.UtcNow;
            _db.Cause.Update(cas);
            await _db.SaveChangesAsync();
            TempData[SD.Success] = "Donation Closed!";
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Donation Ended", "User Ended donation for CauseCampaignRefID: " + cas.CauseCampaignpRef, ct);


            return RedirectToAction(nameof(ApprovedDeaths));
        }

        public async Task<IActionResult> Redo(int id, CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                if (email == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                var currentUser = await _userManager.FindByEmailAsync(email);

                var updateStatus = await _db.ReportedDeath.Where(a => a.Id == id).FirstOrDefaultAsync();

                if (updateStatus != null)
                {

                    updateStatus.DonationStatus = DonationStatus.Live;
                    _db.ReportedDeath.Update(updateStatus);
                    await _db.SaveChangesAsync();
                }

                //
                var cas = await _db.Cause.Where(a => a.DeathId == id).FirstOrDefaultAsync();

                if(cas != null)
                {
                    cas.IsActive = true;
                    cas.IsClosed = false;
                    cas.IsDisplayable = true;                
                    _db.Cause.Update(cas);
                    await _db.SaveChangesAsync();

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Donation ReOpened", "User Reopened donation for CauseCampaignRefID: " + cas.CauseCampaignpRef, ct);
                    TempData[SD.Success] = "Donation Re-opened successfully.";
                }

                else
                {
                    TempData[SD.Error] = "Error Re-opening donation please check if the Cause exist and try again.";
                }

                return RedirectToAction(nameof(ApprovedDeaths));
            }catch (Exception ex)
            {
                TempData[SD.Error] = "Error: " + ex.Message.ToString();
                return View();
            }
        }


        //[HttpGet]
        //[Authorize(Roles = RoleList.GeneralAdmin)]
        //public async Task<IActionResult> EditCause(int id, CancellationToken ct)
        //{
        //    var email = HttpContext.Session.GetString("loginEmail");
        //    if (email == null)
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }
        //    var currentUser = await _userManager.FindByEmailAsync(email);

        //    var model = new CauseViewModel();
        //    var user = await _db.Users.Where(a => a.Email != "seakou2@yahoo.com" || a.IsDeleted == false || a.IsDeleted == null).FirstOrDefaultAsync();
        //    var allDependents = await _db.Dependants
        //        .Where(a => a.Id != user.DependentId || a.IsReportedDead != true || a.IsDeleted == false || a.IsDeleted == null)
        //        .ToListAsync();
        //    // Fetch the reported death
        //    var reportedDeath = await _db.ReportedDeath
        //        .Where(rd => rd.Status == "Approved" && rd.Id == id)
        //        .FirstOrDefaultAsync(ct);

        //    model.Summary = "Remembering " + reportedDeath.DeceasedName + ": A Fund to Support Their Family";

        //    var over18Dependents = allDependents.Where(d => CalculateAge(d.PersonYearOfBirth) > 25).ToList();
        //    var under18Dependents = allDependents.Where(d => CalculateAge(d.PersonYearOfBirth) <= 25).ToList();

        //    // Set the counts
        //    model.over18DependentsCount = over18Dependents.Count;
        //    model.under18DependentsCount = under18Dependents.Count;
        //    //gET CAUSE
        //    var cas = await _db.Cause.FirstOrDefaultAsync(a => a.DeathId == reportedDeath.Id);
        //    if (cas != null)
        //    {
        //        model.Description = cas.Description;
        //        model.TargetAmount = cas.TargetAmount;
        //        model.FullMemberAmount = cas.FullMemberAmount;
        //        model.UnderAge = cas.UnderAge;
        //        model.UnderAgeAmount = cas.UnderAgeAmount;

        //        decimal targetAmount = cas.TargetAmount > 0 ? cas.TargetAmount : 10000; // Default target amount if not set
        //        decimal fullMemberTotal = targetAmount;
        //        decimal underAgeTotal = 0;


        //        model.FullMemberAmount = over18Dependents.Count > 0
        //            ? Math.Round(fullMemberTotal / over18Dependents.Count, 2)
        //            : 0;

        //        model.UnderAgeAmount = under18Dependents.Count > 0
        //            ? Math.Round(underAgeTotal / under18Dependents.Count, 2)
        //            : 0;
        //    }

        //    else
        //    {
        //        model.Description = reportedDeath.OtherRelevantInformation;

        //        // Handle the case where the cause does not exist
        //        model.Description = reportedDeath.OtherRelevantInformation;
        //        model.TargetAmount = 10000; // Default value
        //        model.FullMemberAmount = 0; // Default value
        //        model.UnderAgeAmount = 0;  // Default value
        //        model.UnderAge = 25;       // Default age limit             


        //        decimal fullMemberTotal = model.TargetAmount;
        //        decimal underAgeTotal = 0;
        //        // Distribute the amounts evenly and format to 2 decimal places
        //        model.FullMemberAmount = over18Dependents.Count() > 0
        //            ? Math.Round(fullMemberTotal / over18Dependents.Count(), 2)
        //            : 0;

        //        model.UnderAgeAmount = under18Dependents.Count() > 0
        //            ? Math.Round(underAgeTotal / under18Dependents.Count(), 2)
        //            : 0;

        //        model.UnderAge = 25; // Set the UnderAge limit to 25
        //    }


        //    if (reportedDeath != null)
        //    {
        //        var regNum = await _db.Dependants.Where(a => a.Id == reportedDeath.DependentId).FirstOrDefaultAsync();

        //        model.ReportedDeathId = reportedDeath.Id;
        //        model.DependentName = reportedDeath.DeceasedName;
        //        model.Description = cas.Description;
        //        model.RegistrationNumber = regNum.PersonRegNumber;
        //        model.YearOfBirth = regNum.PersonYearOfBirth;
        //        model.DateJoinedAsString = regNum.DateCreated.ToString("dd/MM/yyyy");
        //        model.DateOfDeathAsString = reportedDeath.DateOfDeath.ToString("dd/MM/yyyy");
        //    }

        //    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit cause", "Edit cause details for: " + reportedDeath.DeceasedName, ct);


        //    return View(model);
        //}

        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> EditCause(int id, CancellationToken ct)
        {
            var model = new CauseViewModel();

            // Fetch the reported death
            var reportedDeath = await _db.ReportedDeath
                .Where(rd => rd.Status == "Approved" && rd.Id == id)
                .FirstOrDefaultAsync(ct);

            if (reportedDeath == null)
            {
                TempData[SD.Error] = "Reported death not found.";
                return RedirectToAction("ApprovedDeaths", "Cause");
            }

            // Fetch the cause related to the reported death
            var caus = await _db.Cause.FirstOrDefaultAsync(a => a.DeathId == reportedDeath.Id, ct);

            if (caus == null)
            {
                TempData[SD.Error] = "Cause not found for the specified report.";
                return RedirectToAction("ApprovedDeaths", "Cause");
            }

            model.Summary = "Remembering " + reportedDeath.DeceasedName + ": A Fund to Support Their Family";
            model.Description = reportedDeath.OtherRelevantInformation;

            model.ReportedDeathId = reportedDeath.Id;

            // Ensure StartDate and EndDate are correctly set from the Cause entity
            model.StartDate = caus.StartDate ?? DateTime.Now;  // Set default value if null
            model.EndDate = caus.EndDate ?? DateTime.Now.AddMonths(1);  // Set default to one month later if null
            model.IsActive = caus.IsActive;
            model.IsDisplayable = caus.IsDisplayable;         
            model.MissPaymentAmount = caus.MissPaymentAmount;
            model.DependentName = reportedDeath.DeceasedName;

            // Assuming regNum is being fetched correctly from ConfirmedDeath table
            var regNum = await _db.ConfirmedDeath.Where(a => a.DeathId == reportedDeath.Id).FirstOrDefaultAsync(ct);

            if (regNum != null)
            {
                model.RegistrationNumber = regNum.PersonRegNumber;
                model.YearOfBirth = regNum.PersonYearOfBirth;
                model.DateJoinedAsString = regNum.DateCreated.ToString("dd/MM/yyyy");
            }

            model.DateOfDeathAsString = reportedDeath.DateOfDeath.ToString("dd/MM/yyyy");

            // Calculate the age of all dependents and categorize them
            var allDependents = await _db.Dependants
                .Where(a => a.IsReportedDead != true)
                .ToListAsync(ct);

            var over18Dependents = allDependents.Where(d => CalculateAge(d.PersonYearOfBirth) > caus.UnderAge).ToList();
            var under18Dependents = allDependents.Where(d => CalculateAge(d.PersonYearOfBirth) <= caus.UnderAge).ToList();

            // Set the counts
            model.over18DependentsCount = over18Dependents.Count;
            model.under18DependentsCount = under18Dependents.Count;

            model.totalmembers = allDependents.Count();

            decimal targetAmount = caus.TargetAmount; // Set the initial target amount to £10,000
            model.TargetAmount = targetAmount;

            decimal fullMemberTotal = targetAmount;
            decimal underAgeTotal = 0;

            // Distribute the amounts evenly and format to 2 decimal places
            model.FullMemberAmount = caus.FullMemberAmount;

            model.UnderAgeAmount = model.UnderAgeAmount;

            model.UnderAge = caus.UnderAge; // Set the UnderAge limit to 25

            return View(model);
        }
        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> EditCause(CauseViewModel model, CancellationToken ct)
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
                return RedirectToAction("Index");
            }

            ModelState.Remove(nameof(model.ReportedDeaths));
            ModelState.Remove(nameof(model.DateJoinedAsString));
            ModelState.Remove(nameof(model.YearOfBirth));
            ModelState.Remove(nameof(model.DateOfDeathAsString));
            ModelState.Remove(nameof(model.DependentName));
            ModelState.Remove(nameof(model.DateJoined));
            ModelState.Remove(nameof(model.DateOfDeath));
            ModelState.Remove(nameof(model.RegistrationNumber));
            ModelState.Remove(nameof(model.PersonRegNumber));
            ModelState.Remove(nameof(model.PersonYearOfBirth));
            ModelState.Remove(nameof(model.StartDate));
            ModelState.Remove(nameof(model.EndDate));
            ModelState.Remove(nameof(model.MissPaymentAmount));
            ModelState.Remove(nameof(model.IsActive));

            if (!ModelState.IsValid)
            {
                // Fetch the list of reported deaths from the database
                var reportedDeaths = await _db.ReportedDeath
                    .Select(rd => new SelectListItem
                    {
                        Value = rd.Id.ToString(),
                        Text = rd.DeceasedName
                    })
                    .ToListAsync(ct);

                model.ReportedDeaths = reportedDeaths;
                return View(model);
            }

            var dec = await _db.ReportedDeath.FirstOrDefaultAsync(a => a.Id == model.ReportedDeathId);
            var cause = await _db.Cause.FirstOrDefaultAsync(a => a.DeathId == model.ReportedDeathId);

            if (cause == null)
            {
                TempData["Error"] = "Cause not found.";
                return RedirectToAction("ApprovedDeaths", "Cause");
            }

            // Initialize change log list
            List<ChangeLog> changeLogs = new List<ChangeLog>();

            // Log changes BEFORE modifying the entity
            void LogChange(string field, object? oldValue, object? newValue)
            {
                if (!Equals(oldValue, newValue))
                {
                    changeLogs.Add(new ChangeLog
                    {
                        UserId = currentUser.UserId,
                        DependentId = currentUser.DependentId,
                        FieldChanged = field,
                        OldValue = oldValue?.ToString(),
                        NewValue = newValue?.ToString(),
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = email
                    });
                }
            }

            // Log changes for each relevant field
            LogChange("Summary", cause.Summary, model.Summary);
            LogChange("StartDate", cause.StartDate?.ToString("yyyy-MM-dd"), model.StartDate?.ToString("yyyy-MM-dd"));
            LogChange("EndDate", cause.EndDate?.ToString("yyyy-MM-dd"), model.EndDate?.ToString("yyyy-MM-dd"));
            LogChange("IsActive", cause.IsActive, model.IsActive);
            LogChange("IsDisplayable", cause.IsDisplayable, model.IsDisplayable);
            LogChange("MissPaymentAmount", cause.MissPaymentAmount, model.MissPaymentAmount);
            LogChange("Description", cause.Description, model.Description);
            LogChange("TargetAmount", cause.TargetAmount, model.TargetAmount);
            LogChange("FullMemberAmount", cause.FullMemberAmount, model.FullMemberAmount);
            LogChange("UnderAge", cause.UnderAge, model.UnderAge);
            LogChange("UnderAgeAmount", cause.UnderAgeAmount, model.UnderAgeAmount);

            // Apply updates AFTER logging changes
            cause.Summary = model.Summary;
            cause.StartDate = model.StartDate ?? cause.StartDate;
            cause.EndDate = model.EndDate ?? cause.EndDate;
            cause.IsActive = model.IsActive;
            cause.IsDisplayable = model.IsDisplayable;
            cause.MissPaymentAmount = model.MissPaymentAmount;
            cause.Description = model.Description;
            cause.TargetAmount = model.TargetAmount;
            cause.FullMemberAmount = model.FullMemberAmount;
            cause.UnderAge = model.UnderAge;
            cause.UnderAgeAmount = model.UnderAgeAmount;
            cause.UpdatedOn = DateTime.UtcNow;

            // Handle donation status update
            if (model.IsActive == true)
            {
                if (dec != null)
                {
                    dec.DonationStatus = DonationStatus.Live;
                    _db.ReportedDeath.Update(dec);
                }
            }
            else
            {
                if (dec != null)
                {
                    dec.DonationStatus = DonationStatus.NotLive;
                    _db.ReportedDeath.Update(dec);
                }
            }

            _db.Cause.Update(cause);

            // Save change logs before saving cause
            if (changeLogs.Any())
            {
                await _db.ChangeLogs.AddRangeAsync(changeLogs, ct);
            }

            // Save all changes at once
            await _db.SaveChangesAsync(ct);

            // Record audit log
            string auditMessage = $"Edited cause details for: {model.DependentName}. Changes: " +
                string.Join(", ", changeLogs.Select(c => $"{c.FieldChanged} changed from '{c.OldValue}' to '{c.NewValue}'"));

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit cause", auditMessage, ct);

            TempData[SD.Success] = "Cause Updated Successfully...";

            return RedirectToAction("ApprovedDeaths", "Cause");
        }


        //[HttpPost]
        //[Authorize(Roles = RoleList.GeneralAdmin)]
        //public async Task<IActionResult> EditCause(CauseViewModel model, CancellationToken ct)
        //{
        //    var email = HttpContext.Session.GetString("loginEmail");
        //    if (email == null)
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }
        //    var currentUser = await _userManager.FindByEmailAsync(email);
        //    if (currentUser == null)
        //    {
        //        TempData["Error"] = "User not found.";
        //        return RedirectToAction("Index");
        //    }
        //    ModelState.Remove(nameof(model.ReportedDeaths));
        //    ModelState.Remove(nameof(model.DateJoinedAsString));
        //    ModelState.Remove(nameof(model.YearOfBirth));
        //    ModelState.Remove(nameof(model.DateOfDeathAsString));
        //    ModelState.Remove(nameof(model.DependentName));
        //    ModelState.Remove(nameof(model.DateJoined));
        //    ModelState.Remove(nameof(model.DateOfDeath));
        //    ModelState.Remove(nameof(model.RegistrationNumber));
        //    ModelState.Remove(nameof(model.PersonRegNumber));
        //    ModelState.Remove(nameof(model.PersonYearOfBirth));
        //    ModelState.Remove(nameof(model.StartDate));
        //    ModelState.Remove(nameof(model.EndDate));
        //    ModelState.Remove(nameof(model.MissPaymentAmount));
        //    ModelState.Remove(nameof(model.IsActive));


        //    if (!ModelState.IsValid)
        //    {
        //        // Fetch the list of reported deaths from the database
        //        var reportedDeaths = await _db.ReportedDeath
        //            .Select(rd => new SelectListItem
        //            {
        //                Value = rd.Id.ToString(),
        //                Text = rd.DeceasedName
        //            })
        //            .ToListAsync(ct);

        //        model.ReportedDeaths = reportedDeaths;
        //        return View(model);
        //    }
        //    var dec = await _db.ReportedDeath.FirstOrDefaultAsync(a => a.Id == model.ReportedDeathId);

        //    var cause = await _db.Cause.FirstOrDefaultAsync(a => a.DeathId == model.ReportedDeathId);
        //    if(cause != null)
        //    {
        //        model.StartDate ??= cause.StartDate;
        //        model.EndDate ??= cause.EndDate;

        //        cause.Summary = model.Summary;
        //        cause.StartDate = model.StartDate;
        //        cause.EndDate = model.EndDate;  
        //        cause.IsActive = model.IsActive;
        //        cause.IsDisplayable = model.IsDisplayable;
        //        cause.MissPaymentAmount = model.MissPaymentAmount;
        //        cause.Description = model.Description;                            
        //        cause.TargetAmount = model.TargetAmount;
        //        cause.FullMemberAmount = model.FullMemberAmount;
        //        cause.UnderAge = model.UnderAge;
        //        cause.UnderAgeAmount = model.UnderAgeAmount;            
        //        cause.UpdatedOn = DateTime.UtcNow;             
        //    }

        //    if (model.IsActive == true)
        //    {

        //        if (dec != null)
        //        {
        //            dec.DonationStatus = DonationStatus.Live;
        //            _db.ReportedDeath.Update(dec);
        //        }
        //    }
        //    else
        //    {
        //        if (dec != null)
        //        {
        //            dec.DonationStatus = DonationStatus.NotLive;
        //            _db.ReportedDeath.Update(dec);
        //        }

        //    }

        //    if (model.IsDisplayable == true)
        //    {               

        //        cause.IsDisplayable = true;
        //        _db.Cause.Update(cause);
        //    }
        //    else
        //    {
        //        cause.IsDisplayable = false;
        //        _db.Cause.Update(cause);

        //    }


        //    _db.Cause.Update(cause);
        //    await _db.SaveChangesAsync(ct);

        //    List<ChangeLog> changeLogs = new List<ChangeLog>();
        //    // Log changes to fields
        //    void LogChange(string field, object? oldValue, object? newValue)
        //    {
        //        if (!Equals(oldValue, newValue))
        //        {
        //            changeLogs.Add(new ChangeLog
        //            {
        //                UserId = currentUser.UserId,
        //                DependentId = currentUser.DependentId,
        //                FieldChanged = field,
        //                OldValue = oldValue?.ToString(),
        //                NewValue = newValue?.ToString(),
        //                ChangeDate = DateTime.UtcNow,
        //                ChangedBy = email
        //            });
        //        }
        //    }
        //    if (cause != null)
        //    {
        //        LogChange("Summary", cause.Summary, model.Summary);
        //        LogChange("StartDate", cause.StartDate?.ToString("dd/MM/yyyy"), model.StartDate?.ToString("dd/MM/yyyy"));
        //        LogChange("EndDate", cause.EndDate?.ToString("dd/MM/yyyy"), model.EndDate?.ToString("dd/MM/yyyy"));
        //        LogChange("IsActive", cause.IsActive, model.IsActive);
        //        LogChange("IsDisplayable", cause.IsDisplayable, model.IsDisplayable);
        //        LogChange("MissPaymentAmount", cause.MissPaymentAmount, model.MissPaymentAmount);
        //        LogChange("Description", cause.Description, model.Description);
        //        LogChange("TargetAmount", cause.TargetAmount, model.TargetAmount);
        //        LogChange("FullMemberAmount", cause.FullMemberAmount, model.FullMemberAmount);
        //        LogChange("UnderAge", cause.UnderAge, model.UnderAge);
        //        LogChange("UnderAgeAmount", cause.UnderAgeAmount, model.UnderAgeAmount);

        //        // Update cause properties with the new model values
        //        cause.Summary = model.Summary;
        //        cause.StartDate = model.StartDate;
        //        cause.EndDate = model.EndDate;
        //        cause.IsActive = model.IsActive;
        //        cause.IsDisplayable = model.IsDisplayable;
        //        cause.MissPaymentAmount = model.MissPaymentAmount;
        //        cause.Description = model.Description;
        //        cause.TargetAmount = model.TargetAmount;
        //        cause.FullMemberAmount = model.FullMemberAmount;
        //        cause.UnderAge = model.UnderAge;
        //        cause.UnderAgeAmount = model.UnderAgeAmount;
        //        cause.UpdatedOn = DateTime.UtcNow;
        //    }

        //    if (changeLogs.Any())
        //    {
        //        await _db.ChangeLogs.AddRangeAsync(changeLogs, ct);
        //        await _db.SaveChangesAsync(ct);
        //    }
        //    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit cause", "Edit cause details for: " + model.DependentName, ct);

        //    TempData[SD.Success] = "Cause Updated Successfully...";

        //    return RedirectToAction("ApprovedDeaths", "Cause");
        //}





        [ActionName("GetReg")]
        public async Task<IActionResult> GetReg(int id)
        {
            Dependant model = new Dependant();

            model = await _db.Dependants.Where(a => a.Id == id).FirstOrDefaultAsync();

            if (model == null)
            {
                return NotFound();
            }

            var formattedDate = model.DateCreated.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            return Json(new
            {
                personRegNumber = model.PersonRegNumber,
                yrbirth = model.PersonYearOfBirth,
                datejoined = formattedDate
            });
        }

        public IActionResult DownloadPaidPayments()
        {
            var pdf = new ActionAsPdf("PaidPaymentsPDF")
            {
                FileName = "PaidPayments.pdf"
            };

            return pdf;
        }

        public IActionResult DownloadMissedPayments()
        {


            var pdf = new ActionAsPdf("MissedPaymentsPDF")
            {
                FileName = "MissedPayments.pdf"
            };

            return pdf;
        }


        [HttpGet]
     
        public async Task<IActionResult> PaidPaymentsPDF(CancellationToken ct)
        {
            try
            {
            
                // Fetch necessary data
                var payments = await _db.Payment.ToListAsync(ct);
                var paymentSessions = await _db.PaymentSessions.ToListAsync(ct);
                var dependents = await _db.Dependants.Where(a=> a.IsActive == true || a.IsActive == null).ToListAsync(ct);
                var displayedSessions = new HashSet<string>();
                // Match payments with their respective sessions
                var paidPayments = payments.Select(p =>
                {
                    var session = paymentSessions.FirstOrDefault(ps => ps.OurRef == p.OurRef);
                    bool isFirstForSession = displayedSessions.Add(p.OurRef); // Adds & checks if already exists

                    return new PaymentDetailViewModel
                    {
                        Id = p.Id,
                        CauseCampaignpRef = p.CauseCampaignpRef,
                        Amount = p.Amount,
                        TransactionFees = isFirstForSession ? (session?.TransactionFees ?? 0m) : 0m, // Show only once
                        TotalAmount = isFirstForSession ? (p.Amount + session?.TotalAmount ?? 0m) : 0m + p.GoodwillAmount, // Show only once
                        GoodwillAmount = p.GoodwillAmount,                     
                        DateCreated = p.DateCreated,
                        DependentName = dependents.FirstOrDefault(d => d.PersonRegNumber == p.personRegNumber)?.PersonName ?? "Unknown",
                        OurRef = p.OurRef
                    };
                }).ToList();

                // Pass paid payments to the view

                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "PaidPaymentsPDF", $"User downloaded PaidPaymentsPDF.", ct);
                return View(paidPayments);
            }
            catch (Exception ex)
            {
                // Handle exceptions and log if necessary
                TempData["Error"] = "An error occurred while generating the PDF. Please try again.";
                return RedirectToAction("PaymentDashboard");
            }
        }
       
        [HttpGet]
       
        public async Task<IActionResult> MissedPaymentsPDF(CancellationToken ct)
        {
            try
            {
              
                // Fetch necessary data
                var payments = await _db.Payment.ToListAsync(ct);
                var dependents = await _db.Dependants.Where(a => a.IsActive == true || a.IsActive == null).ToListAsync(ct);
                var closedCauses = await _db.Cause
                .Where(c => c.IsClosed && !c.IsActive)
                .ToListAsync(ct);


                // Calculate missed payments
                var missedPayments = new List<AdminMissedPaymentViewModel>();
                foreach (var cause in closedCauses)
                {   
                    var unpaidDependents = dependents
                   .Where(d =>
                       !payments.Any(p => p.personRegNumber == d.PersonRegNumber && p.CauseCampaignpRef == cause.CauseCampaignpRef) &&
                       CalculateAgeAtYear(d.PersonYearOfBirth.ToString(), cause.DateCreated) >= 25 &&
                       d.DateCreated <= cause.IsClosedDate 
                   )
                   .ToList();

                    foreach (var dependent in unpaidDependents)
                    {
                        missedPayments.Add(new AdminMissedPaymentViewModel
                        {
                            DependentName = dependent.PersonName,
                            YearOfBirth = dependent.PersonYearOfBirth,
                            PhoneNumber = dependent.Telephone,
                            RegNumber = dependent.PersonRegNumber,
                            CauseCampaignpRef = cause.CauseCampaignpRef,
                            Amount = cause.FullMemberAmount,
                            IsClosedDate = cause.IsClosedDate
                        });
                    }
                }

                // Pass missed payments to the view
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "MissedPaymentsPDF", $"User downloaded MissedPaymentsPDF.", ct);
                return View(missedPayments);
            }
            catch (Exception ex)
            {
                // Handle exceptions and log if necessary
                TempData["Error"] = "An error occurred while generating the PDF. Please try again.";
                return RedirectToAction("PaymentDashboard");
            }
        }


        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin)]

        public async Task<IActionResult> DeactivateDependents(List<int> selectedDependents, string deactivationReason, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

            if (selectedDependents == null || !selectedDependents.Any())
            {
                TempData["Error"] = "No dependents selected for deactivation.";
                return RedirectToAction(nameof(PaymentDashboard));
            }

            if (string.IsNullOrWhiteSpace(deactivationReason))
            {
                TempData["Error"] = "Deactivation reason is required.";
                return RedirectToAction(nameof(PaymentDashboard));
            }

            try
            {
                // Fetch dependents to deactivate
                var dependentsToDeactivate = await _db.Dependants
                    .Where(d => selectedDependents.Contains(d.Id))
                    .ToListAsync(ct);

                foreach (var dependent in dependentsToDeactivate)
                {
                    dependent.IsActive = false; 

                    // Check if the dependent has a user account
                    var user = await _db.Users.FirstOrDefaultAsync(u => u.PersonRegNumber == dependent.PersonRegNumber, ct);
                    if (user != null)
                    {
                        user.IsActive = false; // Deactivate account
                        user.DeactivationReason = deactivationReason; // Add reason
                    }
                }

                // Save changes to the database
                await _db.SaveChangesAsync(ct);

                // Audit logging
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "DeactivateDependents",
                    $"Admin deactivated {dependentsToDeactivate.Count} dependents with reason: {deactivationReason}", ct);

                TempData["Success"] = "Selected dependents have been deactivated successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while deactivating dependents: {ex.Message}";
            }

            return RedirectToAction(nameof(PaymentDashboard));
        }


        
        private async Task NotifyMembers(Cause cause, CancellationToken ct)
        {       

            //  Fetch active users
            var dependents = await _db.Users
                .Where(d => !string.IsNullOrEmpty(d.Email) && d.IsActive == true)
                .ToListAsync(ct);

            if (!dependents.Any()) return; //  Exit if no recipients found

            //  Load email template
            const string templatePath = @"EmailTemplate/CauseCreated.html";
            //const string templatePath = @"EmailTemplate/test.html"; 
            string emailTemplate = await System.IO.File.ReadAllTextAsync(templatePath, ct); // Read template once

            string startDate = cause.StartDate?.ToString("dd/MM/yyyy") ?? "N/A";
            string endDate = cause.EndDate?.ToString("dd/MM/yyyy") ?? "N/A";

            //  Prepare email messages
            var emailMessages = dependents.Select(dependent => new PostmarkMessage
            {
                From = "info@umojawetu.com",
                To = dependent.Email,
                Subject = $"New Cause Created: {cause.CauseCampaignpRef}",
                HtmlBody = emailTemplate
                    .Replace("{{userName}}", $"{dependent.FirstName} {dependent.Surname}")
                    .Replace("{{causeName}}", cause.CauseCampaignpRef)
                    .Replace("{{startDate}}", startDate)
                    .Replace("{{endDate}}", endDate)
                    .Replace("{{causeTarget}}", cause.TargetAmount.ToString("C"))
                    .Replace("{{ref}}", "cause created 003"),
                MessageStream = "broadcast",
                TrackOpens = true,
                TrackLinks = "None"
            }).ToList();

            //  Split into batches of 500
            const int BATCH_SIZE = 500;
            var emailBatches = emailMessages
                .Select((email, index) => new { email, index })
                .GroupBy(x => x.index / BATCH_SIZE)
                .Select(group => group.Select(x => x.email).ToList())
                .ToList();

            List<string> failedEmails = new();
            List<string> successEmails = new();

            using var httpClient = new HttpClient { BaseAddress = new Uri("https://api.postmarkapp.com") };
            var postmarkApiKey = _configuration["Postmark:ApiKey"];
            httpClient.DefaultRequestHeaders.Add("X-Postmark-Server-Token", postmarkApiKey);

            foreach (var batch in emailBatches)
            {
                var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var jsonPayload = JsonSerializer.Serialize(batch, jsonOptions);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("/email/batch", content, ct);
                var responseContent = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    await RecordAuditAsync("Email API Failure", "Cause Notification",
                        $"Batch sending failed. Response: {responseContent}", ct);
                    continue; // Skip to next batch
                }

                var postmarkResponse = JsonSerializer.Deserialize<List<PostmarkResponseMessage>>(responseContent, jsonOptions);
                if (postmarkResponse == null)
                {
                    await RecordAuditAsync("Email API Failure", "Cause Notification",
                        $"Unexpected response format from Postmark: {responseContent}", ct);
                    continue;
                }

                failedEmails.AddRange(postmarkResponse.Where(m => m.ErrorCode != 0).Select(m => m.To));
                successEmails.AddRange(postmarkResponse.Where(m => m.ErrorCode == 0).Select(m => m.To));
            }

            //  Log successful emails
            if (successEmails.Any())
            {
                await RecordAuditAsync("Email Success", "Cause Notification",
                    $"Successfully notified {successEmails.Count} members about new cause {cause.CauseCampaignpRef}.", ct);
            }

            //  Log failed emails
            if (failedEmails.Any())
            {
                await RecordAuditAsync("Email Failure", "Cause Notification",
                    $"Failed to notify {failedEmails.Count} members about new cause {cause.CauseCampaignpRef}.", ct);
            }

            //  Display appropriate success/failure messages
            if (failedEmails.Count == 0)
            {
                TempData[SD.Success] = "All members have been notified successfully!";
            }
            else if (successEmails.Count > 0)
            {
                TempData[SD.Warning] = $"Notified {successEmails.Count} members successfully, but {failedEmails.Count} emails failed.";
            }
            else
            {
                TempData[SD.Error] = "All email notification attempts failed!";
            }
        }


    

    }
}