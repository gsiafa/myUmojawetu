using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RotativaCore;
using Stripe.Checkout;
using Stripe;
using System;
using System.Globalization;
using WebOptimus.Data;
using WebOptimus.Helpers;
using WebOptimus.Models;
using WebOptimus.Models.Stripe;
using WebOptimus.Models.ViewModel;
using WebOptimus.Models.ViewModel.Admin;
using WebOptimus.Services;
using WebOptimus.StaticVariables;
using Microsoft.Extensions.Options;
using WebOptimus.Migrations;
using OtherDonationPayment = WebOptimus.Models.OtherDonationPayment;



namespace WebOptimus.Controllers
{
   
    public class OtherController : BaseController
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
        private readonly StripeSettings _stripeSetting;
        public OtherController(IMapper mapper, UserManager<User> userManager,
           SignInManager<User> signInManager, IWebHostEnvironment hostEnvironment, IOptions<StripeSettings> stripeSetting, RoleManager<IdentityRole> roleManager, IUserStore<User> userStore,
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
            _stripeSetting = stripeSetting.Value;
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
            // Fetch all OtherDonation records from the database
            var donations = await _db.DonationForNonDeathRelated               
                .ToListAsync(ct);
            // Map to ViewModel using AutoMapper
            var donationViewModels = _mapper.Map<List<OtherDonationViewModel>>(donations);

            var currentUser = await _userManager.FindByEmailAsync(email);

            if(currentUser.Email == "seakou2@yahoo.com")
            {
                ViewBag.isAdmin = "true";
            }
            else
            {
                ViewBag.isAdmin = "false";
            }
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "AddNewDonation",
                 $"User navigated to Other Donation page", ct);
            // Pass the mapped list to the view
            return View(donationViewModels);
    
        }
        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> AddNewDonation(CancellationToken ct)
        {

            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "AddNewDonation",
                 $"User navigated to AddNewDonation page", ct);
            return View();
        }

      
        [HttpPost]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> AddNewDonation(OtherDonationViewModel model, CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                if (email == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                var currentUser = await _db.Users.FirstOrDefaultAsync(a=>a.Email == email);

                // Remove validation for fields not relevant for this context
                ModelState.Remove(nameof(model.StartDate));
                ModelState.Remove(nameof(model.ClosedDate));
                ModelState.Remove(nameof(model.IsActive));
                ModelState.Remove(nameof(model.CauseCampaignpRef));
                ModelState.Remove(nameof(model.DateCreated));
                ModelState.Remove(nameof(model.MinmumAmount));
                ModelState.Remove(nameof(model.CreatedBy));
                ModelState.Remove(nameof(model.Status));
        
                if (!ModelState.IsValid)
                {
                    foreach (var state in ModelState)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            Console.WriteLine($"Field: {state.Key}, Error: {error.ErrorMessage}");
                        }
                    }

                    return View(model);
                }
                DateTime now = DateTime.Now;
                string formattedDate = now.ToString("yyyyMMdd_HHmmss"); 
              

                var cause = new DonationForNonDeathRelated
                    {
                        Summary = model.Summary,
                        Description = model.Description,
                        TargetAmount = model.TargetAmount,
                        DependentId = currentUser.DependentId,
                    PersonRegNumber = currentUser.PersonRegNumber,
                    MinmumAmount = model.MinmumAmount,                       
                        IsActive = false,            
                        Status = DonationStatus.pendingGeneralAdminApproval,
                        DateCreated = DateTime.UtcNow,                     
                       StartDate= model.StartDate,
                       ClosedDate= model.ClosedDate,
                        CreatedBy = currentUser.Email,
                        CauseCampaignpRef = "UMO_" + formattedDate
                };                
                _db.DonationForNonDeathRelated.Add(cause);
                await _db.SaveChangesAsync(ct);

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "AddNewDonation",
                    $"User created a new campaign: {model.Summary}", ct);

                TempData[SD.Success] = "Cause created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
        [HttpGet]
       
        [Authorize]
        public async Task<IActionResult> PaymentDetails(string paymentId, CancellationToken ct)
        {
            try
            {
                //  Fetch all payment sessions for the given paymentId where IsPaid is true
                var paymentSessions = await _db.PaymentSessions
                    .Where(ps => ps.OurRef == paymentId && ps.IsPaid == true)
                    .ToListAsync(ct);

                if (!paymentSessions.Any())
                {
                    TempData["Error"] = "No payment sessions found. Please contact admin.";
                    return RedirectToAction("PaymentHistory");
                }

                //  Get the CauseCampaignpRef from the session
                var causeCampaignRefs = paymentSessions.Select(ps => ps.CauseCampaignpRef).Distinct().ToList();

                //  Fetch only non-death-related contributions from OtherDonationPayment
                var otherDonations = await _db.OtherDonationPayment
                    .Where(p => causeCampaignRefs.Contains(p.CauseCampaignpRef))
                    .ToListAsync(ct);

                //  Fetch dependent checklist items (Ensure filtering by PaymentSessionId & IsSelected = true)
                var paymentSessionIds = paymentSessions.Select(ps => ps.Id).ToList();
                var dependentChecklistItems = await _db.DependentChecklistItems
                    .Where(d => paymentSessionIds.Contains(d.PaymentSessionId) && d.IsSelected == true)
                    .ToListAsync(ct);

                //  Ensure unique checklist items and join with names from Dependant table
                var dependentIds = dependentChecklistItems.Select(d => d.PersonRegNumber).Distinct().ToList();
                var dependents = await _db.Dependants
                    .Where(d => dependentIds.Contains(d.PersonRegNumber))
                    .ToDictionaryAsync(d => d.Id, d => d.PersonName, ct);

                //  Map names from Dependants table
                foreach (var item in dependentChecklistItems)
                {
                    if (dependents.TryGetValue(item.DependentId, out var name))
                    {
                        item.Name = name; // Set correct name
                    }
                }

                //  Ensure distinct contributions (Avoid duplicates)
                var uniqueChecklistItems = dependentChecklistItems
                    .GroupBy(d => d.PersonRegNumber)
                    .Select(g => g.First())
                    .ToList();

                //  Determine if there are non-death-related payments
                bool hasNonDeathPayments = otherDonations.Any();

                if (!hasNonDeathPayments && !uniqueChecklistItems.Any())
                {
                    TempData["Error"] = "No payment details found.";
                    return RedirectToAction("PaymentHistory");
                }

                //  Prepare the ViewModel
                var model = new OtherPaymentsVM
                {
                    PaymentSessions = paymentSessions,
                    PaymentItems = uniqueChecklistItems, // Ensure unique checklist items
                    OtherDonationPayments = otherDonations
                };

                //  Calculate total amounts separately
                ViewBag.TotalAmountPaid = paymentSessions.Sum(ps => ps.TotalAmount);

                //  Set ViewBag flags for display logic
                ViewBag.HasNonDeathPayments = hasNonDeathPayments;
                ViewBag.HasDependentChecklistItems = uniqueChecklistItems.Any();

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while processing your request. Please try again later.";
                return RedirectToAction("PaymentHistory");
            }
        }
       
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Get logged-in user's dependent ID
            var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.Email == email && a.IsActive == true, ct);
            string? currentUserDependentId = currentUser?.PersonRegNumber;

            var donation = await _db.DonationForNonDeathRelated
                .FirstOrDefaultAsync(d => d.Id == id, ct);

            if (donation == null)
            {
                TempData["Error"] = "Donation not found.";
                return RedirectToAction("Index");
            }

            var viewModel = _mapper.Map<OtherDonationDetailsViewModel>(donation);

            // Ensure date strings are formatted correctly
            viewModel.StartDateAsString = viewModel.StartDate?.ToString("dd/MM/yyyy");
            viewModel.ClosedDateAsString = viewModel.ClosedDate?.ToString("dd/MM/yyyy");
            viewModel.DateCreatedAsString = viewModel.DateCreated.ToString("dd/MM/yyyy");

            // Pass the logged-in user's DependentId to the view
            if(currentUser.PersonRegNumber == donation.PersonRegNumber && currentUser.Email == donation.CreatedBy)
            {
                ViewBag.CurrentUserDependentId = "true";
            }
            else
            {
                ViewBag.CurrentUserDependentId = "false";
            }

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Details",
                   $"User navigated to campaign details page for: {donation.Id}", ct);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string approvalNote, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Get logged-in user's dependent ID
            var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.Email == email && a.IsActive == true, ct);
            if (string.IsNullOrWhiteSpace(approvalNote))
            {
                TempData["Error"] = "Approval note is required.";
                return RedirectToAction("Details", new { id = id });
            }

            var donation = await _db.DonationForNonDeathRelated.FindAsync(id);
            if (donation == null)
            {
                TempData["Error"] = "Donation not found.";
                return RedirectToAction("Index");
            }

            donation.Status = DonationStatus.Live;
            donation.ApprovedBy = currentUser.Email;
            donation.ApprovalOrDeclinerNote = approvalNote;
            donation.ApprovedDate = DateTime.UtcNow;
            donation.IsActive = true;
            donation.IsDisplayable = true;
                _db.Update(donation);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Donation approved successfully.";

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "AddNewDonation Approved",
                   $"User Approved new campaign: {donation.Summary}", ct);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(int id, string declinedNote, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Get logged-in user's dependent ID
            var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.Email == email && a.IsActive == true, ct);
            if (string.IsNullOrWhiteSpace(declinedNote))
            {
                TempData["Error"] = "Decline note is required.";
                return RedirectToAction(nameof(Index));
            }

            var donation = await _db.DonationForNonDeathRelated.FindAsync(id);
            if (donation == null)
            {
                TempData["Error"] = "Donation not found.";
                return RedirectToAction(nameof(Index));
            }

            donation.Status = DonationStatus.NotLive;
            donation.DeclinedBy = User.Identity.Name;
            donation.DeclinedDate = DateTime.UtcNow;
            donation.ApprovalOrDeclinerNote = declinedNote;

            _db.Update(donation);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Donation declined successfully.";
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "AddNewDonation Declined",
                 $"User Declined new campaign: {donation.Summary}", ct);
            return RedirectToAction(nameof(Index));
                      
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

            // Get logged-in user's dependent ID
            var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.Email == email && a.IsActive==true, ct);
            var donation = await _db.DonationForNonDeathRelated.FindAsync(id);
            if (donation == null)
            {
                TempData["Error"] = "Donation not found.";
                return RedirectToAction("Index");
            }

            _db.DonationForNonDeathRelated.Remove(donation);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Donation deleted successfully.";
            // Record full details before deletion
            string deletedDetails = $"Campaign Reference: {donation.CauseCampaignpRef}, " +
                                    $"Summary: {donation.Summary}, " +
                                    $"Target Amount: {donation.TargetAmount}, " +
                                    $"Created By: {donation.CreatedBy}, " +
                                    $"Start Date: {donation.StartDate?.ToString("dd/MM/yyyy")}, " +
                                    $"End Date: {donation.ClosedDate?.ToString("dd/MM/yyyy")}, " +
                                    $"Status: {donation.Status}";

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Donation Deleted",
                $"User deleted donation: {deletedDetails}", ct);
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Get logged-in user's dependent ID
            var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.Email == email && a.IsActive == true, ct);
            var donation = await _db.DonationForNonDeathRelated.FindAsync(id);
            if (donation == null)
            {
                TempData["Error"] = "Donation not found.";
                return RedirectToAction("Index");
            }

            var viewModel = _mapper.Map<OtherDonationViewModel>(donation);

            // 🔹 Manually format TargetAmount
            viewModel.TargetAmount = decimal.Parse(donation.TargetAmount.ToString("N2"));
            viewModel.MinmumAmount = donation.MinmumAmount.HasValue ? decimal.Parse(donation.MinmumAmount.Value.ToString("N2")) : 0;

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Donation Edit",
            $"User navigated to donation edit: {donation.Id}", ct);
            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OtherDonationViewModel model, CancellationToken ct)
        {
            try
            {
                ModelState.Remove(nameof(model.StartDate));
                ModelState.Remove(nameof(model.ClosedDate));
                ModelState.Remove(nameof(model.IsActive));       
                ModelState.Remove(nameof(model.DateCreated));
                ModelState.Remove(nameof(model.MinmumAmount));
                ModelState.Remove(nameof(model.CreatedBy));
                ModelState.Remove(nameof(model.Status));
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

                var donation = await _db.DonationForNonDeathRelated.FindAsync(model.Id);
                if (donation == null)
                {
                    TempData["Error"] = "Donation not found.";
                    return RedirectToAction("Index");
                }

                // Create a list to store change logs
                List<ChangeLog> changeLogs = new List<ChangeLog>();

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

                // Compare fields and log changes
                LogChange("Summary", donation.Summary, model.Summary);
                LogChange("IsDisplayable", donation.IsDisplayable, model.IsDisplayable);
                LogChange("Description", donation.Description, model.Description);
                LogChange("TargetAmount", donation.TargetAmount, model.TargetAmount);
                LogChange("MinmumAmount", donation.MinmumAmount, model.MinmumAmount);
                LogChange("StartDate", donation.StartDate?.ToString("dd/MM/yyyy"), model.StartDate.ToString("dd/MM/yyyy"));
                LogChange("ClosedDate", donation.ClosedDate?.ToString("dd/MM/yyyy"), model.ClosedDate?.ToString("dd/MM/yyyy"));
                LogChange("IsActive", donation.IsActive, model.IsActive);
                LogChange("CauseCampaignpRef", donation.CauseCampaignpRef, model.CauseCampaignpRef);
                LogChange("Status", donation.Status, model.Status);

                // Map updated values to the donation object
                // Ensure CreatedBy is not null before saving
                if (string.IsNullOrEmpty(model.CreatedBy))
                {
                    model.CreatedBy = donation.CreatedBy;  // Preserve existing value from the database
                }
                // Ensure CreatedBy is not null before saving
                model.DateCreated = donation.DateCreated;
                model.CauseCampaignpRef = donation.CauseCampaignpRef;
              
                model.Status = donation.Status;
                if (model.ClosedDate != null)
                {
                    donation.ClosedDate = model.ClosedDate;
                }


                _mapper.Map(model, donation);

                // Save changes
                _db.DonationForNonDeathRelated.Update(donation);
                await _db.SaveChangesAsync(ct);


                if (model.IsDisplayable == true)
                {

                    donation.IsDisplayable = true;
                    _db.DonationForNonDeathRelated.Update(donation);
                }
                else
                {
                    donation.IsDisplayable = false;
                    _db.DonationForNonDeathRelated.Update(donation);

                }
                // Save change logs if any changes were detected
                if (changeLogs.Any())
                {
                    await _db.ChangeLogs.AddRangeAsync(changeLogs, ct);
                    await _db.SaveChangesAsync(ct);
                }

                // Record audit log
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Edit Donation",
                    $"User edited donation: {donation.Summary} (ID: {donation.Id})", ct);

                TempData["Success"] = "Donation updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error:"+ ex.Message;
                return RedirectToAction("Index");
            }
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
                //  Fetch all payment sessions for the given paymentId where IsPaid is true
                var paymentSessions = await _db.PaymentSessions
                    .Where(ps => ps.OurRef == paymentId && ps.IsPaid == true)
                    .ToListAsync(ct);

                if (!paymentSessions.Any())
                {
                    TempData["Error"] = "No payment sessions found. Please contact admin.";
                    return RedirectToAction("PaymentHistory");
                }

                //  Get the CauseCampaignpRef from the session
                var causeCampaignRefs = paymentSessions.Select(ps => ps.CauseCampaignpRef).Distinct().ToList();

                //  Fetch only non-death-related contributions from OtherDonationPayment
                var otherDonations = await _db.OtherDonationPayment
                    .Where(p => causeCampaignRefs.Contains(p.CauseCampaignpRef))
                    .ToListAsync(ct);

                //  Fetch dependent checklist items (Ensure filtering by PaymentSessionId & IsSelected = true)
                var paymentSessionIds = paymentSessions.Select(ps => ps.Id).ToList();
                var dependentChecklistItems = await _db.DependentChecklistItems
                    .Where(d => paymentSessionIds.Contains(d.PaymentSessionId) && d.IsSelected == true)
                    .ToListAsync(ct);

                //  Ensure unique checklist items and join with names from Dependant table
                var dependentIds = dependentChecklistItems.Select(d => d.PersonRegNumber).Distinct().ToList();
                var dependents = await _db.Dependants
                    .Where(d => dependentIds.Contains(d.PersonRegNumber))
                    .ToDictionaryAsync(d => d.Id, d => d.PersonName, ct);

                //  Map names from Dependants table
                foreach (var item in dependentChecklistItems)
                {
                    if (dependents.TryGetValue(item.DependentId, out var name))
                    {
                        item.Name = name; // Set correct name
                    }
                }

                //  Ensure distinct contributions (Avoid duplicates)
                var uniqueChecklistItems = dependentChecklistItems
                    .GroupBy(d => d.PersonRegNumber)
                    .Select(g => g.First())
                    .ToList();

                //  Determine if there are non-death-related payments
                bool hasNonDeathPayments = otherDonations.Any();

                if (!hasNonDeathPayments && !uniqueChecklistItems.Any())
                {
                    TempData["Error"] = "No payment details found.";
                    return RedirectToAction("PaymentHistory");
                }

                //  Prepare the ViewModel (SAME AS PaymentDetails)
                var model = new OtherPaymentsVM
                {
                    PaymentSessions = paymentSessions,
                    PaymentItems = uniqueChecklistItems, // Ensure unique checklist items
                    OtherDonationPayments = otherDonations
                };

                //  Calculate total amounts separately
                ViewBag.TotalAmountPaid = paymentSessions.Sum(ps => ps.TotalAmount);
                ViewBag.TransactionFees = paymentSessions.Sum(ps => ps.TransactionFees);
                ViewBag.NetReceived = ViewBag.TotalAmountPaid - ViewBag.TransactionFees;

                //  Set ViewBag flags for display logic
                ViewBag.HasNonDeathPayments = hasNonDeathPayments;
                ViewBag.HasDependentChecklistItems = uniqueChecklistItems.Any();

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while processing your request. Please try again later.";
                return RedirectToAction("PaymentHistory");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Members(string Id, CancellationToken ct)
        {
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");
            if (currentUserEmail == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _db.Users.FirstOrDefaultAsync(a=>a.Email == currentUserEmail && a.IsActive == true);

            try
            {
                OtherDonerVM vm = new OtherDonerVM();

                // Fetch the cause by CauseCampaignpRef
                var causes = await _db.DonationForNonDeathRelated.FirstOrDefaultAsync(a => a.CauseCampaignpRef == Id);

                if (causes == null)
                {
                    TempData[SD.Error] = "Error - cause not found - Please contact admin.";
                    return RedirectToAction("Index", "Home");
                }

                vm.Cas = causes;

                // Fetch dependents for the current user
                var dependents = await _db.Dependants
                    .Where(d => d.UserId == currentUser.UserId && d.IsActive == true)
                    .ToListAsync();

                //Fetch group members for the current user
                var userDependent = await _db.Dependants.Where(a => a.IsActive == true)
                    .FirstOrDefaultAsync(d => d.PersonRegNumber == currentUser.PersonRegNumber);

                if (userDependent != null)
                {
                    var group = await _db.GroupMembers
                        .FirstOrDefaultAsync(gm => gm.PersonRegNumber == userDependent.PersonRegNumber && gm.Status == "Confirmed");

                    if (group != null)
                    {
                        var groupMembers = await _db.GroupMembers
                            .Where(gm => gm.Status == "Confirmed" && gm.GroupId == group.GroupId)
                            .Join(_db.Dependants, gm => gm.PersonRegNumber, d => d.PersonRegNumber, (gm, d) => new
                            {
                                gm.GroupId,
                                d.Id,
                                d.PersonName,
                                d.PersonRegNumber,
                                d.PersonYearOfBirth,
                                d.UserId
                            })
                            .Where(gm => gm.UserId != currentUser.UserId) // Exclude the current user's dependents
                            .ToListAsync();

                        var groupMemberChecklist = groupMembers.Select(gm => new DependentChecklistItem
                        {
                            GroupId = gm.GroupId,
                            DependentId = gm.Id,
                            PersonRegNumber = gm.PersonRegNumber,
                            Name = gm.PersonName,
                            IsSelected = false,
                            Price = causes?.MinmumAmount ?? 0m,
                        }).ToList();

                        vm.GroupMembers.AddRange(groupMemberChecklist);
                    }
                }

                // Get IDs of dependents who have already paid
                var paidDependentIds = await _db.OtherDonationPayment
                    .Where(p => p.CauseCampaignpRef == Id && p.HasPaid)
                    .Select(p => p.PersonRegNumber)
                    .ToListAsync();

                // Populate all dependents, marking those who have been paid
                foreach (var d in dependents)
                {                   

                    vm.DependentsChecklist.Add(new DependentChecklistItem
                    {
                        DependentId = d.Id,
                        PersonRegNumber = d.PersonRegNumber,
                        Name = d.PersonName,
                        IsSelected = false,
                        Price = causes?.MinmumAmount ?? 0m,
                        Paid = paidDependentIds.Contains(d.PersonRegNumber) // Mark as paid if it exists in the payment table
                    });
                }

                // Populate all group members, marking those who have been paid
                foreach (var gm in vm.GroupMembers)
                {
                    gm.Paid = paidDependentIds.Contains(gm.PersonRegNumber); // Mark as paid if it exists in the payment table
                }

                vm.User = await _db.Users.FirstOrDefaultAsync(u => u.PersonRegNumber == currentUser.PersonRegNumber && u.IsActive == true);

                vm.CauseCampaignpRef = causes.CauseCampaignpRef;
                ViewBag.IsCause = "true";
                await RecordAuditAsync(vm.User, _requestIpHelper.GetRequestIp(), "Other Donation", "User navigated to donor page", ct);
                return View(vm);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Other Donation", $"Error: {ex.Message}", ct);
                TempData[SD.Error] = "Error - please contact Admin.";
                return RedirectToAction("Members", "Other");
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



        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Payment(OtherDonerVM _data, CancellationToken ct)
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
                    .Concat((_data.GroupMembers ?? new List<DependentChecklistItem>())
                    .Where(g => g.IsSelected && g.CustomAmount.GetValueOrDefault() >= g.Price))
                    .ToList();

                if (!selectedItems.Any())
                {
                    ModelState.AddModelError(string.Empty, "No valid items selected for payment.");
                    return View("Index", _data);
                }

                await RecordAuditAsync("SelectedDep", "Selected", $"Total Selected: {selectedItems.Count()}");

                // Compute total amount without transaction fees
                var totalAmount = selectedItems.Sum(i => i.CustomAmount.GetValueOrDefault());
                var totalAmountInCents = Convert.ToInt32(Math.Round(totalAmount * 100));
                var ourRef = $"{_data.CauseCampaignpRef}{new Random().Next(0, 9999999):D5}";
                var estimatedStripeFee = Math.Round(totalAmount * 0.015m + 0.20m, 2);
                // Prepare Stripe session
                StripeConfiguration.ApiKey = _stripeSetting.SecretKey;
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
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
                            Description = $"Goodwill ref#: {ourRef}"
                        }
                    },
                    Quantity = 1
                }
            },
                    Mode = "payment",
                    SuccessUrl = Url.Action(nameof(DonationConfirmation), "Other", null, Request.Scheme) + "?session_id={CHECKOUT_SESSION_ID}",
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
                    Amount = totalAmount,
                    DependentId = currentUser.DependentId,
                    personRegNumber = currentUser.PersonRegNumber,
                    CauseCampaignpRef = _data.CauseCampaignpRef,
                    TransactionFees = estimatedStripeFee,
                    TotalAmount = totalAmount,
                    IsPaid = false,
                    OurRef = ourRef,
                    Reason = _data.Reason,
                    DateCreated = DateTime.UtcNow,
                };

                _db.PaymentSessions.Add(paymentSession);
                await _db.SaveChangesAsync(ct);

                // Save selected dependents
                foreach (var item in selectedItems)
                {
                    if (item.CustomAmount.GetValueOrDefault() <= 0)
                        continue;

                    var dependentDetails = await _db.Dependants
                        .FirstOrDefaultAsync(d => d.PersonRegNumber == item.PersonRegNumber && d.IsActive == true);

                    if (dependentDetails == null)
                    {
                        await RecordAuditAsync("ValidationError", "SaveDependentItems", $"Dependent not found: {item.PersonRegNumber}");
                        continue;
                    }

                    var checklistItem = new DependentChecklistItem
                    {
                        DependentId = dependentDetails.Id,
                        PersonRegNumber = dependentDetails.PersonRegNumber,
                        Name = dependentDetails.PersonName ?? item.Name,
                        UserId = currentUser.UserId,
                        CauseCampaignpRef = _data.CauseCampaignpRef,
                        Price = item.Price,
                        CustomAmount = item.CustomAmount.GetValueOrDefault(),
                        IsSelected = true,
                        PaymentSessionId = paymentSession.Id,
                        SessionId = session.Id,
                    };

                    await RecordAuditAsync("SelectedDep", "Selected", $"Dependent Reg#: {checklistItem.PersonRegNumber}");
                    _db.DependentChecklistItems.Add(checklistItem);
                    await _db.SaveChangesAsync(ct);
                }

                // Redirect to Stripe checkout
                return Redirect(session.Url);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync("Failed Payment", "FailPayment", $"Payment failed: {ex.Message}", ct);
                return Json(new { success = false, message = "Payment failed." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DonationConfirmation(string session_id, CancellationToken ct)
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
                        await RecordAuditAsync("Invalid Payment Session", "FailPayment", $"Session ID {session_id} not found.", ct);
                        return RedirectToAction("Index", "Home");
                    }

                    var paymentItems = await _db.DependentChecklistItems
                        .Where(d => d.SessionId == session_id && d.IsSelected)
                        .ToListAsync(ct);

                    if (!paymentItems.Any())
                    {
                        await RecordAuditAsync("No Payments Found", "FailPayment", $"No dependents selected for session {session_id}.", ct);
                        return RedirectToAction("Index", "Home");
                    }

                    foreach (var item in paymentItems)
                    {
                        var alreadyExists = await _db.OtherDonationPayment
                            .AnyAsync(p => p.PersonRegNumber == item.PersonRegNumber &&
                                           p.CauseCampaignpRef == paymentSession.CauseCampaignpRef &&
                                           p.OurRef == paymentSession.OurRef, ct);

                        if (alreadyExists)
                            continue; // Skip duplicate

                        var paymentRecord = new OtherDonationPayment
                        {
                            UserId = item.UserId,
                            DependentId = item.DependentId,
                            PersonRegNumber = item.PersonRegNumber,
                            Amount = item.CustomAmount.GetValueOrDefault(),
                            CauseCampaignpRef = paymentSession.CauseCampaignpRef,
                            HasPaid = true,
                            Notes = paymentSession.Reason,
                            DateCreated = DateTime.UtcNow,
                            CreatedBy = paymentSession.Email,
                            OurRef = paymentSession.OurRef
                        };

                        _db.OtherDonationPayment.Add(paymentRecord);
                    }


                    //  Save all payments in one transaction to improve performance
                    await _db.SaveChangesAsync(ct);

                    //  Mark the payment session as paid (outside the loop)
                    paymentSession.IsPaid = true;
                    _db.PaymentSessions.Update(paymentSession);
                    await _db.SaveChangesAsync(ct);

                    ViewBag.Ref = paymentSession.OurRef;
                    return View("DonationConfirmation");
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                await RecordAuditAsync("Failed Payment", "FailPayment", $"Payment failed because of: {ex.Message}", ct);
                return RedirectToAction("Index", "Home");
            }
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

                // Get paid sessions for valid causes
                var paymentSessions = await _db.PaymentSessions
                    .Where(ps => ps.IsPaid == true &&
                                 _db.DonationForNonDeathRelated.Any(d => d.CauseCampaignpRef == ps.CauseCampaignpRef) &&
                                 (string.IsNullOrEmpty(causeFilter) || ps.CauseCampaignpRef == causeFilter))
                    .ToListAsync(ct);

                // Ensure fees are present: infer if missing
                foreach (var session in paymentSessions.Where(s => s.TransactionFees <= 0 && s.TotalAmount > s.Amount))
                {
                    session.TransactionFees = session.TotalAmount - session.Amount;
                }

                var payments = await _db.OtherDonationPayment
                    .Where(p => string.IsNullOrEmpty(causeFilter) || p.CauseCampaignpRef == causeFilter)
                    .ToListAsync(ct);

                var dependents = await _db.Dependants
                    .Where(a => a.IsActive == true || a.IsActive == null)
                    .ToListAsync(ct);

                var causeList = await _db.DonationForNonDeathRelated
                    .Select(d => new { Value = d.CauseCampaignpRef, Text = d.CauseCampaignpRef })
                    .Distinct()
                    .OrderBy(d => d.Text)
                    .ToListAsync(ct);

                ViewBag.CauseList = new SelectList(causeList, "Value", "Text");

                // Totals
                var totalGrossAmount = paymentSessions.Sum(ps => ps.TotalAmount);
                var totalTransactionFees = paymentSessions.Sum(ps => ps.TransactionFees);
                var netTotalAmount = paymentSessions.Sum(ps => ps.Amount); // Net = what was actually donated

                var displayedSessions = new HashSet<string>();

                var paymentDetails = payments.Select(p =>
                {
                    bool isFirstForSession = displayedSessions.Add(p.OurRef);

                    var session = paymentSessions.FirstOrDefault(ps => ps.OurRef == p.OurRef);

                    return new PaymentDetailViewModel
                    {
                        Id = p.Id,
                        CauseCampaignpRef = p.CauseCampaignpRef,
                        Amount = p.Amount,
                        TransactionFees = isFirstForSession ? (session?.TransactionFees ?? 0m) : 0m,
                        TotalAmount = isFirstForSession ? (session?.TotalAmount ?? 0m) : 0m,
                        DateCreated = p.DateCreated,
                        DependentName = dependents.FirstOrDefault(d => d.PersonRegNumber == p.PersonRegNumber && d.IsActive == true)?.PersonName ?? "Unknown",
                        OurRef = p.OurRef
                    };
                }).ToList();

                var adminDashboardViewModel = new AdminPaymentViewModel
                {
                    Payments = paymentDetails,
                    CauseFilter = causeFilter,
                    TotalAmount = totalGrossAmount,
                    TotalTransactionFees = totalTransactionFees
                };

                return View(adminDashboardViewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }

        public IActionResult DownloadPaidPayments()
        {
            var pdf = new ActionAsPdf("PaidPaymentsPDF")
            {
                FileName = "PaidPayments.pdf"
            };

            return pdf;
        }
        [HttpGet]
        public async Task<IActionResult> PaidPaymentsPDF(CancellationToken ct)
        {
            try
            {
                //  Fetch all payment sessions where IsPaid is true (do not filter by paymentId)
                var paymentSessions = await _db.PaymentSessions
                    .Where(ps => ps.IsPaid == true)
                    .ToListAsync(ct);

                if (!paymentSessions.Any())
                {
                    TempData["Error"] = "No paid payments found.";
                    return RedirectToAction("PaymentHistory");
                }

                //  Get the distinct CauseCampaignpRef from all sessions
                var causeCampaignRefs = paymentSessions.Select(ps => ps.CauseCampaignpRef).Distinct().ToList();

                //  Fetch only non-death-related contributions from OtherDonationPayment
                var otherDonations = await _db.OtherDonationPayment
                    .Where(p => causeCampaignRefs.Contains(p.CauseCampaignpRef))
                    .ToListAsync(ct);

                //  Fetch dependent checklist items (Ensure filtering by CauseCampaignpRef & IsSelected = true)
                var dependentChecklistItems = await _db.DependentChecklistItems
                    .Where(d => causeCampaignRefs.Contains(d.CauseCampaignpRef) && d.IsSelected == true)
                    .ToListAsync(ct);

                //  Ensure unique checklist items and join with names from Dependant table
                var dependentIds = dependentChecklistItems.Select(d => d.DependentId).Distinct().ToList();
                var dependents = await _db.Dependants
                    .Where(d => dependentIds.Contains(d.Id))
                    .ToDictionaryAsync(d => d.Id, d => d.PersonName, ct);

                //  Map names from Dependants table
                foreach (var item in dependentChecklistItems)
                {
                    if (dependents.TryGetValue(item.DependentId, out var name))
                    {
                        item.Name = name; // Set correct name
                    }
                }

                //  Ensure distinct contributions (Avoid duplicates)
                var uniqueChecklistItems = dependentChecklistItems
                    .GroupBy(d => d.PersonRegNumber)
                    .Select(g => g.First())
                    .ToList();

                //  Determine if there are non-death-related payments
                bool hasNonDeathPayments = otherDonations.Any();

                if (!hasNonDeathPayments && !uniqueChecklistItems.Any())
                {
                    TempData["Error"] = "No payment details found.";
                    return RedirectToAction("PaymentHistory");
                }

                //  Prepare the ViewModel
                var model = new OtherPaymentsVM
                {
                    PaymentSessions = paymentSessions,
                    PaymentItems = uniqueChecklistItems, // Ensure unique checklist items
                    OtherDonationPayments = otherDonations
                };

                //  Calculate total amounts separately
                ViewBag.TotalAmountPaid = paymentSessions.Sum(ps => ps.TotalAmount);
                ViewBag.TransactionFees = paymentSessions.Sum(ps => ps.TransactionFees);
                ViewBag.NetReceived = ViewBag.TotalAmountPaid - ViewBag.TransactionFees;

                //  Set ViewBag flags for display logic
                ViewBag.HasNonDeathPayments = hasNonDeathPayments;
                ViewBag.HasDependentChecklistItems = uniqueChecklistItems.Any();

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while processing your request.";
                return RedirectToAction("PaymentHistory");
            }
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

                //
                var cas = await _db.DonationForNonDeathRelated.Where(a => a.Id == id).FirstOrDefaultAsync();

                if (cas != null)
                {
                    cas.IsActive = true;              
                    cas.IsDisplayable = true;
                    cas.Status = DonationStatus.Live;
                    _db.DonationForNonDeathRelated.Update(cas);
                    await _db.SaveChangesAsync();

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Other Donation ReOpened", "User Reopened donation for CauseCampaignRefID: " + cas.CauseCampaignpRef, ct);
                    TempData[SD.Success] = "Donation Re-opened successfully.";
                }

                else
                {
                    TempData[SD.Error] = "Error Re-opening donation please check if the Cause exist and try again.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "Error: " + ex.Message.ToString();
                return View();
            }
        }

        public async Task<IActionResult> EndCause(int id, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

         
            //
            var cas = await _db.DonationForNonDeathRelated.FirstOrDefaultAsync(a => a.Id == id);


            cas.IsActive = false;
            cas.Status = DonationStatus.DonationEnded;                
            _db.DonationForNonDeathRelated.Update(cas);
            await _db.SaveChangesAsync();
            TempData[SD.Success] = "Donation Closed!";
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Donation Ended", "User Ended donation for CauseCampaignRefID: " + cas.CauseCampaignpRef, ct);


            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MissedPayment(string Id, CancellationToken ct)
        {
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");
            if (currentUserEmail == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

            try
            {
                DonorVM vm = new DonorVM();

                // Fetch cause details
                var causes = await _db.Cause.FirstOrDefaultAsync(a => a.CauseCampaignpRef == Id);
                decimal missedPaymentFee = causes?.MissPaymentAmount ?? 0;
                decimal fullMemberAmount = causes?.FullMemberAmount ?? 0;
                decimal underAgeAmount = causes?.UnderAgeAmount ?? 0;
                int underAgeLimit = causes?.UnderAge ?? 25;

                vm.Cas = causes;
                vm.MissedPaymentFees = missedPaymentFee;

                int eligibility = 0;
                int underAge = 0;
                decimal adultSum = 0;
                decimal childrenSum = 0;
               

                // Fetch all custom payment reductions once
                var customPayments = await _db.CustomPayment
                    .Where(cp => cp.CauseCampaignpRef == Id)
                    .ToDictionaryAsync(cp => cp.PersonRegNumber, cp => cp.ReduceFees);

                // Fetch dependents
                var dependents = await _db.Dependants
                    .Where(d => d.UserId == currentUser.UserId)
                    .ToListAsync();

                var paidPersonRegNumbers = await _db.Payment
                    .Where(p => p.CauseCampaignpRef == Id && p.HasPaid)
                    .Select(p => p.personRegNumber)
                    .ToListAsync();

                foreach (var d in dependents)
                {
                    var age = AgeHelper.CalculateAge(d.PersonYearOfBirth);
                    decimal price = age >= underAgeLimit ? fullMemberAmount : underAgeAmount;
                    bool hasPaid = paidPersonRegNumbers.Contains(d.PersonRegNumber);

                    decimal finalMissedFee = 0;
                    if (!hasPaid)
                    {
                        customPayments.TryGetValue(d.PersonRegNumber, out finalMissedFee);
                        if (finalMissedFee == 0 && !customPayments.ContainsKey(d.PersonRegNumber))
                        {
                            finalMissedFee = missedPaymentFee;
                        }
                    }

                    vm.DependentsChecklist.Add(new DependentChecklistItem
                    {
                        PersonRegNumber = d.PersonRegNumber,
                        Name = d.PersonName,
                        IsSelected = false,
                        Price = price,
                        Paid = hasPaid,
                        MissedPayment = hasPaid ? 0 : finalMissedFee
                    });

                    if (age >= underAgeLimit)
                    {
                        eligibility++;
                        adultSum += price;
                    }
                    else
                    {
                        underAge++;
                        childrenSum += price;
                    }
                }

                // Fetch group members
                var userDependent = await _db.Dependants
                    .FirstOrDefaultAsync(d => d.PersonRegNumber == currentUser.PersonRegNumber && d.IsActive == true);

                if (userDependent != null)
                {
                    var group = await _db.GroupMembers
                        .FirstOrDefaultAsync(gm => gm.PersonRegNumber == userDependent.PersonRegNumber && gm.Status == "Confirmed");

                    if (group != null)
                    {
                        var groupMembersRaw = await _db.GroupMembers
                            .Where(gm => gm.Status == "Confirmed" && gm.GroupId == group.GroupId)
                            .Join(_db.Dependants, gm => gm.PersonRegNumber, d => d.PersonRegNumber, (gm, d) => new { gm.GroupId, Dep = d })
                            .Where(x => x.Dep.PersonRegNumber != currentUser.PersonRegNumber)
                            .ToListAsync();

                        var groupMembers = new List<DependentChecklistItem>();
                        foreach (var item in groupMembersRaw)
                        {
                            var age = AgeHelper.CalculateAge(item.Dep.PersonYearOfBirth);
                            var price = age >= underAgeLimit ? fullMemberAmount : underAgeAmount;
                            bool hasPaid = paidPersonRegNumbers.Contains(item.Dep.PersonRegNumber);

                            decimal finalMissedFee = 0;
                            if (!hasPaid)
                            {
                                customPayments.TryGetValue(item.Dep.PersonRegNumber, out finalMissedFee);
                                if (finalMissedFee == 0 && !customPayments.ContainsKey(item.Dep.PersonRegNumber))
                                {
                                    finalMissedFee = missedPaymentFee;
                                }
                            }

                            groupMembers.Add(new DependentChecklistItem
                            {
                                GroupId = item.GroupId,
                                PersonRegNumber = item.Dep.PersonRegNumber,
                                Name = item.Dep.PersonName,
                                IsSelected = false,
                                Price = price,
                                Paid = hasPaid,
                                MissedPayment = hasPaid ? 0 : finalMissedFee
                            });
                        }

                        var familyRegNumbers = dependents.Select(d => d.PersonRegNumber).ToList();
                        vm.GroupMembers.AddRange(groupMembers.Where(gm => !familyRegNumbers.Contains(gm.PersonRegNumber)));
                    }
                }

                vm.User = await _db.Users.FirstOrDefaultAsync(u => u.PersonRegNumber == currentUser.PersonRegNumber);

                ViewBag.IsCause = causes != null ? "true" : "false";
                ViewBag.Adult = eligibility;
                ViewBag.Children = underAge;
                ViewBag.Total = adultSum + childrenSum;

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


        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentProcess(DonorVM _data, CancellationToken ct)
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
                    .Concat((_data.GroupMembers ?? new List<DependentChecklistItem>())
                    .Where(g => g.IsSelected && g.CustomAmount.GetValueOrDefault() >= g.Price))
                    .ToList();

                await RecordAuditAsync("Selected Group Members", "Payment",
                    $"Total Group Members Selected: {_data.GroupMembers.Count(g => g.IsSelected)}");

                await _db.SaveChangesAsync();

                if (!selectedItems.Any())
                {
                    ModelState.AddModelError(string.Empty, "No valid items selected for payment.");
                    return View("Index", _data);
                }

                await RecordAuditAsync("SelectedDep", "Selected", $"Total Dependents: {selectedItems.Count()}");

                // Fetch Cause details to get Missed Payment Fee
                var causeDetails = await _db.Cause
                    .FirstOrDefaultAsync(c => c.CauseCampaignpRef == _data.CauseCampaignpRef, ct);

                decimal missedPaymentFee = causeDetails?.MissPaymentAmount ?? 0; // Default to 0 if null

                // Prepare Stripe session
                StripeConfiguration.ApiKey = _stripeSetting.SecretKey;
                var totalAmountInCents = Convert.ToInt32(Math.Round(_data.TotalAmount * 100));
                var ourRef = $"{_data.CauseCampaignpRef}{new Random().Next(0, 9999999):D5}";

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
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
                    SuccessUrl = Url.Action(nameof(Confirmation), "Other", null, Request.Scheme) + "?session_id={CHECKOUT_SESSION_ID}",
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
                    personRegNumber = currentUser.PersonRegNumber,
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

                // Save selected dependents in DependentChecklistItems
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

                    // **Calculate age using AgeHelper**
                    int personAge = AgeHelper.CalculateAge(dependentDetails?.PersonYearOfBirth ?? "");

                    // **Exclude Missed Payment for persons under 25**
                    decimal finalMissedPayment = (personAge >= 25) ? missedPaymentFee : 0;

                    var checklistItem = new DependentChecklistItem
                    {
                        DependentId = dependentDetails?.Id ?? (groupMemberDetails != null ? 0 : 0),
                        PersonRegNumber = item.PersonRegNumber,
                        UserId = dependentDetails.UserId,
                        Name = dependentDetails.PersonName,
                        CauseCampaignpRef = _data.CauseCampaignpRef,
                        Price = item.Price,
                        MissedPayment = finalMissedPayment, // Exclude for under 25
                        CustomAmount = item.CustomAmount.GetValueOrDefault(),
                        IsSelected = true,
                        PaymentSessionId = paymentSession.Id,
                        SessionId = session.Id,
                    };

                    await RecordAuditAsync("SelectedDep", "Selected", $"Processed: {checklistItem.PersonRegNumber} (Age: {personAge}, MissedPayment: {finalMissedPayment})");
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


        //    [HttpPost]
        //    [AllowAnonymous]      
        //    public async Task<IActionResult> PaymentProcess(DonorVM _data, CancellationToken ct)
        //    {
        //        try
        //        {
        //            // Validate session user
        //            var currentUserEmail = HttpContext.Session.GetString("loginEmail");
        //            if (currentUserEmail == null)
        //            {
        //                return RedirectToAction("Index", "Home");
        //            }

        //            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
        //            if (currentUser == null)
        //            {
        //                ModelState.AddModelError(string.Empty, "User not found.");
        //                return View("Index", _data);
        //            }

        //            // Validate selected items
        //            var selectedItems = (_data.DependentsChecklist ?? new List<DependentChecklistItem>())
        //      .Where(d => d.IsSelected && d.CustomAmount.GetValueOrDefault() >= d.Price)
        //      .Concat((_data.GroupMembers ?? new List<DependentChecklistItem>())
        //      .Where(g => g.IsSelected && g.CustomAmount.GetValueOrDefault() >= g.Price))
        //      .ToList();
        //            await RecordAuditAsync("Selected Group Members", "Payment",
        //$"Total Group Members Selected: {_data.GroupMembers.Count(g => g.IsSelected)}");
        //            await _db.SaveChangesAsync();
        //            if (!selectedItems.Any())
        //            {
        //                ModelState.AddModelError(string.Empty, "No valid items selected for payment.");
        //                return View("Index", _data);
        //            }
        //            await RecordAuditAsync("SelectedDep", "Selected", $"Total Dependent)d: {selectedItems.Count()}");
        //            var causeDetails = await _db.Cause
        //        .FirstOrDefaultAsync(c => c.CauseCampaignpRef == _data.CauseCampaignpRef, ct);

        //            decimal missedPaymentFee = causeDetails?.MissPaymentAmount ?? 0; // Default to 0 if null
        //            // Prepare Stripe session
        //            StripeConfiguration.ApiKey = _stripeSetting.SecretKey;
        //            var totalAmountInCents = Convert.ToInt32(Math.Round(_data.TotalAmount * 100));
        //            var ourRef = $"{_data.CauseCampaignpRef}{new Random().Next(0, 9999999):D5}";

        //            var options = new SessionCreateOptions
        //            {
        //                PaymentMethodTypes = new List<string>
        //            {
        //                "card"
        //            },
        //                LineItems = new List<SessionLineItemOptions>
        //        {
        //            new SessionLineItemOptions
        //            {
        //                PriceData = new SessionLineItemPriceDataOptions
        //                {
        //                    Currency = "gbp",
        //                    UnitAmount = totalAmountInCents,
        //                    ProductData = new SessionLineItemPriceDataProductDataOptions
        //                    {
        //                        Name = "Umoja Wetu",
        //                        Description = $"Payment ref#: {ourRef}"
        //                    }
        //                },
        //                Quantity = 1
        //            }
        //        },
        //                Mode = "payment",
        //                SuccessUrl = Url.Action(nameof(DonationConfirmation), "Donation", null, Request.Scheme) + "?session_id={CHECKOUT_SESSION_ID}",
        //                CancelUrl = Url.Action("Index", "Home", null, Request.Scheme)
        //            };

        //            var service = new SessionService();
        //            var session = service.Create(options);

        //            // Save the payment session
        //            var paymentSession = new PaymentSession
        //            {
        //                SessionId = session.Id,
        //                UserId = currentUser.UserId,
        //                Email = currentUserEmail,
        //                Amount = _data.TotalAmount,
        //                personRegNumber = currentUser.PersonRegNumber,
        //                DependentId = currentUser.DependentId,
        //                CauseCampaignpRef = _data.CauseCampaignpRef,
        //                TransactionFees = _data.TransactionFees,
        //                TotalAmount = _data.TotalAmount,
        //                IsPaid = false,
        //                OurRef = ourRef,
        //                Reason = _data.Reason,
        //                DateCreated = DateTime.UtcNow,
        //            };

        //            _db.PaymentSessions.Add(paymentSession);
        //            await _db.SaveChangesAsync(ct);
        //            // Save selected dependents in DependentChecklistItems and create payment records
        //            // Save selected dependents in DependentChecklistItems and create payment records
        //            foreach (var item in selectedItems)
        //            {
        //                if (!item.IsSelected)
        //                {
        //                    continue;
        //                }

        //                // **Check if the person is a dependent**
        //                var dependentDetails = await _db.Dependants
        //     .FirstOrDefaultAsync(d => d.PersonRegNumber == item.PersonRegNumber && d.IsActive == true);

        //                var groupMemberDetails = await _db.GroupMembers
        //                    .FirstOrDefaultAsync(gm => gm.PersonRegNumber == item.PersonRegNumber && gm.Status == "Confirmed");

        //                if (dependentDetails == null && groupMemberDetails == null)
        //                {
        //                    await RecordAuditAsync("ValidationError", "SaveDependentItems",
        //                        $"Person details not found for {item.PersonRegNumber}");
        //                    continue;
        //                }



        //                var checklistItem = new DependentChecklistItem
        //                {
        //                    DependentId = dependentDetails?.Id ?? (groupMemberDetails != null ? 0 : 0),   // Use 0 if null
        //                    PersonRegNumber = item.PersonRegNumber,
        //                    UserId = dependentDetails.UserId,
        //                    Name = dependentDetails.PersonName,
        //                    CauseCampaignpRef = _data.CauseCampaignpRef,                        
        //                    Price = item.Price,
        //                    MissedPayment = missedPaymentFee,
        //                    CustomAmount = item.CustomAmount.GetValueOrDefault(),
        //                    IsSelected = true,
        //                    PaymentSessionId = paymentSession.Id,
        //                    SessionId = session.Id,
        //                };

        //                await RecordAuditAsync("SelectedDep", "Selected", $"Processed: {checklistItem.PersonRegNumber}");
        //                _db.DependentChecklistItems.Add(checklistItem);
        //                await _db.SaveChangesAsync(ct);
        //            }


        //            // Redirect to Stripe checkout
        //            return Redirect(session.Url);
        //        }
        //        catch (Exception ex)
        //        {
        //            await RecordAuditAsync("Failed Payment", "FailPayment", $"Payment failed because of: {ex.Message}", ct);
        //            return Json(new { success = false, message = "Payment failed." });
        //        }
        //    }
        [HttpGet]
        public async Task<IActionResult> Confirmation(string session_id, CancellationToken ct)
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

                    if (!paymentItems.Any())
                    {
                        TempData["Error"] = "No valid dependents or group members found for this payment.";
                        return RedirectToAction("Index", "Home");
                    }

                    var paymentIntentId = session.PaymentIntentId;
                    decimal stripeFee = 0;
                    decimal netAmount = 0;
                    decimal actualAmountReceived = paymentSession.TotalAmount;

                    // Default estimated fee using Stripe UK standard formula (1.5% + £0.20)
                    decimal estimatedFee = Math.Round(paymentSession.Amount * 0.015m + 0.20m, 2);
                    stripeFee = estimatedFee;
                    if (paymentSession.TransactionFees == 0)
                    {
                        paymentSession.TransactionFees = stripeFee;
                    }

                    netAmount = paymentSession.Amount;

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
                                stripeFee = balanceTransaction.Fee / 100m;
                                netAmount = actualAmountReceived - stripeFee;
                            }
                        }
                    }

                    // Save payment records
                    foreach (var item in paymentItems)
                    {
                        var exists = await _db.Payment
                            .AnyAsync(p => p.OurRef == paymentSession.OurRef && p.personRegNumber == item.PersonRegNumber, ct);
                        if (exists) continue;

                        decimal effectiveAmountPaid = item.CustomAmount.GetValueOrDefault();
                        decimal totalCharge = item.Price + item.MissedPayment ?? 0;
                        decimal goodwill = (effectiveAmountPaid > totalCharge)
                            ? effectiveAmountPaid - totalCharge
                            : 0;

                        _db.Payment.Add(new Payment
                        {
                            UserId = item.UserId,
                            DependentId = item.DependentId,
                            personRegNumber = item.PersonRegNumber,
                            Amount = item.Price,
                            GoodwillAmount = goodwill,
                            CauseCampaignpRef = paymentSession.CauseCampaignpRef,
                            HasPaid = true,
                            Notes = paymentSession.Reason,
                            DateCreated = DateTime.UtcNow,
                            CreatedBy = paymentSession.Email,
                            OurRef = paymentSession.OurRef
                        });
                    }

                    await _db.SaveChangesAsync(ct);

                    // Final session update
                    paymentSession.StripeActualFees = stripeFee;
                    paymentSession.StripeNetAmount = netAmount;
                    paymentSession.TransactionFees = stripeFee;
                    paymentSession.IsPaid = true;

                    _db.PaymentSessions.Update(paymentSession);
                    await _db.SaveChangesAsync(ct);

                    ViewBag.Ref = paymentSession.OurRef;
                    ViewBag.TotalPaid = paymentSession.TotalAmount;
                    ViewBag.TransactionFees = paymentSession.TransactionFees;
                    ViewBag.PaidItems = paymentItems;

                    await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "DonationConfirmation",
                        $"Donation payment confirmed for sessionId: {session_id}", ct);

                    return View("Confirmation");
                }

                TempData["Error"] = "Payment verification failed.";
                return RedirectToAction(nameof(Index), "Home");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Payment verification failed.";
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "DonationConfirmation",
                    $"Error processing payment confirmation: {ex.Message}", ct);
                return RedirectToAction(nameof(Index), "Home");
            }
        }


    }
}