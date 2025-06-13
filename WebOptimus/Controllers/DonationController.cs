using AutoMapper;
using WebOptimus.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebOptimus.Models;
using WebOptimus.Helpers;
using WebOptimus.Data;
using Stripe.Checkout;
using Stripe;
using UAParser;
using WebOptimus.StaticVariables;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using WebOptimus.Models.ViewModel;
using System.Globalization;
using System.Text;
using RotativaCore;
using WebOptimus.Models.Stripe;
using DocumentFormat.OpenXml.Spreadsheet;


namespace WebOptimus.Controllers
{
    public class DonationController : BaseController
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
        private readonly IPostmarkClient _postmark;
        private readonly StripeSettings _stripeSetting;
        private readonly IWebHostEnvironment _hostEnvironment;
        public DonationController(IMapper mapper, UserManager<User> userManager,
          SignInManager<User> signInManager, RoleManager<IdentityRole> roleManager, IWebHostEnvironment hostEnvironment, IOptions<StripeSettings> stripeSetting, IUserStore<User> userStore,
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
            _stripeSetting = stripeSetting.Value;
            _hostEnvironment = hostEnvironment;
        }

        //[HttpGet]
        //[Authorize]
        //public async Task<IActionResult> Index(string Id, CancellationToken ct)
        //{
        //    var currentUserEmail = HttpContext.Session.GetString("loginEmail");
        //    if (currentUserEmail == null)
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }

        //    var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

        //    try
        //    {
        //        DonorVM vm = new DonorVM();

        //        vm.PersonRegNumber = currentUser.PersonRegNumber;
        //        // Fetch the cause by CauseCampaignpRef
        //        var causes = await _db.Cause.FirstOrDefaultAsync(a => a.CauseCampaignpRef == Id);

        //        // Initialize default values
        //        decimal fullMemberAmount = 0;
        //        decimal underAgeAmount = 0;
        //        int underAgeLimit = 25;

        //        if (causes != null)
        //        {
        //            fullMemberAmount = causes.FullMemberAmount;
        //            underAgeAmount = causes.UnderAgeAmount;
        //            underAgeLimit = causes.UnderAge;
        //            vm.Cas = causes;
        //        }

        //        int eligibility = 0;
        //        int underAge = 0;
        //        decimal adultSum = 0;
        //        decimal childrenSum = 0;

        //        // Fetch dependents for the current user
        //        var dependents = await _db.Dependants
        //            .Where(d => d.UserId == currentUser.UserId)
        //            .ToListAsync();

        //        //Fetch group members for the current user
        //        var userDependent = await _db.Dependants.Where(a=>a.IsActive == true)
        //            .FirstOrDefaultAsync(d => d.PersonRegNumber == currentUser.PersonRegNumber);

        //        if (userDependent != null)
        //        {
        //            var group = await _db.GroupMembers
        //                .FirstOrDefaultAsync(gm => gm.PersonRegNumber == userDependent.PersonRegNumber && gm.Status == "Confirmed");

        //            if (group != null)
        //            {
        //                var groupMembers = await _db.GroupMembers
        //                    .Where(gm => gm.Status == "Confirmed" && gm.GroupId == group.GroupId)
        //                    .Join(_db.Dependants, gm => gm.PersonRegNumber, d => d.PersonRegNumber, (gm, d) => new
        //                    {
        //                        gm.GroupId,
        //                        d.Id,
        //                        d.PersonName,
        //                        d.PersonRegNumber,
        //                        d.PersonYearOfBirth,
        //                        d.UserId
        //                    })
        //                    .Where(gm => gm.UserId != currentUser.UserId) // Exclude the current user's dependents
        //                    .ToListAsync();

        //                var groupMemberChecklist = groupMembers.Select(gm => new DependentChecklistItem
        //                {
        //                    GroupId = gm.GroupId,
        //                    PersonRegNumber = gm.PersonName,
        //                    Name = gm.PersonName,
        //                    IsSelected = false,
        //                    Price = CalculateAge(gm.PersonYearOfBirth) >= underAgeLimit
        //                        ? fullMemberAmount
        //                        : underAgeAmount
        //                }).ToList();

        //                vm.GroupMembers.AddRange(groupMemberChecklist);
        //            }
        //        }

        //        // Get IDs of dependents who have already paid
        //        var paidDependentIds = await _db.Payment
        //            .Where(p => p.CauseCampaignpRef == Id && p.HasPaid)
        //            .Select(p => p.personRegNumber)
        //            .ToListAsync();

        //        // Populate all dependents, marking those who have been paid
        //        foreach (var d in dependents)
        //        {
        //            var checkAge = CalculateAge(d.PersonYearOfBirth.ToString());
        //            decimal price = checkAge >= underAgeLimit ? fullMemberAmount : underAgeAmount;

        //            eligibility += checkAge >= underAgeLimit ? 1 : 0;
        //            underAge += checkAge < underAgeLimit ? 1 : 0;

        //            vm.DependentsChecklist.Add(new DependentChecklistItem
        //            {
        //                PersonRegNumber = d.PersonRegNumber,
        //                Name = d.PersonName,
        //                IsSelected = false,
        //                Price = price,
        //                Paid = paidDependentIds.Contains(d.PersonRegNumber) // Mark as paid if it exists in the payment table
        //            });
        //        }

        //        // Populate all group members, marking those who have been paid
        //        foreach (var gm in vm.GroupMembers)
        //        {
        //            gm.Paid = paidDependentIds.Contains(gm.PersonRegNumber); // Mark as paid if it exists in the payment table
        //        }


        //        vm.User = await _db.Users.FirstOrDefaultAsync(u => u.PersonRegNumber == currentUser.PersonRegNumber && u.IsActive == true);
        //        adultSum = eligibility * fullMemberAmount;
        //        childrenSum = underAge * underAgeAmount;

        //        ViewBag.IsCause = causes != null ? "true" : "false";
        //        ViewBag.Adult = eligibility;
        //        ViewBag.Children = underAge;
        //        ViewBag.Total = adultSum + childrenSum;

        //        await RecordAuditAsync(vm.User, _requestIpHelper.GetRequestIp(), "Donation", "User navigated to donor page", ct);
        //        return View(vm);
        //    }
        //    catch (Exception ex)
        //    {
        //        await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Donation", $"Error: {ex.Message}", ct);
        //        TempData[SD.Error] = "Error - please contact Admin.";
        //        return RedirectToAction("Donation", "Home");
        //    }
        //}
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index(string Id, CancellationToken ct)
        {
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");
            if (currentUserEmail == null)
            {
                return RedirectToAction("Index", "Home");
            }
            int exemptCount = 0;
            decimal exemptTotal = 0;
            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
            try
            {
                DonorVM vm = new DonorVM
                {
                    PersonRegNumber = currentUser.PersonRegNumber
                };

                // Fetch cause details
                var causes = await _db.Cause.FirstOrDefaultAsync(a => a.CauseCampaignpRef == Id);
                decimal fullMemberAmount = 0;
                decimal underAgeAmount = 0;
                int underAgeLimit = 25;

                if (causes != null)
                {
                    fullMemberAmount = causes.FullMemberAmount;
                    underAgeAmount = causes.UnderAgeAmount;
                    underAgeLimit = causes.UnderAge;
                    vm.Cas = causes;
                }

                int eligibility = 0;
                int underAge = 0;
                decimal adultSum = 0;
                decimal childrenSum = 0;

                // Get the current user's dependents
                var dependents = await _db.Dependants
                    .Where(d => d.UserId == currentUser.UserId)
                    .ToListAsync();

                // Fetch the user's custom payment exemption for this specific cause
                var userCustomPayment = await _db.CustomPayment
                    .FirstOrDefaultAsync(cp => cp.PersonRegNumber == currentUser.PersonRegNumber && cp.CauseCampaignpRef == Id);
                var paidPersonRegNumbers = await _db.Payment
    .Where(p => p.CauseCampaignpRef == Id && p.HasPaid)
    .Select(p => p.personRegNumber)
    .ToListAsync();
                // Populate dependents with exemption check
                foreach (var d in dependents)
                {
                    var checkAge = CalculateAge(d.PersonYearOfBirth.ToString());
                    decimal price = checkAge >= underAgeLimit ? fullMemberAmount : underAgeAmount;

                    // Check if the dependent is exempt

                    bool isExempt = await _db.CustomPayment
        .AnyAsync(cp => cp.PersonRegNumber == d.PersonRegNumber && cp.CauseCampaignpRef == Id);

                    if (isExempt)
                    {
                        exemptCount++;
                        exemptTotal += price;  //  Subtract exempt person's price from total
                    }
                    eligibility += checkAge >= underAgeLimit ? 1 : 0;
                    underAge += checkAge < underAgeLimit ? 1 : 0;

                    vm.DependentsChecklist.Add(new DependentChecklistItem
                    {
                        PersonRegNumber = d.PersonRegNumber,
                        Name = d.PersonName,
                        IsSelected = false,
                        IsExempt = isExempt,
                       Price = price,
                        Paid = paidPersonRegNumbers.Contains(d.PersonRegNumber)

                    });
                }

                // Fetch the current user's group members
                var dependentRegNumbers = dependents.Select(d => d.PersonRegNumber).ToList();
                var userDependent = await _db.Dependants
     .FirstOrDefaultAsync(d => d.PersonRegNumber == currentUser.PersonRegNumber && d.IsActive == true);

                if (userDependent != null)
                {
                    // Get all confirmed groups this user is part of
                    var userGroups = await _db.GroupMembers
                        .Where(gm => gm.PersonRegNumber == userDependent.PersonRegNumber && gm.Status == "Confirmed")
                        .Select(gm => gm.GroupId)
                        .ToListAsync();

                    if (userGroups.Any())
                    {
                        // Get all confirmed group members from those groups
                        var allGroupMembers = await _db.GroupMembers
                            .Where(gm => userGroups.Contains(gm.GroupId) && gm.Status == "Confirmed")
                            .Select(gm => gm.PersonRegNumber)
                            .Distinct()
                            .ToListAsync();

                        // Optionally exclude the user's own dependents
                        var excludeList = dependents.Select(d => d.PersonRegNumber).ToList();

                        var members = await _db.Dependants
                            .Where(d => allGroupMembers.Contains(d.PersonRegNumber) && !excludeList.Contains(d.PersonRegNumber))
                            .ToListAsync();

                        foreach (var gm in members)
                        {
                            var checkAge = CalculateAge(gm.PersonYearOfBirth.ToString());
                            decimal price = checkAge >= underAgeLimit ? fullMemberAmount : underAgeAmount;

                            bool isExempt = await _db.CustomPayment
                                .AnyAsync(cp => cp.PersonRegNumber == gm.PersonRegNumber && cp.CauseCampaignpRef == Id);

                            vm.GroupMembers.Add(new DependentChecklistItem
                            {
                                GroupId = userGroups.First(), // Or gm.GroupId if needed
                                PersonRegNumber = gm.PersonRegNumber,
                                Name = gm.PersonName,
                                IsSelected = false,
                                IsExempt = isExempt,
                                Price = price,
                                Paid = paidPersonRegNumbers.Contains(gm.PersonRegNumber)
                            });
                        }
                    }
                }


                vm.User = await _db.Users.FirstOrDefaultAsync(u => u.PersonRegNumber == currentUser.PersonRegNumber && u.IsActive);

                adultSum = eligibility * fullMemberAmount;
                childrenSum = underAge * underAgeAmount;
                decimal totalAmount = (eligibility * fullMemberAmount) + (underAge * underAgeAmount) - exemptTotal;

                ViewBag.IsCause = causes != null ? "true" : "false";
                ViewBag.Adult = eligibility;
                ViewBag.Children = underAge;
                ViewBag.Total = totalAmount;  //  Total now correctly reflects exempt members
                ViewBag.ExemptCount = exemptCount;

                await RecordAuditAsync(vm.User, _requestIpHelper.GetRequestIp(), "Donation", "User navigated to donor page", ct);
                return View(vm);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Donation", $"Error: {ex.Message}", ct);
                TempData[SD.Error] = "Error - please contact Admin.";
                return RedirectToAction("Donation", "Home");
            }
        }

        private decimal CalculateDependentPrice(Dependant dependent, Cause cause)
        {
            int age = CalculateAge(dependent.PersonYearOfBirth);
            return age >= cause.UnderAge ? cause.FullMemberAmount : cause.UnderAgeAmount;
        }

        //[HttpGet]
        //[Authorize]
        //public async Task<IActionResult> Index(string Id, CancellationToken ct)
        //{
        //    var currentUserEmail = HttpContext.Session.GetString("loginEmail");
        //    if (currentUserEmail == null)
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }
        //    var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

        //    try
        //    {

        //        DonorVM vm = new DonorVM();

        //        var causes = await _db.Cause.FirstOrDefaultAsync(a => a.CauseCampaignpRef == Id);
        //        if (causes != null)
        //        {
        //            int eligibility = 0;
        //            int underAge = 0;
        //            decimal AdultSum = 0;
        //            decimal ChildrenSum = 0;

        //            // Get all dependents for the cause's user
        //            var dependents = await _db.Dependants
        //                .Where(a => a.UserId == currentUser.UserId).ToListAsync();



        //            // Get IDs of dependents who have already paid
        //            var paidDependentIds = await _db.Payment
        //                .Where(p => p.CauseCampaignpRef == Id && p.HasPaid)
        //                .Select(p => p.DependentId)
        //                .ToListAsync();

        //            // Filter out dependents who have already paid
        //            var unpaidDependents = dependents
        //                .Where(d => !paidDependentIds.Contains(d.Id))
        //                .ToList();

        //            foreach (var d in unpaidDependents)
        //            {
        //                var checkAge = CalculateAge(d.PersonYearOfBirth.ToString());
        //                decimal price;

        //                if (checkAge >= causes.UnderAge)
        //                {
        //                    eligibility++;
        //                    price = causes.FullMemberAmount;
        //                }
        //                else
        //                {
        //                    underAge++;
        //                    price = causes.UnderAgeAmount;
        //                }

        //                vm.DependentsChecklist.Add(new DependentChecklistItem
        //                {
        //                    DependentId = d.Id,
        //                    Name = d.PersonName,
        //                    IsSelected = false, // default value
        //                    Price = price
        //                });
        //            }

        //            vm.Cas = causes;
        //            vm.User = await _db.Users.FirstOrDefaultAsync(a => a.UserId == currentUser.UserId);


        //            AdultSum = eligibility * causes.FullMemberAmount;
        //            ChildrenSum = underAge * causes.UnderAgeAmount;

        //            ViewBag.IsCause = "true";
        //            ViewBag.Adult = eligibility;
        //            ViewBag.Children = underAge;
        //            ViewBag.Total = AdultSum + ChildrenSum;
        //        }
        //        else
        //        {
        //            ViewBag.IsCause = "false";
        //        }

        //        await RecordAuditAsync(vm.User, _requestIpHelper.GetRequestIp(), "Donation", "user navigated to donor page", ct);
        //        return View(vm);
        //    }
        //    catch (Exception ex)
        //    {
        //        await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Donation", "user navigated to donor page" + ex.Message.ToString(), ct);

        //        TempData[SD.Error] = "Error - please contact Admin.";
        //        return RedirectToAction("Donation", "Home");
        //    }
        //}
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Payment(DonorVM _data, CancellationToken ct)
        {
            try
            {
                // Validate session user
                var currentUserEmail = HttpContext.Session.GetString("loginEmail");
                if (currentUserEmail == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
                if (currentUser == null)
                {
                    ModelState.AddModelError(string.Empty, "User not found.");
                    return View("Index", _data);
                }

                // Validate selected items
                var selectedItems = (_data.DependentsChecklist ?? new List<DependentChecklistItem>())
                 .Where(d => d.IsSelected && d.CustomAmount.GetValueOrDefault() >= d.Price)
                 .ToList();

                selectedItems.AddRange((_data.GroupMembers ?? new List<DependentChecklistItem>())
                    .Where(g => g.IsSelected && g.CustomAmount.GetValueOrDefault() >= g.Price));

                await RecordAuditAsync("Selected Items", "Payment", $"Total Selected Items: {selectedItems.Count()}");

                // Debugging - Log each selected item
                foreach (var item in selectedItems)
                {
                    await RecordAuditAsync("Selected Item", "Payment", $"Processing: {item.PersonRegNumber} (Selected: {item.IsSelected})");
                }

                await RecordAuditAsync("SelectedDep", "Selected", $"Total Dependent)d: {selectedItems.Count()}");

                // Prepare Stripe session
                StripeConfiguration.ApiKey = _stripeSetting.SecretKey;
                var totalAmountInCents = Convert.ToInt32(Math.Round(_data.TotalAmount * 100));
                var ourRef = $"{_data.CauseCampaignpRef}{new Random().Next(0, 9999999):D5}";

                var options = new SessionCreateOptions
                {
                PaymentMethodTypes = new List<string>
                {
                    "card"                   
                },
                    LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "gbp",
                        UnitAmount = totalAmountInCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Umoja Wetu",
                            Description = $"Payment ref#: {ourRef}"
                        }
                    },
                    Quantity = 1
                }
            },
                    Mode = "payment",
                    SuccessUrl = Url.Action(nameof(DonationConfirmation), "Donation", null, Request.Scheme) + "?session_id={CHECKOUT_SESSION_ID}",
                    CancelUrl = Url.Action("Index", "Home", null, Request.Scheme)
                };

                var service = new SessionService();
                var session = service.Create(options);

                // Save the payment session
                var paymentSession = new PaymentSession
                {
                    SessionId = session.Id,
                    UserId = currentUser.UserId,
                    Email = currentUserEmail,
                    Amount = _data.TotalAmount,
                    personRegNumber = _data.PersonRegNumber,
                    DependentId = currentUser.DependentId,
                    CauseCampaignpRef = _data.CauseCampaignpRef,
                    TransactionFees = _data.TransactionFees,
                    TotalAmount = _data.TotalAmount,
                    IsPaid = false,
                    OurRef = ourRef,
                    Reason = _data.Reason,
                    DateCreated = DateTime.UtcNow,
                };

                _db.PaymentSessions.Add(paymentSession);
                await _db.SaveChangesAsync(ct);
                // Save selected dependents in DependentChecklistItems and create payment records
                // Save selected dependents in DependentChecklistItems and create payment records
                foreach (var item in selectedItems)
                {
                    if (item.CustomAmount.GetValueOrDefault() <= 0)
                        continue; // Skip invalid custom amount

                    if (!item.IsSelected)
                    {
                        continue;
                    }

                    // **Check if the person is a dependent**
                    var dependentDetails = await _db.Dependants
         .FirstOrDefaultAsync(d => d.PersonRegNumber == item.PersonRegNumber && d.IsActive == true);

                    var groupMemberDetails = await _db.GroupMembers
                        .FirstOrDefaultAsync(gm => gm.PersonRegNumber == item.PersonRegNumber && gm.Status == "Confirmed");

                    if (dependentDetails == null && groupMemberDetails == null)
                    {
                        await RecordAuditAsync("ValidationError", "SaveDependentItems",
                            $"Person details not found for {item.PersonRegNumber}");
                        continue;
                    }

                    

                    var checklistItem = new DependentChecklistItem
                    {
                        DependentId = dependentDetails?.Id ?? (groupMemberDetails != null ? 0 : 0),   // Use 0 if null
                        PersonRegNumber = item.PersonRegNumber,
                        UserId = dependentDetails.UserId,
                        Name = dependentDetails.PersonName,                         
                        CauseCampaignpRef = _data.CauseCampaignpRef,
                        Price = item.Price,
                        CustomAmount = item.CustomAmount.GetValueOrDefault(),
                        IsSelected = true,
                        PaymentSessionId = paymentSession.Id,
                        SessionId = session.Id,
                    };

                    await RecordAuditAsync("SelectedDep", "Selected", $"Processed: {checklistItem.PersonRegNumber}");
                    _db.DependentChecklistItems.Add(checklistItem);
                    await _db.SaveChangesAsync(ct);
                }


                // Redirect to Stripe checkout
                return Redirect(session.Url);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync("Failed Payment", "FailPayment", $"Payment failed because of: {ex.Message}", ct);
                return Json(new { success = false, message = "Payment failed." });
            }
        }


        //[HttpGet]
        //public async Task<IActionResult> DonationConfirmation(string session_id, CancellationToken ct)
        //{
        //    try
        //    {
        //        // await RecordAuditAsync("Payment session", "Session", $"Session ID: {session_id}", ct);
        //        StripeConfiguration.ApiKey = _stripeSetting.SecretKey;
        //        var service = new SessionService();
        //        var session = service.Get(session_id);

        //        if (session.PaymentStatus == "paid")
        //        {
        //            var paymentSession = await _db.PaymentSessions
        //                .FirstOrDefaultAsync(ps => ps.SessionId == session_id, ct);

        //            if (paymentSession == null)
        //            {
        //                await RecordAuditAsync("Invalid Payment Session", "FailPayment", $"Session ID {session_id} not found.", ct);
        //                return RedirectToAction("Index", "Home");
        //            }

        //            var paymentItems = await _db.DependentChecklistItems
        //                .Where(d => d.SessionId == session_id && d.IsSelected)
        //                .ToListAsync(ct);

        //            foreach (var item in paymentItems)
        //            {
        //                var existingPayment = await _db.Payment
        //                .AnyAsync(p => p.OurRef == paymentSession.OurRef && p.personRegNumber == item.PersonRegNumber, ct);
        //                if (existingPayment)
        //                {
        //                    continue; // Skip if payment already exists
        //                }

        //                var goodwill = (item.CustomAmount.GetValueOrDefault() > item.Price)
        //                    ? item.CustomAmount.GetValueOrDefault() - item.Price
        //                    : 0;

        //                var paymentRecord = new Payment
        //                {
        //                    UserId = item.UserId,
        //                    DependentId = (item.DependentId != 0 ? item.DependentId : 0),
        //                    personRegNumber = item.PersonRegNumber,
        //                    Amount = item.Price,
        //                    GoodwillAmount = goodwill,
        //                    CauseCampaignpRef = paymentSession.CauseCampaignpRef,
        //                    HasPaid = true,
        //                    Notes = paymentSession.Reason,
        //                    DateCreated = DateTime.UtcNow,
        //                    CreatedBy = paymentSession.Email,
        //                    OurRef = paymentSession.OurRef
        //                };

        //                _db.Payment.Add(paymentRecord);
        //                await _db.SaveChangesAsync(ct);

        //            }

        //            paymentSession.IsPaid = true;
        //            _db.PaymentSessions.Update(paymentSession);

        //            await _db.SaveChangesAsync(ct);
        //            ViewBag.Ref = paymentSession.OurRef;
        //            return View("DonationConfirmation");
        //        }

        //        return RedirectToAction("Index", "Home");
        //    }
        //    catch (Exception ex)
        //    {
        //        await RecordAuditAsync("Failed Payment", "FailPayment", $"Payment failed because of: {ex.Message}", ct);
        //        return RedirectToAction("Index", "Home");
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> DonationConfirmation(string session_id, CancellationToken ct)
        {
            try
            {
                StripeConfiguration.ApiKey = _stripeSetting.SecretKey;
                var service = new SessionService();
                Session session = null;

                int maxRetries = 5; // Maximum number of retries
                int delayMilliseconds = 2000; //  Start with a 2-second delay

                // Retry loop for fetching Stripe session
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        session = service.Get(session_id);
                        if (session != null) break; //  Exit retry loop if successful
                    }
                    catch (Exception ex)
                    {
                        await RecordAuditAsync("Stripe Timeout", "Retry",
                            $"Attempt {attempt}: Stripe session fetch failed - {ex.Message}", ct);
                    }

                    await Task.Delay(delayMilliseconds, ct); // Wait before retrying
                    delayMilliseconds *= 2; // Exponential backoff (2s, 4s, 8s)
                }

                // Check if session was retrieved successfully
                if (session == null)
                {
                    await RecordAuditAsync("Failed Payment", "FailPayment",
                        $"Failed to fetch Stripe session after {maxRetries} attempts.", ct);
                    return RedirectToAction("Index", "Home");
                }

                if (session.PaymentStatus == "paid" || session.Status == "complete")
                {
                    var paymentSession = await _db.PaymentSessions
                        .FirstOrDefaultAsync(ps => ps.SessionId == session_id, ct);

                    if (paymentSession == null)
                    {
                        await RecordAuditAsync("Invalid Payment Session", "FailPayment",
                            $"Session ID {session_id} not found.", ct);
                        return RedirectToAction("Index", "Home");
                    }
                    if (paymentSession.IsPaid)
                    {
                        TempData["Success"] = "Payment for this session has already been processed.";
                        return RedirectToAction("Index", "Home");
                    }
                    var paymentItems = await _db.DependentChecklistItems
                       .Where(d => d.SessionId == session_id && d.IsSelected)
                       .ToListAsync(ct);
                    foreach (var item in paymentItems)
                    {
                        var existingPayment = await _db.Payment
                            .AnyAsync(p => p.OurRef == paymentSession.OurRef && p.personRegNumber == item.PersonRegNumber, ct);

                        if (existingPayment)
                        {
                            continue; // Skip if payment already exists
                        }

                        var goodwill = (item.CustomAmount.GetValueOrDefault() > item.Price)
                            ? item.CustomAmount.GetValueOrDefault() - item.Price
                            : 0;

                        var paymentRecord = new Payment
                        {
                            UserId = item.UserId,
                            DependentId = (item.DependentId != 0 ? item.DependentId : 0),
                            personRegNumber = item.PersonRegNumber,
                            Amount = item.Price,
                            GoodwillAmount = goodwill,
                            CauseCampaignpRef = paymentSession.CauseCampaignpRef,
                            HasPaid = true,
                            Notes = paymentSession.Reason,
                            DateCreated = DateTime.UtcNow,
                            CreatedBy = paymentSession.Email,
                            OurRef = paymentSession.OurRef
                        };

                        _db.Payment.Add(paymentRecord);
                        await _db.SaveChangesAsync(ct);
                    }


                    var paymentIntentId = session.PaymentIntentId;
                    decimal stripeFee = 0;
                    decimal netAmount = 0;
                    decimal actualAmountReceived = paymentSession.TotalAmount;

                    if (!string.IsNullOrEmpty(paymentIntentId))
                    {
                        var chargeService = new ChargeService();
                        var chargeList = await chargeService.ListAsync(new ChargeListOptions { PaymentIntent = paymentIntentId });

                        var charge = chargeList.Data.FirstOrDefault();
                        if (charge != null && !string.IsNullOrEmpty(charge.BalanceTransactionId))
                        {
                            var balanceTransactionService = new BalanceTransactionService();
                            var balanceTransaction = await balanceTransactionService.GetAsync(charge.BalanceTransactionId);

                            if (balanceTransaction != null)
                            {
                                stripeFee = balanceTransaction.Fee / 100m; // Convert from cents to GBP
                                netAmount = actualAmountReceived - stripeFee;
                            }
                        }
                    }

                    //  Ensure `paymentSession` updates are actually saved
                    paymentSession.StripeActualFees = stripeFee;
                    paymentSession.StripeNetAmount = netAmount;
                    paymentSession.IsPaid = true;
                    _db.PaymentSessions.Update(paymentSession);
                    await _db.SaveChangesAsync(ct); //  Ensure this save executes 

                    ViewBag.Ref = paymentSession.OurRef;
                    return View("DonationConfirmation");
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                await RecordAuditAsync("Failed Payment", "FailPayment",
                    $"Payment failed because of: {ex.Message}", ct);
                return RedirectToAction("Index", "Home");
            }
        }


        public decimal CalculateGoodwill(decimal totalAmount, decimal amount, decimal transactionFees)
        {
            decimal goodwill = totalAmount - amount - transactionFees;

            // Ensure goodwill is never negative
            return Math.Max(0, goodwill);
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> PaymentHistory(CancellationToken ct)
        {
            try
            {
                var currentUserEmail = HttpContext.Session.GetString("loginEmail");

                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    return RedirectToAction("Login", "Account");
                }

                var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Fetch death-related payments for the logged-in user
                var payments = await _db.Payment
              .Where(p => p.UserId == currentUser.UserId)
              .OrderByDescending(p => p.DateCreated) // Order by date
              .ToListAsync(ct);


                        // Fetch non-death-related donations from `OtherDonationPayment`
                        var otherDonations = await _db.OtherDonationPayment
             .Where(p => p.UserId == currentUser.UserId && p.PersonRegNumber == currentUser.PersonRegNumber)
             .OrderByDescending(p => p.DateCreated) // Order by date
             .ToListAsync(ct);


                // Fetch dependents for the logged-in user's family
                var dependents = await _db.Dependants
                    .Where(d => d.UserId == currentUser.UserId)
                    .ToListAsync(ct);

                // Fetch all closed causes (death-related)
                var closedCauses = await _db.Cause
                 .Where(c => c.IsClosed && !c.IsActive)
                 .ToListAsync(ct);


                // Calculate unpaid dependents for each cause
                decimal totalMissedPayments = 0m;
                var missedPayments = new List<PaymentHistoryViewModel>();

                foreach (var cause in closedCauses)
                {
                    if (cause.IsClosedDate <= currentUser.DateCreated)
                    {
                        continue; // Skip causes closed before the user joined
                    }

                    // Filter dependents who were eligible but haven't paid
                    var unpaidDependents = dependents
                        .Where(d =>
                            !payments.Any(p => p.personRegNumber == d.PersonRegNumber && p.CauseCampaignpRef == cause.CauseCampaignpRef) &&
                            CalculateAgeAtYear(d.PersonYearOfBirth.ToString(), cause.DateCreated) >= 25 &&
                            d.DateCreated <= cause.IsClosedDate
                        )
                        .ToList();

                    foreach (var dependent in unpaidDependents)
                    {
                        var missedAmount = cause.FullMemberAmount;
                        totalMissedPayments += missedAmount;

                        missedPayments.Add(new PaymentHistoryViewModel
                        {
                            DependentName = dependent.PersonName,
                            YearOfBirth = dependent.PersonYearOfBirth,
                            RegNumber = dependent.PersonRegNumber,
                            CauseCampaignpRef = cause.CauseCampaignpRef,
                            Amount = missedAmount,
                            EndDate = cause.EndDate
                        });
                    }
                }

                // Calculate total contributions for death-related payments
                var userPayments = payments.Where(p => p.personRegNumber == currentUser.PersonRegNumber).ToList();
                var totalContribution = userPayments.Sum(p => p.Amount);
                var totalGoodwill = userPayments.Sum(p => p.GoodwillAmount);

                // Calculate total contributions for non-death-related donations
                var totalOtherDonations = otherDonations.Sum(p => p.Amount);

                // Pass total contributions to the view
                ViewBag.TotalContribution = totalContribution + totalGoodwill + totalOtherDonations;
                ViewBag.TotalGoodwill = totalGoodwill;
                ViewBag.TotalMissedPayments = totalMissedPayments;
                ViewBag.MissedPayments = missedPayments;

                // Prepare payment history details for death-related donations
                var paymentDetails = userPayments
                .Select(p => new PaymentHistoryViewModel
                {
                    Id = p.Id,
                    CauseCampaignpRef = p.CauseCampaignpRef,
                    Amount = p.Amount,
                    DateCreated = p.DateCreated,
                    DependentId = p.DependentId,
                    PersonRegNumber = p.personRegNumber,
                    DependentName = dependents.FirstOrDefault(d => d.PersonRegNumber == p.personRegNumber && d.IsActive == true)?.PersonName ?? $"{currentUser.FirstName} {currentUser.Surname}",
                    OurRef = p.OurRef,
                    GoodwillAmount = p.GoodwillAmount,
                    EndDate = closedCauses.FirstOrDefault(c => c.CauseCampaignpRef == p.CauseCampaignpRef)?.IsClosedDate ?? DateTime.MinValue
                })
                .OrderByDescending(p => p.DateCreated) // Ensure sorting here
                .ToList();


                // Prepare payment history details for non-death-related donations
                var otherDonationDetails = otherDonations
     .GroupBy(p => p.OurRef) // Assuming OurRef is unique per donation
     .Select(g => g.First()) // Get one entry per unique donation
     .Select(p => new PaymentHistoryViewModel
     {
         Id = p.Id,
         CauseCampaignpRef = p.CauseCampaignpRef,
         Amount = p.Amount,
         DateCreated = p.DateCreated,
         PersonRegNumber = p.PersonRegNumber,
         DependentId = p.DependentId,
         DependentName = dependents.FirstOrDefault(d => d.PersonRegNumber == p.PersonRegNumber && d.IsActive == true)?.PersonName
                          ?? $"{currentUser.FirstName} {currentUser.Surname}",
         OurRef = p.OurRef,
         GoodwillAmount = 0,
         EndDate = DateTime.MinValue
     }).ToList();


                ViewBag.OtherDonationPayments = otherDonationDetails;

                return View(paymentDetails);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync("Payment History Error", "PaymentHistory", $"Error fetching payment history: {ex.Message}", ct);
                return RedirectToAction("Error", "Home");
            }
        }


  

        private int CalculateAgeAtYear(string personYearOfBirth, DateTime startDate)
        {
            if (int.TryParse(personYearOfBirth, out var birthYear))
            {
                return startDate.Year - birthYear;
            }
            return 0; // Return 0 if parsing fails
        }

        [Authorize]
        public async Task<IActionResult> PaymentDetails(string paymentId, CancellationToken ct)
        {
            try
            {
                // Fetch all payment sessions for the given paymentId where IsPaid is true
                var paymentSessions = await _db.PaymentSessions
                    .Where(ps => ps.OurRef == paymentId && ps.IsPaid == true)
                    .ToListAsync(ct);

                if (!paymentSessions.Any())
                {
                    TempData["Error"] = "No payment sessions found. Please contact admin.";
                    return RedirectToAction("PaymentHistory");
                }

                // Get the CauseCampaignpRef from the session
                var causeCampaignRefs = paymentSessions.Select(ps => ps.CauseCampaignpRef).Distinct().ToList();

                // Fetch death-related payments
                var payments = await _db.Payment
                    .Where(p => p.OurRef == paymentId)
                    .ToListAsync(ct);

                // Fetch non-death-related contributions from OtherDonationPayment
                var otherDonations = await _db.OtherDonationPayment
                    .Where(p => causeCampaignRefs.Contains(p.CauseCampaignpRef))
                    .ToListAsync(ct);

                // Fetch dependent checklist items for death-related payments
                var paymentSessionIds = paymentSessions.Select(ps => ps.Id).ToList();
                var paymentItems = await _db.DependentChecklistItems
                    .Where(d => paymentSessionIds.Contains(d.PaymentSessionId))
                    .ToListAsync(ct);

                // Determine payment type: Death-related or Non-Death-related
                bool hasDeathRelatedPayments = payments.Any();
                bool hasNonDeathPayments = otherDonations.Any();

                if (!hasDeathRelatedPayments && !hasNonDeathPayments)
                {
                    TempData["Error"] = "No payment details found.";
                    return RedirectToAction("PaymentHistory");
                }

                // Prepare the view model
                var model = new PaymentHistoryDetailsViewModel
                {
                    PaymentSessions = paymentSessions,
                    PaymentItems = hasDeathRelatedPayments ? paymentItems : new List<DependentChecklistItem>(),
                    Payments = hasDeathRelatedPayments ? payments : new List<Payment>(),
                    OtherDonationPayments = hasNonDeathPayments ? otherDonations : new List<OtherDonationPayment>()
                };

                // Calculate total amounts
                decimal totalAmountPaid = paymentSessions.Sum(ps => ps.TotalAmount);
                decimal totalTransactionFees = paymentSessions.Sum(ps => ps.TransactionFees);
                decimal totalContribution = paymentItems.Sum(d => d.Price);  // Contributions from dependents
                decimal totalGoodwill = payments.Sum(p => p.GoodwillAmount); // Goodwill from death-related payments
                decimal totalOtherDonations = otherDonations.Sum(p => p.Amount); // Non-death donations
              
                //  Convert nullable decimal (decimal?) to decimal using `.GetValueOrDefault(0)`
                decimal firstMissedPayment = paymentItems
                    .Where(d => d.MissedPayment.HasValue && d.MissedPayment > 0)
                    .Select(d => d.MissedPayment.GetValueOrDefault(0)) // Convert nullable to decimal safely
                    .FirstOrDefault();
                decimal expectedTotal = totalContribution + totalGoodwill + totalOtherDonations + totalTransactionFees;

                //  If there’s a missed payment, use that instead of calculating late fees manually
                decimal latePaymentFees = firstMissedPayment > 0 ? firstMissedPayment :
                    (totalAmountPaid > expectedTotal ? (totalAmountPaid - expectedTotal) : 0);


                // Store values in ViewBag for use in the view
                ViewBag.TotalAmountPaid = totalAmountPaid;
                ViewBag.TransactionFees = totalTransactionFees;
                ViewBag.NetReceived = totalAmountPaid - totalTransactionFees;
                ViewBag.TotalContribution = totalContribution + totalGoodwill + totalOtherDonations;
                ViewBag.TotalGoodwill = totalGoodwill;
                ViewBag.LatePaymentFees = latePaymentFees; // This is what we need!

                // Set ViewBag flags to help the View show only relevant sections
                ViewBag.HasDeathRelatedPayments = hasDeathRelatedPayments;
                ViewBag.HasNonDeathPayments = hasNonDeathPayments;

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while processing your request. Please try again later.";
                return RedirectToAction("PaymentHistory");
            }
        }

        //[HttpGet]
        //[Authorize]
        //public async Task<IActionResult> PaymentDetails(string paymentId, CancellationToken ct)
        //{
        //    try
        //    {
        //        // Fetch all payment sessions for the given paymentId where IsPaid is true
        //        var paymentSessions = await _db.PaymentSessions
        //            .Where(ps => ps.OurRef == paymentId && ps.IsPaid == true)
        //            .ToListAsync(ct);

        //        if (!paymentSessions.Any())
        //        {
        //            TempData["Error"] = "No payment sessions found. Please contact admin.";
        //            return RedirectToAction("PaymentHistory");
        //        }

        //        // Get the CauseCampaignpRef from the session
        //        var causeCampaignRefs = paymentSessions.Select(ps => ps.CauseCampaignpRef).Distinct().ToList();

        //        // Fetch death-related payments
        //        var payments = await _db.Payment
        //            .Where(p => p.OurRef == paymentId)
        //            .ToListAsync(ct);

        //        // Fetch non-death-related contributions from OtherDonationPayment
        //        var otherDonations = await _db.OtherDonationPayment
        //            .Where(p => causeCampaignRefs.Contains(p.CauseCampaignpRef))
        //            .ToListAsync(ct);

        //        // Fetch dependent checklist items for death-related payments
        //        var paymentSessionIds = paymentSessions.Select(ps => ps.Id).ToList();
        //        var paymentItems = await _db.DependentChecklistItems
        //            .Where(d => paymentSessionIds.Contains(d.PaymentSessionId))
        //            .ToListAsync(ct);

        //        // Determine payment type: Death-related or Non-Death-related
        //        bool hasDeathRelatedPayments = payments.Any();
        //        bool hasNonDeathPayments = otherDonations.Any();

        //        if (!hasDeathRelatedPayments && !hasNonDeathPayments)
        //        {
        //            TempData["Error"] = "No payment details found.";
        //            return RedirectToAction("PaymentHistory");
        //        }

        //        // Prepare the view model
        //        var model = new PaymentHistoryDetailsViewModel
        //        {
        //            PaymentSessions = paymentSessions,
        //            PaymentItems = hasDeathRelatedPayments ? paymentItems : new List<DependentChecklistItem>(),
        //            Payments = hasDeathRelatedPayments ? payments : new List<Payment>(),
        //            OtherDonationPayments = hasNonDeathPayments ? otherDonations : new List<OtherDonationPayment>()
        //        };

        //        // Calculate total amounts separately for each type
        //        ViewBag.TotalAmountPaid = paymentSessions.Sum(ps => ps.TotalAmount);


        //        //  FIXED: Now correctly summing Transaction Fees for non-death payments
        //        ViewBag.TransactionFees = paymentSessions.Sum(ps => ps.TransactionFees);

        //        ViewBag.NetReceived = ViewBag.TotalAmountPaid - ViewBag.TransactionFees;

        //        // Set ViewBag flags to help the View show only relevant sections
        //        ViewBag.HasDeathRelatedPayments = hasDeathRelatedPayments;
        //        ViewBag.HasNonDeathPayments = hasNonDeathPayments;

        //        return View(model);
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["Error"] = "An error occurred while processing your request. Please try again later.";
        //        return RedirectToAction("PaymentHistory");
        //    }
        //}

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


     
        public IActionResult DownloadPaymentDetails(string paymentId)
        {
            
            var pdf = new ActionAsPdf("PaymentPDF", new { paymentId = paymentId })
            {
               
                FileName = "PaymentInvoice.pdf"
            };

            return pdf;
        }



        [HttpGet]
        public async Task<IActionResult> PaymentPDF(string paymentId, CancellationToken ct)
        {
            try
            {
                // Fetch all payment sessions for the given paymentId where IsPaid is true
                var paymentSessions = await _db.PaymentSessions
                    .Where(ps => ps.OurRef == paymentId && ps.IsPaid == true)
                    .ToListAsync(ct);

                if (!paymentSessions.Any())
                {
                    TempData["Error"] = "No payment sessions found. Please contact admin.";
                    return RedirectToAction("PaymentHistory");
                }

                // Get the CauseCampaignpRef from the session
                var causeCampaignRefs = paymentSessions.Select(ps => ps.CauseCampaignpRef).Distinct().ToList();

                // Fetch death-related payments
                var payments = await _db.Payment
                    .Where(p => p.OurRef == paymentId)
                    .ToListAsync(ct);

                // Fetch non-death-related contributions from OtherDonationPayment
                var otherDonations = await _db.OtherDonationPayment
                    .Where(p => causeCampaignRefs.Contains(p.CauseCampaignpRef))
                    .ToListAsync(ct);

                // Fetch dependent checklist items for death-related payments
                var paymentSessionIds = paymentSessions.Select(ps => ps.Id).ToList();
                var paymentItems = await _db.DependentChecklistItems
                    .Where(d => paymentSessionIds.Contains(d.PaymentSessionId))
                    .ToListAsync(ct);

                // Determine payment type: Death-related or Non-Death-related
                bool hasDeathRelatedPayments = payments.Any();
                bool hasNonDeathPayments = otherDonations.Any();

                if (!hasDeathRelatedPayments && !hasNonDeathPayments)
                {
                    TempData["Error"] = "No payment details found.";
                    return RedirectToAction("PaymentHistory");
                }

                // Prepare the view model
                var model = new PaymentHistoryDetailsViewModel
                {
                    PaymentSessions = paymentSessions,
                    PaymentItems = hasDeathRelatedPayments ? paymentItems : new List<DependentChecklistItem>(),
                    Payments = hasDeathRelatedPayments ? payments : new List<Payment>(),
                    OtherDonationPayments = hasNonDeathPayments ? otherDonations : new List<OtherDonationPayment>()
                };

                // Calculate total amounts
                decimal totalAmountPaid = paymentSessions.Sum(ps => ps.TotalAmount);
                decimal totalTransactionFees = paymentSessions.Sum(ps => ps.TransactionFees);
                decimal totalContribution = paymentItems.Sum(d => d.Price);  // Contributions from dependents
                decimal totalGoodwill = payments.Sum(p => p.GoodwillAmount); // Goodwill from death-related payments
                decimal totalOtherDonations = otherDonations.Sum(p => p.Amount); // Non-death donations

          
                // Convert nullable decimal (decimal?) to decimal using `.GetValueOrDefault(0)`
                decimal firstMissedPayment = paymentItems
                    .Where(d => d.MissedPayment.HasValue && d.MissedPayment > 0)
                    .Select(d => d.MissedPayment.GetValueOrDefault(0)) // Convert nullable to decimal safely
                    .FirstOrDefault();
                decimal expectedTotal = totalContribution + totalGoodwill + totalOtherDonations + totalTransactionFees;

                //  If there’s a missed payment, use that instead of calculating late fees manually
                decimal latePaymentFees = firstMissedPayment > 0 ? firstMissedPayment :
                    (totalAmountPaid > expectedTotal ? (totalAmountPaid - expectedTotal) : 0);

                // Store values in ViewBag for use in the view
                ViewBag.TotalAmountPaid = totalAmountPaid;
                ViewBag.TransactionFees = totalTransactionFees;
                ViewBag.NetReceived = totalAmountPaid - totalTransactionFees;
                ViewBag.TotalContribution = totalContribution + totalGoodwill + totalOtherDonations;
                ViewBag.TotalGoodwill = totalGoodwill;
                ViewBag.LatePaymentFees = latePaymentFees; // This is what we need!

                // Set ViewBag flags to help the View show only relevant sections
                ViewBag.HasDeathRelatedPayments = hasDeathRelatedPayments;
                ViewBag.HasNonDeathPayments = hasNonDeathPayments;

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while processing your request. Please try again later.";
                return RedirectToAction("PaymentHistory");
            }
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





        [HttpGet]
        [Authorize]
        public async Task<IActionResult> History(string exempt, string notPaid)
        {
            try
            {
                var currentUserEmail = HttpContext.Session.GetString("loginEmail");
                if (currentUserEmail == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

                // Join Payment with PaymentSession using OurRef
                var history = await (from payment in _db.Payment
                                     join paymentSession in _db.PaymentSessions
                                     on payment.OurRef equals paymentSession.OurRef
                                     where payment.UserId == currentUser.UserId
                                     select new PaymentViewModel
                                     {
                                         Id = payment.Id,
                                         UserId = payment.UserId,
                                         DependentId = payment.DependentId,
                                         PersonRegNumber = payment.personRegNumber,
                                         CauseCampaignpRef = payment.CauseCampaignpRef,
                                         Amount = payment.Amount, // Assuming amount is in cents
                                         TransactionFees = paymentSession.TransactionFees,
                                         TotalAmount = paymentSession.TotalAmount, // Assuming total amount is in cents
                                         HasPaid = payment.HasPaid,
                                         OurRef = payment.OurRef,
                                         Notes = payment.Notes,
                                         DateCreated = payment.DateCreated,
                                         CreatedBy = payment.CreatedBy,
                                         DateRaised = payment.DateCreated.ToString("dd/MM/yyyy h:mm:ss tt")
                                     })
                                     .ToListAsync();

                ViewBag.Total = history.Sum(a => a.Amount);

                return View(history);
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it)
                return View();
            }
        }


    }


}
