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
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using WebOptimus.Models.ViewModel;
using WebOptimus.Models.Stripe;
using DocumentFormat.OpenXml.Spreadsheet;



namespace WebOptimus.Controllers
{
    public class PaymentsController : BaseController
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
        public PaymentsController(IMapper mapper, UserManager<User> userManager,
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



        [HttpGet]
        public IActionResult MakePayment()
        {
            return View();
        }

        //U2972FS
        //[HttpPost]
        //public async Task<IActionResult> MakePayment(string identifier, CancellationToken ct)
        //{
        //    if (string.IsNullOrWhiteSpace(identifier))
        //    {
        //        TempData["Error"] = "Please enter an Email or Registration Number.";
        //        await RecordAuditAsync(null, identifier, _requestIpHelper.GetRequestIp(), "CheckFines", "User searched for: " + identifier + " but did not enter any value.", ct);
        //        return RedirectToAction(nameof(MakePayment));
        //    }

        //    // Find the dependent
        //    var dependent = await _db.Dependants
        //        .FirstOrDefaultAsync(d => (d.Email == identifier || d.PersonRegNumber == identifier), ct);


        //    if (dependent == null)
        //    {
        //        TempData["Error"] = "No records found, please contact us on the details below this page.";
        //        await RecordAuditAsync(null, identifier, _requestIpHelper.GetRequestIp(), "CheckFines", "User searched for: " + identifier + " but no dependent details found.", ct);
        //        return RedirectToAction(nameof(MakePayment));
        //    }

        //    var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.PersonRegNumber == dependent.PersonRegNumber, ct);

        //    string userEmail = currentUser?.Email ?? "onlinepayment@umojawetu.com";

        //    // Fetch all closed causes
        //    var closedCauses = await _db.Cause
        //        .Where(c => c.IsClosed && !c.IsActive)
        //        .ToListAsync(ct);

        //    decimal totalMissedPayments = 0m;
        //    decimal totalMissedPaymentFees = 0m; //  Track missed payment fees
        //    var missedPayments = new List<PaymentHistoryViewModel>();

        //    foreach (var cause in closedCauses)
        //    {
        //        if (cause.IsClosedDate <= dependent.DateCreated)
        //        {
        //            continue; // Skip causes closed before the dependent joined
        //        }

        //        bool hasNotPaid = !await _db.Payment.AnyAsync(p => p.personRegNumber == dependent.PersonRegNumber && p.CauseCampaignpRef == cause.CauseCampaignpRef, ct);
        //        bool wasEligible = CalculateAgeAtYear(dependent.PersonYearOfBirth.ToString(), cause.DateCreated) >= 25;
        //        bool wasMemberBeforeClose = dependent.DateCreated <= cause.IsClosedDate;

        //        if (hasNotPaid && wasEligible && wasMemberBeforeClose)
        //        {
        //            var missedAmount = cause.FullMemberAmount;
        //            var missedPaymentFee = cause.MissPaymentAmount; //  Get the missed payment fee from the Cause table
        //            totalMissedPayments += missedAmount;
        //            totalMissedPaymentFees += missedPaymentFee; //  Add missed payment fees

        //            missedPayments.Add(new PaymentHistoryViewModel
        //            {
        //                DependentName = dependent.PersonName,
        //                YearOfBirth = dependent.PersonYearOfBirth,
        //                RegNumber = dependent.PersonRegNumber,
        //                CauseCampaignpRef = cause.CauseCampaignpRef,
        //                Amount = missedAmount,
        //                LatePaymentFee = missedPaymentFee, //  Include late payment fee in the breakdown
        //                IsClosedDate = cause.IsClosedDate
        //            });
        //        }

        //        await RecordAuditAsync(null, identifier, _requestIpHelper.GetRequestIp(), "MakePayment Details", $"User shown missed payments details for cause Ref: {cause.CauseCampaignpRef} Amount: {cause.FullMemberAmount} Late Fee: {cause.MissPaymentAmount}", ct);
        //    }

        //    if (!missedPayments.Any())
        //    {
        //        TempData["Success"] = "No outstanding Payment(s).";
        //        await RecordAuditAsync(null, identifier, _requestIpHelper.GetRequestIp(), "MakePayment", $"User searched for: {identifier} but has no outstanding fines.", ct);
        //        return RedirectToAction(nameof(MakePayment));
        //    }

        //    //  Calculate transaction fees (1.4% + £0.20)
        //    var transactionFees = ((totalMissedPayments + totalMissedPaymentFees) * 0.015m) + 0.20m;
        //    var totalToPay = totalMissedPayments + totalMissedPaymentFees + transactionFees; //  Add everything to the total

        //    var model = new FinesViewModel
        //    {
        //        UserId = dependent.UserId,
        //        DependentId = dependent.Id,
        //        PersonName = dependent.PersonName.Split(' ').FirstOrDefault(), 
        //        Email = userEmail, 
        //        PersonRegNumber = dependent.PersonRegNumber, 
        //        CauseCampaignpRef = closedCauses.Select(c => c.CauseCampaignpRef).FirstOrDefault(),
        //        Amount = totalMissedPayments,
        //        MissedPaymentFees = totalMissedPaymentFees,
        //        TransactionFees = transactionFees,
        //        TotalToPay = totalToPay,
        //        MissedPayments = missedPayments
        //    };


        //    await RecordAuditAsync(null, identifier, _requestIpHelper.GetRequestIp(), "MakePayment", $"User searched for: {identifier} and found details, navigated to fines details.", ct);

        //    return View("MakePaymentDetails", model);
        //}

        [HttpPost]
        public async Task<IActionResult> MakePayment(string identifier, CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(identifier))
                {
                    TempData["Error"] = "Please enter an Email or Registration Number.";
                    await RecordAuditAsync(null, identifier, _requestIpHelper.GetRequestIp(), "CheckFines", $"User searched for: {identifier} but did not enter any value.", ct);
                    return RedirectToAction(nameof(MakePayment));
                }

                // Find the dependent
                var dependent = await _db.Dependants.FirstOrDefaultAsync(d => d.PersonRegNumber == identifier, ct);

                if (dependent == null)
                {
                    TempData["Error"] = "No records found, please contact us.";
                    await RecordAuditAsync(null, identifier, _requestIpHelper.GetRequestIp(), "CheckFines", $"User searched for: {identifier} but no dependent details found.", ct);
                    return RedirectToAction(nameof(MakePayment));
                }

                string userEmail = dependent?.Email ?? "onlinepayment@umojawetu.com";             

                var unpaidCauses = await _db.Cause
                .Where(c =>
                    (
                        c.IsActive == true ||
                        (!c.IsActive && c.EndDate.HasValue && c.EndDate.Value < DateTime.UtcNow)
                    )
                    && c.DateCreated >= dependent.DateCreated
                    && !_db.Payment.Any(p => p.personRegNumber == dependent.PersonRegNumber && p.CauseCampaignpRef == c.CauseCampaignpRef))
                .ToListAsync(ct);


                if (!unpaidCauses.Any())
                {
                    TempData["Success"] = "No outstanding Payment(s).";
                    await RecordAuditAsync(null, identifier, _requestIpHelper.GetRequestIp(), "MakePayment", $"User searched for: {identifier} but has no unpaid causes.", ct);
                    return RedirectToAction(nameof(MakePayment));
                }

                await RecordAuditAsync(null, identifier, _requestIpHelper.GetRequestIp(), "MakePayment",
                    $"User searched for: {identifier}. Found {unpaidCauses.Count} unpaid causes (excluding causes before they joined).", ct);

                // Fetch Custom Payments for this person
                var customPayments = await _db.CustomPayment
       .Where(cp => cp.PersonRegNumber == dependent.PersonRegNumber)
       .ToDictionaryAsync(cp => cp.CauseCampaignpRef, cp => cp);

                // Prepare ViewModel with Eligibility Check
                var missedPayments = new List<PaymentHistoryViewModel>();

                foreach (var cause in unpaidCauses)
                {
                    var contributionDate = cause.StartDate ?? cause.DateCreated;

                    bool wasEligible = CalculateAgeAtYear(dependent.PersonYearOfBirth.ToString(), contributionDate) >= cause.UnderAge;
                    bool wasMemberAtStart = dependent.DateCreated <= contributionDate;

                    bool isMissedPayment = cause.IsClosed == true && cause.EndDate.HasValue && cause.EndDate.Value < DateTime.UtcNow;
                 
                    if (wasEligible && wasMemberAtStart)
                    {
                        decimal originalLateFee = 0;
                        decimal reducedLateFee = 0;

                        if (isMissedPayment)
                        {
                            originalLateFee = cause.MissPaymentAmount;

                            if (customPayments.TryGetValue(cause.CauseCampaignpRef, out var customPayment))
                            {
                                reducedLateFee = customPayment.ReduceFees;
                            }
                            else
                            {
                                reducedLateFee = originalLateFee;
                            }
                        }


                        missedPayments.Add(new PaymentHistoryViewModel
                        {
                            DependentName = dependent.PersonName,
                            YearOfBirth = dependent.PersonYearOfBirth,
                            RegNumber = dependent.PersonRegNumber,
                            CauseCampaignpRef = cause.CauseCampaignpRef,
                            Amount = cause.FullMemberAmount,
                            OriginalLatePaymentFee = originalLateFee, // Store original Late Fee
                            LatePaymentFee = reducedLateFee, // Store reduced Late Fee
                            EndDate = cause.EndDate
                        });
                    }
                }
                // Set ViewBag for View
                ViewBag.HasMissedPayments = missedPayments.Any(mp => mp.LatePaymentFee > 0);
                ViewBag.HasActivePayments = missedPayments.Any(mp => mp.LatePaymentFee == 0);

                if (!missedPayments.Any())
                {
                    TempData["Success"] = "No outstanding Payment(s).";
                    await RecordAuditAsync(null, identifier, _requestIpHelper.GetRequestIp(), "MakePayment", $"User searched for: {identifier} but has no unpaid eligible causes.", ct);
                    return RedirectToAction(nameof(MakePayment));
                }

                // Calculate total cost correctly with reductions
                decimal totalAmount = missedPayments.Sum(mp => mp.Amount);
                decimal totalMissedFees = missedPayments.Sum(mp => mp.LatePaymentFee); // Now correctly reduced
                                                                                       //decimal transactionFees = ((totalAmount + totalMissedFees) * 0.015m) + 0.20m;
                                                                                       //decimal totalToPay = totalAmount + totalMissedFees + transactionFees;

                decimal transactionFees = ((totalAmount + totalMissedFees) * 0.015m) + 0.20m;
                decimal totalToPay = totalAmount + totalMissedFees; //  Excludes transaction fees

                var searchedByEmail = identifier.Contains("@");

                var model = new FinesViewModel
                {
                    UserId = dependent.UserId,
                    DependentId = dependent.Id,
                    PersonName = dependent.PersonName.Split(' ').FirstOrDefault(),
                    Email = searchedByEmail ? identifier : userEmail,
                    SearchIdentifier = identifier,
                    PersonRegNumber = dependent.PersonRegNumber,
                    CauseCampaignpRef = missedPayments.FirstOrDefault()?.CauseCampaignpRef,
                    Amount = totalAmount,
                    MissedPaymentFees = totalMissedFees, // Ensure Missed Payment Fees are applied only if closed
                    TransactionFees = transactionFees,
                    TotalToPay = totalToPay,
                    MissedPayments = missedPayments
                };

                await RecordAuditAsync(null, identifier, _requestIpHelper.GetRequestIp(), "MakePayment", $"User searched for: {identifier} and found unpaid eligible causes.", ct);

                return View("MakePaymentDetails", model);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, identifier, _requestIpHelper.GetRequestIp(), "MakePayment", $"User had error for: {identifier} " + ex.Message, ct);
                return View("MakePayment");
            }
        }


        [HttpGet]
        public IActionResult MakePaymentDetails(FinesViewModel model)
        {
            return View(model);
        }
        private int CalculateAgeAtYear(string personYearOfBirth, DateTime startDate)
        {
            if (int.TryParse(personYearOfBirth, out var birthYear))
            {
                return startDate.Year - birthYear;
            }
            return 0; // Return 0 if parsing fails
        }


        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ProcessFinePayment(FinesViewModel model, CancellationToken ct)
        {
            try
            {
                            

                if (model == null || model.Amount <= 0 || model.TotalToPay <= 0 || model.TransactionFees <= 0)
                {
                    TempData["Error"] = "Invalid payment request.";
                     return RedirectToAction(nameof(MakePayment));
                }

                StripeConfiguration.ApiKey = _stripeSetting.SecretKey;
                var totalAmountInCents = Convert.ToInt32(Math.Round(model.TotalToPay * 100));               
                var ourRef = $"{model.CauseCampaignpRef}{new Random().Next(0, 9999999):D5}";
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string>
                {
                    "card",                  
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
                    SuccessUrl = Url.Action(nameof(FinePaymentConfirmation), "Payments", null, Request.Scheme) + "?session_id={CHECKOUT_SESSION_ID}",
                    CancelUrl = Url.Action("MakePayment", "Payments", null, Request.Scheme)
                };

                var service = new SessionService();
                var session = service.Create(options);

                //  Save Payment Session (Store Missed Payments in JSON)
                var paymentSession = new PaymentSession
                {
                    SessionId = session.Id,
                    UserId = model.UserId,
                    Email = model.Email,                    
                    Amount = model.Amount,
                    CauseCampaignpRef = model.CauseCampaignpRef,
                    DependentId = model.DependentId,
                    personRegNumber = model.PersonRegNumber,
                    TransactionFees = model.TransactionFees,
                    TotalAmount = model.TotalToPay,
                    IsPaid = false,
                    OurRef = ourRef,
                    Reason = "",
                    DateCreated = DateTime.UtcNow,

                };

                _db.PaymentSessions.Add(paymentSession);
                await _db.SaveChangesAsync(ct);

                return Redirect(session.Url);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Payment processing failed.";
                return RedirectToAction(nameof(MakePayment));
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> FinePaymentConfirmation(string session_id, CancellationToken ct)
        {
            try
            {
                StripeConfiguration.ApiKey = _stripeSetting.SecretKey;
                var service = new SessionService();
                var session = service.Get(session_id);

                if (session.PaymentStatus == "paid" || session.Status == "complete")
                {
                    var paymentSession = await _db.PaymentSessions
                        .FirstOrDefaultAsync(ps => ps.SessionId == session_id, ct);

                    if (paymentSession == null)
                    {
                        TempData["Error"] = "Invalid payment session.";
                        await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "FinePaymentConfirmation",
                            $"Payment session not found: {session_id}", ct);
                        return RedirectToAction(nameof(MakePayment));
                    }

                    if (paymentSession.IsPaid)
                    {
                        TempData["Success"] = "Payment for this session has already been processed.";
                        return RedirectToAction(nameof(MakePayment));
                    }
                    var user = await _db.Users.FirstOrDefaultAsync(u => u.PersonRegNumber == paymentSession.personRegNumber, ct);
                    var dependent = await _db.Dependants.FirstOrDefaultAsync(d => d.PersonRegNumber == paymentSession.personRegNumber, ct);

                    if (user != null)
                    {
                        user.IsActive = true;
                        user.DeactivationReason = "";
                        _db.Users.Update(user);
                        await _db.SaveChangesAsync(ct);
                    }

                    if (dependent != null)
                    {
                        dependent.IsActive = true;
                        dependent.DeactivationReason = "";
                        _db.Dependants.Update(dependent);
                        await _db.SaveChangesAsync(ct);
                    }


                    //  Fetch missed payments from `Cause` table based on `DependentId`
                    var unpaidFines = await _db.Cause                       
                        .Where(c => !_db.Payment.Any(p => p.personRegNumber == paymentSession.personRegNumber && p.CauseCampaignpRef == c.CauseCampaignpRef))
                        .Select(c => new FinesViewModel
                        {
                            PersonRegNumber = paymentSession.personRegNumber ?? "",
                            CauseCampaignpRef = c.CauseCampaignpRef,
                            Amount = c.FullMemberAmount,
                            MissedPaymentFees = c.IsClosed == true ? c.MissPaymentAmount : 0
                        })
                        .ToListAsync(ct);

                    if (!unpaidFines.Any())
                    {
                        TempData["Success"] = "No outstanding Payment found.";
                        return RedirectToAction("MakePayment");
                    }

                    decimal totalPaidAmount = 0;

                    foreach (var fine in unpaidFines)
                    {
                        var existingPayment = await _db.Payment
                        .AnyAsync(p => p.OurRef == paymentSession.OurRef
                            && p.personRegNumber == fine.PersonRegNumber
                            && p.Amount == fine.Amount, ct);
                        if (existingPayment)
                        {
                            continue; // Skip this entry if payment already exists
                        }

                        // 🔹 Store record in the `Payment` table
                        var paymentRecord = new Payment
                        {
                            UserId = paymentSession.UserId,
                            DependentId = paymentSession.DependentId ?? 0,
                            personRegNumber = paymentSession.personRegNumber,
                            Amount = fine.Amount,
                            GoodwillAmount = 0,
                            CauseCampaignpRef = fine.CauseCampaignpRef,
                            HasPaid = true,
                            Notes = paymentSession.Reason,
                            DateCreated = DateTime.UtcNow,
                            CreatedBy = paymentSession.Email,
                            OurRef = paymentSession.OurRef
                        };

                        _db.Payment.Add(paymentRecord);
                        totalPaidAmount += fine.Amount;

                        var existingChecklistItem = await _db.DependentChecklistItems
    .AnyAsync(dci => dci.PersonRegNumber == fine.PersonRegNumber
        && dci.CauseCampaignpRef == fine.CauseCampaignpRef
        && dci.PaymentSessionId == paymentSession.Id, ct);

                        if (!existingChecklistItem) //  Add only if not already stored
                        {
                            var checklistItem = new DependentChecklistItem
                            {
                                DependentId = dependent.Id,
                                Name = dependent?.PersonName,
                                MissedPayment = fine.MissedPaymentFees,
                                PersonRegNumber = fine.PersonRegNumber,
                                UserId = paymentSession.UserId,
                                CauseCampaignpRef = fine.CauseCampaignpRef,
                                Price = fine.Amount,
                                CustomAmount = fine.Amount,
                                IsSelected = true,
                                PaymentSessionId = paymentSession.Id,
                                SessionId = session.Id,
                            };

                            _db.DependentChecklistItems.Add(checklistItem);
                        }
                    }

                    //  Update the total amount raised for each campaign
                    var affectedCampaigns = unpaidFines
                        .GroupBy(f => f.CauseCampaignpRef)
                        .Select(g => g.Key)
                        .ToList();

                    foreach (var campaignRef in affectedCampaigns)
                    {
                        var cause = await _db.Cause
                            .FirstOrDefaultAsync(c => c.CauseCampaignpRef == campaignRef, ct);

                        if (cause != null)
                        {
                            decimal totalPaidForCampaign = unpaidFines
                                .Where(f => f.CauseCampaignpRef == campaignRef)
                                .Sum(f => f.Amount);
                            _db.Cause.Update(cause);
                        }
                    }

                    //  Mark the payment session as complete


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

                    //Pass payment details to the confirmation view
                    ViewBag.Ref = paymentSession.OurRef;
                    ViewBag.TotalPaid = paymentSession.TotalAmount;
                    ViewBag.TransactionFees = paymentSession.TransactionFees;
                    ViewBag.MissedPayments = unpaidFines; // Pass fines to view

                    await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "FinePaymentConfirmation",
                        $"Fine payment confirmed for sessionId: {session_id}", ct);


                    return View("FinePaymentConfirmation");
                }

                TempData["Error"] = "Payment verification failed.";
                return RedirectToAction(nameof(MakePayment));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Payment verification failed.";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "FinePaymentConfirmation",
                    $"Error processing payment confirmation: {ex.Message}", ct);
                return RedirectToAction(nameof(MakePayment));
            }
        }



    }


}
