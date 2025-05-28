using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebOptimus.Data;
using WebOptimus.Helpers;
using WebOptimus.Models;
using WebOptimus.Models.ViewModel;
using WebOptimus.Services;
using WebOptimus.StaticVariables;


namespace WebOptimus.Controllers
{

    public class PaymentSessionController : BaseController
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
        public PaymentSessionController(IMapper mapper, UserManager<User> userManager,
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

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index(CancellationToken ct)
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
                    return RedirectToAction("Index", "Home");
                }

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "PaymentSessions", "User accessed Payment Sessions page", ct);               

                var sessions = await _db.PaymentSessions
                     .OrderByDescending(ps => ps.DateCreated)
                    .Select(ps => new MovePaymentsViewModel
                    {
                        Id = ps.Id,
                        PaymentSessionId = ps.Id,
                        OurRef = ps.OurRef ?? "N/A",
                        Amount = ps.TotalAmount,
                        UserId = ps.UserId,
                        CauseCampaignpRef = ps.CauseCampaignpRef,
                        TotalAmount = ps.TotalAmount,
                        TransactionFees = ps.TransactionFees,
                        IsPaid = ps.IsPaid,
                        CurrentEmail = ps.Email,
                        CurrentPersonRegNumber = ps.personRegNumber,
                        DateCreated = ps.DateCreated
                    })
                    .ToListAsync(ct);

                return View(sessions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching PaymentSessions: {ex.Message}");
                return RedirectToAction("Index", "Admin");
            }
        }
     
        // 3️⃣ Process Payment Move with Logging & Audit
        [HttpPost]
        public async Task<IActionResult> MovePayment([FromBody] MovePaymentsViewModel model, CancellationToken ct)
        {
            if (model == null || model.PaymentSessionId <= 0 || string.IsNullOrEmpty(model.CurrentPersonRegNumber))
            {
                return BadRequest(new { error = "Invalid request parameters." });
            }

            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return Unauthorized(new { error = "User not authenticated." });
            }

            var currentUser = await _userManager.FindByEmailAsync(email);
            if (currentUser == null)
            {
                return Unauthorized(new { error = "User not found." });
            }

            var session = await _db.PaymentSessions.FirstOrDefaultAsync(a => a.Id == model.PaymentSessionId);
            if (session == null)
            {
                return NotFound(new { error = "Payment session not found." });
            }

            var newUser = await _db.Dependants.FirstOrDefaultAsync(a => a.PersonRegNumber == model.CurrentPersonRegNumber);
            if (newUser == null)
            {
                return NotFound(new { error = "Target user not found." });
            }

            var newUserId = newUser.UserId;
            var newPersonRegNumber = newUser.PersonRegNumber;

            //  Track Changes for Logging
            List<ChangeLog> changeLogs = new List<ChangeLog>();

            void LogChange(string entity, string field, object? oldValue, object? newValue)
            {
                if (!Equals(oldValue, newValue))
                {
                    changeLogs.Add(new ChangeLog
                    {
                        UserId = currentUser.UserId,
                        FieldChanged = $"{entity} - {field}",
                        OldValue = oldValue?.ToString(),
                        NewValue = newValue?.ToString(),
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = email
                    });
                }
            }

            //  Log PaymentSession Changes
            LogChange("PaymentSession", "UserId", session.UserId, newUserId);
            LogChange("PaymentSession", "PersonRegNumber", session.personRegNumber, newPersonRegNumber);

            //  Update PaymentSession
            session.UserId = newUserId;
            session.personRegNumber = newPersonRegNumber;
            _db.PaymentSessions.Update(session);

            //  Update Payments & Log Changes
            var payments = await _db.Payment
                .Where(p => p.OurRef == session.OurRef)
                .ToListAsync(ct);

            foreach (var payment in payments)
            {
                LogChange("Payment", "UserId", payment.UserId, newUserId);
                LogChange("Payment", "PersonRegNumber", payment.personRegNumber, newPersonRegNumber);

                payment.UserId = newUserId;
                payment.personRegNumber = newPersonRegNumber;
            }
            _db.Payment.UpdateRange(payments);

            //  Update DependentChecklistItems & Log Changes
            var checklistItems = await _db.DependentChecklistItems
                .Where(d => d.PaymentSessionId == session.Id)
                .ToListAsync(ct);

            foreach (var item in checklistItems)
            {
                LogChange("DependentChecklist", "UserId", item.UserId, newUserId);
                LogChange("DependentChecklist", "PersonRegNumber", item.PersonRegNumber, newPersonRegNumber);

                item.UserId = newUserId;
                item.PersonRegNumber = newPersonRegNumber;
            }

            _db.DependentChecklistItems.UpdateRange(checklistItems);

            //  Save Change Logs
            await _db.ChangeLogs.AddRangeAsync(changeLogs, ct);
            await _db.SaveChangesAsync(ct);

            //  Audit Log
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "MovePayment",
                $"Admin moved PaymentSession {session.Id} from {session.personRegNumber} to {newPersonRegNumber}", ct);

            return Ok(new { message = "Payment successfully moved to the correct user." });
        }

        [HttpGet]
        public async Task<IActionResult> PaymentDetails(int paymentSessionId, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);
            if (currentUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var session = await _db.PaymentSessions
                .Where(ps => ps.Id == paymentSessionId)
                .FirstOrDefaultAsync(ct);

            if (session == null)
            {
                TempData["Error"] = "Payment session not found.";
                return RedirectToAction("PaymentSessions");
            }

            var dependents = await _db.DependentChecklistItems
                .Where(d => d.PaymentSessionId == session.Id)
                .ToListAsync(ct);

            var payments = await _db.Payment
                .Where(p => p.OurRef == session.OurRef)
                .ToListAsync(ct);

            // Fetch member names from DependentChecklistItems using a join
            var regNumbers = payments.Select(p => p.personRegNumber).Distinct().ToList();

            var memberNames = await _db.DependentChecklistItems
                .Where(d => regNumbers.Contains(d.PersonRegNumber))
                .Select(d => new { d.PersonRegNumber, d.Name })
                .ToListAsync(ct);

            // Map member names to payments
            foreach (var payment in payments)
            {
                var member = memberNames.FirstOrDefault(m => m.PersonRegNumber == payment.personRegNumber);
                if (member != null)
                {
                    payment.MemberName = member.Name;
                }
            }
            var model = new PaymentSessionDetailsViewModel
            {
                PaymentSession = session,
                DependentsChecklist = dependents,
                Payments = payments
            };

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "PaymentDetails", $"Admin viewed PaymentSession {session.Id} details", ct);           
            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> UpdatePaymentSession([FromBody] PaymentSession model, CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                if (email == null)
                {
                    return Unauthorized("User not authenticated.");
                }

                var currentUser = await _userManager.FindByEmailAsync(email);
                if (currentUser == null)
                {
                    return Unauthorized("User not found.");
                }

                var session = await _db.PaymentSessions.FirstOrDefaultAsync(a => a.Id == model.Id, ct);
                if (session == null)
                {
                    return NotFound("Payment session not found.");
                }

                List<ChangeLog> changeLogs = new List<ChangeLog>();

                void LogChange(string field, object? oldValue, object? newValue)
                {
                    if (!Equals(oldValue, newValue))
                    {
                        changeLogs.Add(new ChangeLog
                        {
                            UserId = currentUser.UserId,
                            FieldChanged = field,
                            OldValue = oldValue?.ToString(),
                            NewValue = newValue?.ToString(),
                            ChangeDate = DateTime.UtcNow,
                            ChangedBy = email
                        });
                    }
                }

                // Log changes
                LogChange("UserId", session.UserId, model.UserId);
                LogChange("OurRef", session.OurRef, model.OurRef);
                LogChange("PersonRegNumber", session.personRegNumber, model.personRegNumber);
                LogChange("Email", session.Email, model.Email);
                LogChange("CauseCampaignpRef", session.CauseCampaignpRef, model.CauseCampaignpRef);
                LogChange("Amount", session.Amount, model.Amount);
                LogChange("TransactionFees", session.TransactionFees, model.TransactionFees);
                LogChange("TotalAmount", session.TotalAmount, model.TotalAmount);
                LogChange("IsPaid", session.IsPaid, model.IsPaid);
                LogChange("DateCreated", session.DateCreated, model.DateCreated);

                // Apply changes
                session.UserId = model.UserId;
                session.OurRef = model.OurRef;
                session.personRegNumber = model.personRegNumber;
                session.Email = model.Email;
                session.CauseCampaignpRef = model.CauseCampaignpRef;
                session.Amount = model.Amount;
                session.TransactionFees = model.TransactionFees;
                session.TotalAmount = model.TotalAmount;
                session.IsPaid = model.IsPaid;
                session.DateCreated = model.DateCreated;

                //  Handle dependents movement in payment table
                if (model.IsPaid)
                {
                    // If status is Paid, copy dependents from DependentChecklistItems to Payments (if not exists)
                    var dependents = await _db.DependentChecklistItems
                        .Where(d => d.PaymentSessionId == model.Id)
                        .ToListAsync(ct);

                    foreach (var dependent in dependents)
                    {
                        var existingPayment = await _db.Payment.FirstOrDefaultAsync(p => p.personRegNumber == dependent.PersonRegNumber && p.OurRef == model.OurRef, ct);

                        if (existingPayment == null)
                        {
                            var newPayment = new Payment
                            {
                                Notes = "",                            
                                UserId = model.UserId,                               
                                personRegNumber = dependent.PersonRegNumber,
                                DependentId = dependent.Id,
                                CauseCampaignpRef = dependent.CauseCampaignpRef,
                                Amount = dependent.Price,
                                GoodwillAmount = dependent.CustomAmount ?? 0,
                                HasPaid = true,
                                OurRef = model.OurRef,
                                DateCreated = model.DateCreated,
                                CreatedBy = dependent.Name,  
                               
                            };

                            await _db.Payment.AddAsync(newPayment, ct);
                        }
                    }
                }
                else
                {
                    // If status is Not Paid, remove dependents from Payment table
                    var paymentsToRemove = await _db.Payment
                        .Where(p => p.UserId == model.UserId && p.OurRef == model.OurRef)
                        .ToListAsync(ct);

                    _db.Payment.RemoveRange(paymentsToRemove);
                }

                // Save logs & update database
                await _db.ChangeLogs.AddRangeAsync(changeLogs, ct);
                _db.PaymentSessions.Update(session);
                await _db.SaveChangesAsync(ct);

                // Log the update
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "PaymentSession Update",
                    $"Payment session updated (ID: {model.Id}),(UserId: {model.UserId}),(Reg: {model.personRegNumber}),(email: {model.Email}),(cause ref: {model.CauseCampaignpRef})", ct);

                return Ok(new { message = "Payment session updated successfully." });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new { error = "Database update failed.", details = dbEx.Message });
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "PaymentSession Update", $"Error updating payment session: {ex.Message}", ct);
                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDependent([FromBody] DependentChecklistItem model, CancellationToken ct)
        {
            try
            {
                var dependent = await _db.DependentChecklistItems.FirstOrDefaultAsync(d => d.Id == model.Id, ct);
                if (dependent == null)
                    return NotFound("Dependent not found.");

                var email = HttpContext.Session.GetString("loginEmail");
                var currentUser = await _userManager.FindByEmailAsync(email);

                List<ChangeLog> changeLogs = new List<ChangeLog>();
                void LogChange(string field, object? oldValue, object? newValue)
                {
                    if (!Equals(oldValue, newValue))
                    {
                        changeLogs.Add(new ChangeLog
                        {
                            UserId = currentUser.UserId,
                            FieldChanged = field,
                            OldValue = oldValue?.ToString(),
                            NewValue = newValue?.ToString(),
                            ChangeDate = DateTime.UtcNow,
                            ChangedBy = email
                        });
                    }
                }

                LogChange("Price", dependent.Price, model.Price);
                LogChange("CustomAmount", dependent.CustomAmount, model.CustomAmount);
                LogChange("MissedPayment", dependent.MissedPayment, model.MissedPayment);
                LogChange("Name", dependent.Name, model.Name);
                LogChange("PersonRegNumber", dependent.PersonRegNumber, model.PersonRegNumber);
                LogChange("PaymentSessionId", dependent.PaymentSessionId, model.PaymentSessionId);
                LogChange("CauseCampaignpRef", dependent.CauseCampaignpRef, model.CauseCampaignpRef);

                dependent.Price = model.Price;
                dependent.CustomAmount = model.CustomAmount;
                dependent.MissedPayment = model.MissedPayment;
                dependent.Name = model.Name;
                dependent.PersonRegNumber = model.PersonRegNumber;
                dependent.PaymentSessionId = model.PaymentSessionId;
                dependent.CauseCampaignpRef = model.CauseCampaignpRef;
                await _db.ChangeLogs.AddRangeAsync(changeLogs, ct);
                await _db.SaveChangesAsync(ct);

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "UpdateDependent",
           $"UpdateDependent (ID: {model.Id}),(UserId: {model.UserId}),(Reg: {model.PersonRegNumber}),(cause ref: {model.CauseCampaignpRef})", ct);

                return Ok(new { message = "Dependent updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> DeleteDependent([FromBody] DependentChecklistItem model, CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                if (email == null)
                {
                    return Unauthorized("User not authenticated.");
                }

                var currentUser = await _userManager.FindByEmailAsync(email);
                if (currentUser == null)
                {
                    return Unauthorized("User not found.");
                }

                var dependent = await _db.DependentChecklistItems.FirstOrDefaultAsync(d => d.Id == model.Id, ct);
                if (dependent == null)
                {
                    return NotFound("Dependent not found.");
                }
                var paymentrecord = await _db.Payment.FirstOrDefaultAsync(d => d.personRegNumber == model.PersonRegNumber, ct);
                if (dependent != null)
                {
                    _db.Payment.Remove(paymentrecord);
                }
                // Log deletion
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Dependent Deleted",
                    $"Deleted dependent (ID: {dependent.Id}), (UserId: {dependent.UserId}), (Reg#: {dependent.PersonRegNumber}), (CampaignRef: {dependent.CauseCampaignpRef})", ct);

                _db.DependentChecklistItems.Remove(dependent);
                await _db.SaveChangesAsync(ct);

                return Ok(new { message = "Dependent deleted successfully." });
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Dependent Delete Error",
                    $"Error deleting dependent: {ex.Message}", ct);

                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeletePayment([FromBody] Payment model, CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                if (email == null)
                {
                    return Unauthorized("User not authenticated.");
                }

                var currentUser = await _userManager.FindByEmailAsync(email);
                if (currentUser == null)
                {
                    return Unauthorized("User not found.");
                }

                var dependent = await _db.Payment.FirstOrDefaultAsync(d => d.Id == model.Id, ct);
                if (dependent == null)
                {
                    return NotFound("payment not found.");
                }

                var dependentchecklist = await _db.DependentChecklistItems.FirstOrDefaultAsync(d => d.PersonRegNumber == dependent.personRegNumber, ct);
                if (dependentchecklist != null)
                {
                    _db.DependentChecklistItems.Remove(dependentchecklist);
                }

                // Log deletion
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Dependent Deleted",
                    $"Deleted dependent (ID: {dependent.Id}), (UserId: {dependent.UserId}), (Reg#: {dependent.personRegNumber}), (CampaignRef: {dependent.CauseCampaignpRef})", ct);

                _db.Payment.Remove(dependent);
                await _db.SaveChangesAsync(ct);

                return Ok(new { message = "Payment deleted successfully." });
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Dependent Delete Error",
                    $"Error deleting dependent: {ex.Message}", ct);

                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePayment([FromBody] Payment model, CancellationToken ct)
        {
            try
            {
                var email = HttpContext.Session.GetString("loginEmail");
                if (email == null)
                {
                    return Unauthorized(new { error = "User not authenticated." });
                }

                var currentUser = await _userManager.FindByEmailAsync(email);
                if (currentUser == null)
                {
                    return Unauthorized(new { error = "User not found." });
                }

                var payment = await _db.Payment
                    .FirstOrDefaultAsync(p => p.Id == model.Id, ct);

                if (payment == null)
                {
                    return NotFound(new { error = "Payment not found." });
                }

                List<ChangeLog> changeLogs = new List<ChangeLog>();

                void LogChange(string field, object? oldValue, object? newValue)
                {
                    if (!Equals(oldValue, newValue))
                    {
                        changeLogs.Add(new ChangeLog
                        {
                            UserId = currentUser.UserId,
                            FieldChanged = field,
                            OldValue = oldValue?.ToString(),
                            NewValue = newValue?.ToString(),
                            ChangeDate = DateTime.UtcNow,
                            ChangedBy = email
                        });
                    }
                }

                // Log changes
                LogChange("UserId", payment.UserId, model.UserId);
                LogChange("RegNumber", payment.personRegNumber, model.personRegNumber);
                LogChange("OurRef", payment.OurRef, model.OurRef);
                LogChange("Amount", payment.Amount, model.Amount);
                LogChange("GoodwillAmount", payment.GoodwillAmount, model.GoodwillAmount);
                LogChange("DateCreated", payment.DateCreated, model.DateCreated);

                // Apply changes
                payment.UserId = model.UserId;
                payment.personRegNumber = model.personRegNumber;
                payment.OurRef = model.OurRef;
                payment.Amount = model.Amount;
                payment.GoodwillAmount = model.GoodwillAmount;
                payment.DateCreated = model.DateCreated;

                //  Ensure EF Core tracks changes properly
                _db.Payment.Update(payment);
                await _db.ChangeLogs.AddRangeAsync(changeLogs, ct);
                await _db.SaveChangesAsync(ct);

                // Verify if the payment still exists
                var checkPayment = await _db.Payment.AsNoTracking().FirstOrDefaultAsync(p => p.Id == model.Id, ct);
                if (checkPayment == null)
                {
                    return StatusCode(500, new { error = "Payment disappeared after update!" });
                }

                // Audit Log
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Update Payment",
                    $"Updated Payment (ID: {model.Id}), Amount: {model.Amount}, Goodwill: {model.GoodwillAmount}, Reg: {model.personRegNumber}, Date: {model.DateCreated}", ct);

                return Ok(new { message = "Payment updated successfully." });
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Update Payment",
                    $"Error updating payment (ID: {model.Id}): {ex.Message}", ct);

                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddDuplicatePayment([FromBody] MovePaymentsViewModel model, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

            if (model == null || model.PaymentSessionId <= 0 || string.IsNullOrEmpty(model.NewPersonRegNumber))
            {
                return BadRequest(new { error = "Invalid request parameters." });
            }

            var session = await _db.PaymentSessions.FirstOrDefaultAsync(a => a.Id == model.PaymentSessionId);
            if (session == null)
            {
                return NotFound(new { error = "Payment session not found." });
            }

            var newUser = await _db.Dependants.FirstOrDefaultAsync(a => a.PersonRegNumber == model.NewPersonRegNumber);
            if (newUser == null)
            {
                return NotFound(new { error = "Target user not found." });
            }
            int existingDependents = await _db.DependentChecklistItems
            .CountAsync(d => d.PaymentSessionId == session.Id);

            decimal amountPerPerson = session.Amount / (existingDependents + 1);

            var duplicatePayment = new Payment
            {
                UserId = newUser.UserId,
                personRegNumber = newUser.PersonRegNumber,
                CauseCampaignpRef = session.CauseCampaignpRef,
                Amount = amountPerPerson,
                GoodwillAmount = 0,
                HasPaid = true,
                OurRef = session.OurRef, 
                DateCreated = session.DateCreated,
                CreatedBy = newUser.PersonName
            };

            _db.Payment.Add(duplicatePayment);

            var checklistItem = new DependentChecklistItem
            {
                DependentId = newUser.Id,
                Name = newUser.PersonName,
                PersonRegNumber = newUser.PersonRegNumber,
                Price = amountPerPerson,
                IsSelected = true,
                PaymentSessionId = session.Id,
                SessionId = session.SessionId,
                CustomAmount = session.Amount,
                CauseCampaignpRef = session.CauseCampaignpRef,
                UserId = newUser.UserId,           
                       
            };

            _db.DependentChecklistItems.Add(checklistItem);
            await _db.SaveChangesAsync(ct);
            await RecordAuditAsync(
        currentUser,
        _requestIpHelper.GetRequestIp(),
        "AddDuplicatePayment",
        $"Admin {currentUser.FirstName + " "  + currentUser.Surname} ({currentUser.Email}) added a payment record for {newUser.PersonName} ({newUser.PersonRegNumber}) under reference {session.OurRef}.",
        ct
    );
            return Ok(new { message = "Payment successfully added." });
        }


        [HttpGet]
        public async Task<IActionResult> GetDependents()
        {
            var dependents = await _db.Dependants
                .Select(d => new
                {
                    id = d.Id,
                    name = d.PersonName,
                    email = d.Email ?? "",
                    regNumber = d.PersonRegNumber
                }).ToListAsync();

            return Ok(dependents);
        }
      

    }
}