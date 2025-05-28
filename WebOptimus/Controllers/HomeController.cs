using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using UAParser;
using WebOptimus.Data;
using WebOptimus.Helpers;
using WebOptimus.Models;
using WebOptimus.Models.ViewModel;
using WebOptimus.Services;
using WebOptimus.StaticVariables;

namespace WebOptimus.Controllers
{
    public class HomeController : BaseController
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
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IMemoryCache _memoryCache;
        public HomeController(IMapper mapper, UserManager<User> userManager,
           SignInManager<User> signInManager, IWebHostEnvironment hostEnvironment, IMemoryCache memoryCache, RoleManager<IdentityRole> roleManager, IUserStore<User> userStore,
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
            _hostEnvironment = hostEnvironment;
            _memoryCache = memoryCache;
        }

        public async Task<IActionResult> Index()
        {
          
            var _Headers = HttpContext.Request.Headers["User-Agent"];
            var _Parser = Parser.GetDefault();
            ClientInfo _ClientInfo = _Parser.Parse(_Headers);

            ViewBag.HasPublicAnnouncements = await _db.Announcements.AnyAsync(a => a.IsActiveToPublic == true);

            History vm = new History
            {
                Name = "",
                Browser = _ClientInfo.UA.Family,
                OperatingSystem = _ClientInfo.OS.Family,
                Device = _ClientInfo.Device.Family,
                PublicIP = _requestIpHelper.GetRequestIp(),
                CreatedDate = DateTime.UtcNow
            };

            _db.History.Add(vm);
            await _db.SaveChangesAsync();


            // Fetch active banner text from the database
            var bannerText = await _db.Banners
            .Where(b => b.IsActive == true)
            .Select(b => b.Title)
           .FirstOrDefaultAsync();
            ViewBag.BannerText = bannerText;
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Contact()
        {
            ViewBag.HasPublicAnnouncements = await _db.Announcements.AnyAsync(a => a.IsActiveToPublic == true);
            // Fetch active banner text from the database
            var bannerText = await _db.Banners
            .Where(b => b.IsActive == true)
            .Select(b => b.Title)
           .FirstOrDefaultAsync();
            ViewBag.BannerText = bannerText;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(Contact _data, CancellationToken ct)
        {
            try
            {

                if (_data != null)
                {
                    _data.DateCreated = DateTime.UtcNow;
                    _data.UpdateOn = DateTime.UtcNow;
                    _db.Contacts.Add(_data);
                    _db.SaveChanges();

                    TempData[SD.Success] = "Thank you! Your message has been submitted successfully. A member of the team will be in touch with you shortly.";
                    return RedirectToAction(nameof(Contact));
                }
                else
                {

                    return View();
                }

            }
            catch (Exception ex)
            {

                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Contact", "HC01: failed to submit contact form because:" + ex.Message.ToString(), ct);
                TempData[SD.Success] = "Message not sent. Please Email or Call us quoting error code: HC01.";
                return View();
            }

        }
        [HttpGet]
        public async Task<IActionResult> Donation(CancellationToken ct)
        {
            try
            {
                var currentUserEmail = HttpContext.Session.GetString("loginEmail");
                if (currentUserEmail == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

                //  Cache Death-Related Causes
                var allCauses = await _memoryCache.GetOrCreateAsync("AllCauses", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    return await _db.Cause.AsNoTracking().ToListAsync(ct);
                });


                //  Cache Non-Death Donations
                var allDonations = await _memoryCache.GetOrCreateAsync("AllDonations", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    return await _db.DonationForNonDeathRelated.AsNoTracking().ToListAsync(ct);
                });

                //  Cache Payments for Death-Related Causes
                var causePayments = await _memoryCache.GetOrCreateAsync("AllCausePayments", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    return await _db.Payment
                        .Where(p => p.HasPaid)
                        .AsNoTracking()
                        .ToListAsync(ct);
                });

                //  Cache Payments for Non-Death Donations
                var otherPayments = await _memoryCache.GetOrCreateAsync("AllOtherPayments", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    return await _db.OtherDonationPayment
                        .Where(p => p.HasPaid)
                        .AsNoTracking()
                        .ToListAsync(ct);
                });

                //  Fetch Dependants (for both death-related and non-death-related donations)
                var dependants = await _db.Dependants
     .Where(d => causePayments.Select(p => p.personRegNumber).Contains(d.PersonRegNumber) ||
                 otherPayments.Select(p => p.PersonRegNumber).Contains(d.PersonRegNumber))
     .GroupBy(d => d.PersonRegNumber) // Group by PersonRegNumber to handle duplicates
     .ToDictionaryAsync(g => g.Key, g => g.First().PersonName); // Take the first name


                //  Compute Death-Related Donations (Grouped by CauseCampaignpRef)
                //var causePaymentDetails = causePayments
                //    .GroupBy(p => p.CauseCampaignpRef)
                //    .ToDictionary(
                //        g => g.Key,
                //        g => g.Sum(p => p.Amount)
                //    );
                var causePaymentDetails = _db.PaymentSessions // Use PaymentSessions to match PaymentDashboard
          .Where(ps => ps.IsPaid) // Only count fully paid sessions
          .GroupBy(ps => ps.CauseCampaignpRef)
          .ToDictionary(
              g => g.Key,
              g => g.Sum(ps => ps.TotalAmount - ps.TransactionFees) // Subtract transaction fees
          );


                //  Compute Non-Death Donations (Grouped by CauseCampaignpRef)
                var donationPaymentDetails = otherPayments
                    .GroupBy(p => p.CauseCampaignpRef)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(p => p.Amount)
                    );

                //  Replace CreatedBy with the Dependant Name for Death-Related Donations
                foreach (var payment in causePayments)
                {
                    if (dependants.ContainsKey(payment.personRegNumber))
                    {
                        payment.CreatedBy = dependants[payment.personRegNumber]; // Replace CreatedBy with Name
                    }
                }

                //  Replace CreatedBy with the Dependant Name for Non-Death-Related Donations
                foreach (var payment in otherPayments)
                {
                    if (dependants.ContainsKey(payment.PersonRegNumber))
                    {
                        payment.CreatedBy = dependants[payment.PersonRegNumber]; // Replace CreatedBy with Name
                    }
                }

                //  Build ViewModel
                var donorVM = new DonorVM
                {
                    Causes = new List<Cause>(),
                    Donations = new List<DonationForNonDeathRelated>(),
                    User = currentUser,
                    Donors = causePayments
                    .GroupBy(p => new { p.personRegNumber, p.CauseCampaignpRef })
                    .Select(g => g.First())
                    .OrderByDescending(p => p.DateCreated)
                    .ToList(),


                    OtherDonationPayments = otherPayments
                    .GroupBy(p => new { p.PersonRegNumber, p.CauseCampaignpRef })
                    .Select(g => g.First())
                    .OrderByDescending(p => p.DateCreated)
                    .ToList()



                };

                //  Process Death-Related Causes
                foreach (var cause in allCauses)
                {
                    var totalAmountRaised = causePaymentDetails.ContainsKey(cause.CauseCampaignpRef)
                        ? causePaymentDetails[cause.CauseCampaignpRef]
                        : 0m;

                    var totalGoodwill = causePayments
      .Where(p => p.CauseCampaignpRef == cause.CauseCampaignpRef)
      .Sum(p => p.GoodwillAmount);

                    donorVM.Causes.Add(new Cause
                    {
                        Id = cause.Id,
                        CauseCampaignpRef = cause.CauseCampaignpRef,
                       Summary = cause.Summary,
                        IsActive = cause.IsActive,
                        TargetAmount = cause.TargetAmount,
                        Goodwill = totalGoodwill,                       
                        Description = cause.Description,
                        DateCreated = cause.DateCreated,
                        IsDisplayable = cause.IsDisplayable,
                        AmountRaised = totalAmountRaised 
                    });
                }

                //  Process Non-Death Donations
                foreach (var donation in allDonations)
                {
                    var totalAmountRaised = donationPaymentDetails.ContainsKey(donation.CauseCampaignpRef)
                        ? donationPaymentDetails[donation.CauseCampaignpRef]
                        : 0m;

                    donorVM.Donations.Add(new DonationForNonDeathRelated
                    {
                        Id = donation.Id,
                        CauseCampaignpRef = donation.CauseCampaignpRef,
                        IsActive = donation.IsActive,
                        AmountRaised = totalAmountRaised,
                        TargetAmount = donation.TargetAmount,
                        StartDate = donation.StartDate,
                        Summary = donation.Summary,
                        ClosedDate = donation.ClosedDate ?? null,
                        Description = donation.Description,
                        DateCreated = donation.DateCreated,
                        IsDisplayable = donation.IsDisplayable
                    });
                }

                //  Separate active and ended causes (Death-Related)
                ViewBag.ActiveCauses = donorVM.Causes
                    .Where(c => c.IsActive)
                    .OrderByDescending(c => c.DateCreated)
                    .ToList();

                ViewBag.EndedCauses = donorVM.Causes
                    .Where(c => !c.IsActive && c.IsDisplayable)
                    .OrderByDescending(c => c.DateCreated)
                    .ToList();
                var endedCauseLateFees = _db.PaymentSessions
    .Where(ps => ps.IsPaid == true) 
    .GroupBy(ps => ps.CauseCampaignpRef)
    .ToDictionary(
        g => g.Key,
        g => g.Sum(ps => ps.TotalAmount - (ps.TransactionFees +
                                           causePayments.Where(p => p.CauseCampaignpRef == g.Key).Sum(p => p.Amount + p.GoodwillAmount)))
    );

                // Store Late Payment Fees for ended causes in ViewBag
                ViewBag.EndedCauseLateFees = endedCauseLateFees;
                //  Separate active and ended non-death donations
                ViewBag.ActiveDonations = donorVM.Donations
                    .Where(d => d.IsActive)
                    .OrderByDescending(d => d.DateCreated)
                    .ToList();

                ViewBag.EndedDonations = donorVM.Donations
                    .Where(d => !d.IsActive && d.IsDisplayable)
                    .OrderByDescending(d => d.DateCreated)
                    .ToList();

                return View(donorVM);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }


        //[HttpGet]
        //[ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client, NoStore = false)]

        //public async Task<IActionResult> Donation(CancellationToken ct)
        //{
        //    try
        //    {
        //        var currentUserEmail = HttpContext.Session.GetString("loginEmail");
        //        if (currentUserEmail == null)
        //        {
        //            return RedirectToAction("Login", "Account");
        //        }

        //        var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

        //        // Cache Causes
        //        var allCauses = await _memoryCache.GetOrCreateAsync("AllCauses", async entry =>
        //        {
        //            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
        //            return await _db.Cause.AsNoTracking().ToListAsync(ct);
        //        });

        //        // Cache Payments
        //        var payments = await _memoryCache.GetOrCreateAsync("AllPayments", async entry =>
        //        {
        //            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
        //            return await _db.Payment.Where(p => p.HasPaid).AsNoTracking().ToListAsync(ct);
        //        });

        //        // Fetch PaymentSessions
        //        var paymentSessions = await _db.PaymentSessions
        //            .Where(ps => ps.IsPaid)
        //            .ToListAsync(ct);

        //        // Combine PaymentSessions and Payments to calculate AmountRaised
        //        var paymentDetails = paymentSessions
        //            .GroupBy(ps => ps.CauseCampaignpRef)
        //            .ToDictionary(
        //                g => g.Key,
        //                g => new
        //                {
        //                    TotalAmountRaised = g.Sum(ps => ps.TotalAmount - ps.TransactionFees), // Subtract fees
        //                    TotalGoodwill = g.Sum(ps => payments
        //                        .Where(p => p.OurRef == ps.OurRef)
        //                        .Sum(p => p.GoodwillAmount)) // Include goodwill
        //                });

        //        // Build ViewModel
        //        var causesVM = new DonorVM
        //        {
        //            Causes = new List<Cause>(),
        //            User = currentUser,
        //            Donors = payments
        //        };

        //        foreach (var cause in allCauses)
        //        {
        //            // Calculate AmountRaised and Goodwill
        //            var causeData = paymentDetails.ContainsKey(cause.CauseCampaignpRef)
        //                ? paymentDetails[cause.CauseCampaignpRef]
        //                : new { TotalAmountRaised = 0m, TotalGoodwill = 0m };

        //            cause.AmountRaised = causeData.TotalAmountRaised;
        //            cause.Goodwill = causeData.TotalGoodwill;

        //            causesVM.Causes.Add(new Cause
        //            {
        //                Id = cause.Id,
        //                CauseCampaignpRef = cause.CauseCampaignpRef,
        //                IsActive = cause.IsActive,
        //                AmountRaised = cause.AmountRaised,
        //                Goodwill = cause.Goodwill,
        //                TargetAmount = cause.TargetAmount,
        //                Description = cause.Description,
        //                DateCreated = cause.DateCreated
        //            });
        //        }

        //        // Separate active and ended causes
        //        ViewBag.ActiveCauses = causesVM.Causes
        //            .Where(c => c.IsActive == true && c.IsDisplayable)
        //            .ToList();

        //        ViewBag.EndedCauses = causesVM.Causes
        //            .Where(c => c.IsClosed && c.IsDisplayable)
        //            .ToList();

        //        return View(causesVM);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error: {ex.Message}");
        //        return RedirectToAction("Error", "Home");
        //    }
        //}


        public async Task<IActionResult> Dashboard(CancellationToken ct)
        {
            try
            {
                var currentUserEmail = HttpContext.Session.GetString("loginEmail");

                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    return RedirectToAction("Login", "Account");
                }
				var minimumage = await _db.Settings
				.Where(s => s.IsActive == true && s.MinimumAge > 0)
				.Select(s => s.MinimumAge)
				.FirstOrDefaultAsync();
				
				if (minimumage == 0)
				{
					minimumage = 18;
				}
				var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                //  Fetch all dependents in the system for total statistics
                var allDependentsInDb = await _db.Dependants
                    .Where(d => d.IsActive == true || d.IsActive == null)
                    .ToListAsync(ct);

                //  Fetch dependents of the current user only
                var userDependents = allDependentsInDb
                    .Where(d => d.UserId == currentUser.UserId)
                    .ToList();

                //  Convert to `Dependant` objects for ViewModel
                var dependentsunder18 = userDependents
                    .Where(d => CalculateAge(d.PersonYearOfBirth.ToString()) < minimumage)
                    .ToList();

                var dependentsover18 = userDependents
                    .Where(d => CalculateAge(d.PersonYearOfBirth.ToString()) >= minimumage)
                    .ToList();

                var totalDependents = userDependents.Count;
                var under18Count = dependentsunder18.Count;
                var over18Count = dependentsover18.Count;

                //  Fetch total number of dependents across all regions
                var totalDependentsInDb = allDependentsInDb.Count;
                var under18InDb = allDependentsInDb.Count(d => CalculateAge(d.PersonYearOfBirth.ToString()) < minimumage);
                var over18InDb = allDependentsInDb.Count(d => CalculateAge(d.PersonYearOfBirth.ToString()) >= minimumage);

                //  Fetch total amount paid by the user
                var payments = await _db.Payment
                    .Where(p => p.personRegNumber == currentUser.PersonRegNumber)
                    .ToListAsync(ct);

                var totalContribution = payments.Sum(p => p.Amount);
                var totalGoodwill = payments.Sum(p => p.GoodwillAmount);
                var totalAmount = totalContribution + totalGoodwill;
                var regionId = currentUser.RegionId;
                //  Fetch all users in the same region as the current user
                var dependentsInRegion = allDependentsInDb
                 .Where(d => d.RegionId == regionId)
                 .ToList();

                var totalover18DependentsInRegion = dependentsInRegion.Count(d => CalculateAge(d.PersonYearOfBirth.ToString()) >= minimumage);
                var totalunder18DependentsInRegion = dependentsInRegion.Count(d => CalculateAge(d.PersonYearOfBirth.ToString()) < minimumage);
                var numberOfPeopleInRegion = dependentsInRegion.Count;



                var regionName = await _db.Region.FirstOrDefaultAsync(a => a.Id == currentUser.RegionId);
                if (regionName != null)
                {
                    ViewBag.RegionName = regionName.Name;
                }

                var otherDonations = await _db.OtherDonationPayment
                    .Where(p => p.UserId == currentUser.UserId && p.PersonRegNumber == currentUser.PersonRegNumber)
                    .ToListAsync(ct);

                var totalOtherDonations = otherDonations.Sum(p => p.Amount);

                // Populate ViewModel with all necessary stats
                var dashboardViewModel = new PaymentDashboardViewModel
                {
                    TotalAmount = totalAmount + totalOtherDonations,
                    TotalPayments = payments.Count,
                    TotalDependents = totalDependents,
                    over18Dependents = over18Count,
                    under18Dependents = under18Count,
                    TotalDependentsInDb = totalDependentsInDb, // Ensures "Total Members" displays correctly
                    under18DependentsInDb = under18InDb,      // Ensures "Under 25s" displays correctly
                    over18DependentsInDb = over18InDb,        // Ensures "Over 25s" displays correctly
                    Dependentsunder18 = dependentsunder18,
                    Dependentsover18 = dependentsover18,
                    NumberOfPeopleInRegion = numberOfPeopleInRegion,
                    over18DependentsInRegion = totalover18DependentsInRegion,
                    under18DependentsInRegion = totalunder18DependentsInRegion,
                    DateJoined = currentUser.DateCreated
                };
				ViewBag.minimumAge = minimumage;
				return View(dashboardViewModel);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync("Dashboard Error", "Dashboard", $"Error fetching dashboard data: {ex.Message}", ct);
                return RedirectToAction("Error", "Home");
            }
        }

        private int CalculateAge(string dateOfBirth)
        {
            if (string.IsNullOrWhiteSpace(dateOfBirth)) return -1;

            if (int.TryParse(dateOfBirth, out int year))
            {
                var today = DateTime.Today;
                return today.Year - year;
            }

            DateTime dob;
            var formats = new[] { "d/M/yyyy" };
            if (DateTime.TryParseExact(dateOfBirth, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dob))
            {
                var today = DateTime.Today;
                var age = today.Year - dob.Year;
                if (dob.Date > today.AddYears(-age)) age--;
                return age;
            }

            return -1;
        }

        //private int CalculateAge(string dateOfBirth)
        //{
        //    DateTime dob;
        //    var formats = new[] { "d/M/yyyy", "yyyy" };
        //    if (DateTime.TryParseExact(dateOfBirth, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dob))
        //    {
        //        var today = DateTime.Today;
        //        var age = today.Year - dob.Year;
        //        if (dob.Date > today.AddYears(-age)) age--;
        //        return age;
        //    }
        //    return -1; // or handle the error accordingly
        //}

        public async Task<IActionResult> About()
        {
            ViewBag.HasPublicAnnouncements = await _db.Announcements.AnyAsync(a => a.IsActiveToPublic == true);
            var bannerText = await _db.Banners
          .Where(b => b.IsActive == true)
          .Select(b => b.Title)
         .FirstOrDefaultAsync();
            ViewBag.BannerText = bannerText;
            return View();
        }
        //public async Task<IActionResult> SpecialAnnouncement(CancellationToken ct)
        //{
        //    var currentUserEmail = HttpContext.Session.GetString("loginEmail");

        //    if (currentUserEmail == null)
        //    {
        //        return RedirectToAction("Login", "Account");
        //    }

        //    var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
        //    try
        //    {

        //        AnnouncementViewModel vm = new AnnouncementViewModel()
        //        {
        //            News = await _db.Announcements.Where(a => a.IsActiveToInternMember == true || a.IsActiveToPublic == true).ToListAsync(),
        //        };
        //        if (vm.News.Count > 0)
        //        {
        //            foreach (var i in vm.News)
        //            {
        //                vm.formatDate = i.Date.ToString("dd/MM/yyyy h:mm:ss tt");

        //            }
        //            ViewBag.news = "yes";
        //            return View(vm);
        //        }

        //        ViewBag.news = "no";

        //        await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "SpecialAnnouncement", "User navigated to SpecialAnnouncement page.", ct);

        //        return View();
        //    }
        //    catch (Exception ex)
        //    {
        //        ViewBag.news = "no";
        //        return View();
        //    }
        //}

        public async Task<IActionResult> PublicAnnouncement(CancellationToken ct)
        {


            try
            {
                ViewBag.HasPublicAnnouncements = await _db.Announcements.AnyAsync(a => a.IsActiveToPublic == true);

                // Fetch announcements that are active for internal members or the public
                var announcements = await _db.Announcements
             .Where(a => a.IsActiveToInternMember == true || a.IsActiveToPublic == true)
             .OrderByDescending(a => a.Date) 
             .ToListAsync(ct);

                var vm = new AnnouncementViewModel
                {
                    News = announcements
                };

                if (vm.News.Count > 0)
                {
                    foreach (var announcement in vm.News)
                    {
                        vm.formatDate = announcement.Date.ToString("dd/MM/yyyy h:mm:ss tt") ?? string.Empty;
                    }

                    ViewBag.news = "yes";
                    return View(vm);
                }

                ViewBag.news = "no";

                return View(vm);
            }
            catch (Exception ex)
            {
                ViewBag.news = "no";
                return View();
            }
        }

        public async Task<IActionResult> SpecialAnnouncement(CancellationToken ct)
        {
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");

            if (currentUserEmail == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
            try
            {
                // Fetch announcements that are active for internal members or the public
                var announcements = await _db.Announcements
                    .Where(a => a.IsActiveToInternMember == true || a.IsActiveToPublic == true)
                    .OrderByDescending(a => a.Date)
                    .ToListAsync(ct);

                var vm = new AnnouncementViewModel
                {
                    News = announcements
                };

                if (vm.News.Count > 0)
                {
                    foreach (var announcement in vm.News)
                    {
                        vm.formatDate = announcement.Date.ToString("dd/MM/yyyy h:mm:ss tt") ?? string.Empty;
                    }

                    ViewBag.news = "yes";
                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "SpecialAnnouncement", "User navigated to SpecialAnnouncement page.", ct);
                    return View(vm);
                }

                ViewBag.news = "no";
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "SpecialAnnouncement", "No announcements available.", ct);
                return View(vm);
            }
            catch (Exception ex)
            {
                ViewBag.news = "no";
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "SpecialAnnouncement", $"ERROR: {ex.Message}", ct);
                return View();
            }
        }

        public IActionResult Services()
        {
            return View();
        }
        public async Task<IActionResult> Privacy()
        {
            var activeFile = await _db.Constitution.FirstOrDefaultAsync(c => c.IsActive);
            ViewBag.PdfUrl = activeFile?.FilePath ?? "/privacy/default.pdf";
            return View();
        }

     
        [Authorize(Roles = RoleList.GeneralAdmin)]
        [HttpGet]
        public async Task<IActionResult> UploadPolicy()
        {

            return View();
        }
     
        [HttpGet]

        public async Task<IActionResult> Vote()
        {
            // Fetch all candidates with their associated user data


            return View();
        }


        [Route("/NotFound")]
        public IActionResult PageNotFound()
        {
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Error(int? statusCode = null)
        {
            if (statusCode.HasValue)
            {
                if (statusCode.Value == 404)
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            return RedirectToAction(nameof(Index));
        }

    
    }
}
