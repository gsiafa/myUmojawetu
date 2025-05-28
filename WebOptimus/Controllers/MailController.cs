using AutoMapper;
using DocumentFormat.OpenXml.Vml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RotativaCore;
using System.Text.Json;
using System.Text;
using WebOptimus.Data;
using WebOptimus.Helpers;
using WebOptimus.Models;
using WebOptimus.Models.ViewModel;
using WebOptimus.Services;
using WebOptimus.StaticVariables;
using Microsoft.AspNetCore.Http.Json;


namespace WebOptimus.Controllers
{
   
    public class MailController : BaseController
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
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IPostmarkClient _postmark;
        public MailController(IMapper mapper, UserManager<User> userManager,
           SignInManager<User> signInManager, IConfiguration configuration, IWebHostEnvironment hostEnvironment, RoleManager<IdentityRole> roleManager, IUserStore<User> userStore,
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
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> Compose(CancellationToken ct)
        {
            try
            {

                var email = HttpContext.Session.GetString("loginEmail");
                if (email == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                var currentUser = await _userManager.FindByEmailAsync(email);

                var users = await _db.Users.Select(u => new SelectListItem
                {
                    Value = u.Email,
                    Text = u.Email
                }).ToListAsync();

                // Option to select all users
                users.Insert(0, new SelectListItem
                {
                    Value = "All",
                    Text = "All Users",
                    Selected = false
                });

             

                return View();
            }
            catch (Exception ex)
            {

                return RedirectToAction("Index", "Admin");

            }
            
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Compose(ComposeMailViewModel model, CancellationToken ct)
        {
            ModelState.Remove("SelectedEmailsString");
            ModelState.Remove("SelectedEmails");
            ModelState.Remove("SendToAll");

            if (!ModelState.IsValid)
            {
                return View(model); // Return view if model validation fails
            }

            try
            {
                //  Load the email template once
                const string pathToFile = @"EmailTemplate/MailCompose.html";
                string htmlBody;
                using (StreamReader reader = System.IO.File.OpenText(pathToFile))
                {
                    htmlBody = await reader.ReadToEndAsync(ct);
                }

                //  Fetch recipient emails (Either all users or manually entered emails)
                List<string> recipientEmails = new();

                if (model.SendToAll)
                {
                    recipientEmails = await _db.Users
                        .Where(u => !string.IsNullOrEmpty(u.Email) && u.IsActive)
                        .Select(u => u.Email)
                        .ToListAsync(ct);
                }
                else if (!string.IsNullOrWhiteSpace(model.SelectedEmailsString))
                {
                    recipientEmails = model.SelectedEmailsString
                        .Split(';')
                        .Select(email => email.Trim())
                        .Where(email => !string.IsNullOrEmpty(email))
                        .ToList();
                }

                if (!recipientEmails.Any())
                {
                    TempData[SD.Error] = "No recipients were selected.";
                    return View(model);
                }

                //  Fetch Postmark API Key from Configuration
                var postmarkApiKey = _configuration["Postmark:ApiKey"];
                if (string.IsNullOrWhiteSpace(postmarkApiKey))
                {
                    TempData[SD.Error] = "Postmark API Key is missing in configuration.";
                    return View(model);
                }

                //  Fetch user details for personalization
                var usersDict = await _db.Users
                    .Where(u => !string.IsNullOrEmpty(u.Email) && u.IsActive)
                    .ToDictionaryAsync(u => u.Email, u => u.FirstName + " " + u.Surname ?? "Member", ct);

                //  Prepare the list of email messages for batch sending
                var emailMessages = recipientEmails.Select(email =>
                {
                    string userName = usersDict.ContainsKey(email) ? usersDict[email] : "Member";

                    return new PostmarkMessage
                    {
                        From = "info@umojawetu.com",
                        To = email,
                        Subject = model.Subject,
                        HtmlBody = htmlBody
                            .Replace("{{userName}}", userName)
                            .Replace("{{messageContent}}", model.Message)
                            .Replace("{{currentYear}}", DateTime.UtcNow.Year.ToString()),
                        TextBody = "Please check your email for important updates.",
                        MessageStream = "broadcast",
                        TrackOpens = true,
                        TrackLinks = "None"
                    };
                }).ToList();

                //  Split emails into batches of 500
                const int BATCH_SIZE = 500;
                var emailBatches = emailMessages
                    .Select((email, index) => new { email, index })
                    .GroupBy(x => x.index / BATCH_SIZE)
                    .Select(group => group.Select(x => x.email).ToList())
                    .ToList();

                List<string> failedEmails = new();
                List<string> successEmails = new();

                using var httpClient = new HttpClient { BaseAddress = new Uri("https://api.postmarkapp.com") };
                httpClient.DefaultRequestHeaders.Add("X-Postmark-Server-Token", postmarkApiKey);

                foreach (var batch in emailBatches)
                {
                    var jsonPayload = JsonSerializer.Serialize(batch, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync("/email/batch", content, ct);
                    var responseContent = await response.Content.ReadAsStringAsync(ct);

                    if (!response.IsSuccessStatusCode)
                    {
                        await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Email API Failure",
                            $"Batch sending failed. Response: {responseContent}", ct);
                        continue; //  Skip to next batch instead of stopping completely
                    }

                    var postmarkResponse = JsonSerializer.Deserialize<List<PostmarkResponseMessage>>(responseContent);
                    if (postmarkResponse == null)
                    {
                        await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Email API Failure",
                            $"Unexpected response format from Postmark: {responseContent}", ct);
                        continue;
                    }

                    failedEmails.AddRange(postmarkResponse.Where(m => m.ErrorCode != 0).Select(m => m.To));
                    successEmails.AddRange(postmarkResponse.Where(m => m.ErrorCode == 0).Select(m => m.To));
                }

                //  Retry sending failed emails in smaller batches
                if (failedEmails.Count > 0)
                {
                    foreach (var failedEmail in failedEmails)
                    {
                        var retryMessage = new PostmarkMessage
                        {
                            From = "info@umojawetu.com",
                            To = failedEmail,
                            Subject = model.Subject,
                            HtmlBody = htmlBody,
                            TextBody = "Please check your email for important updates.",
                            MessageStream = "broadcast",
                            TrackOpens = true,
                            TrackLinks = "None"
                        };

                        var retryPayload = JsonSerializer.Serialize(new List<PostmarkMessage> { retryMessage });
                        var retryContent = new StringContent(retryPayload, Encoding.UTF8, "application/json");
                        var retryResponse = await httpClient.PostAsync("/email/batch", retryContent, ct);

                        if (retryResponse.IsSuccessStatusCode)
                        {
                            successEmails.Add(failedEmail);
                        }
                        else
                        {
                            var retryResponseContent = await retryResponse.Content.ReadAsStringAsync(ct);
                            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Email Retry Failure",
                                $"Retry failed for {failedEmail}. Response: {retryResponseContent}", ct);
                        }
                    }
                }

                //  Log successful emails
                if (successEmails.Any())
                {
                    await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Email Success",
                        $"Successfully sent emails to: {string.Join(", ", successEmails)}", ct);
                }

                //  Log failed emails
                if (failedEmails.Any())
                {
                    await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Email Failure",
                        $"Failed to send emails to: {string.Join(", ", failedEmails)}", ct);
                }

                //  Display appropriate success/failure messages
                if (failedEmails.Count == 0)
                {
                    TempData[SD.Success] = "All emails were sent successfully!";
                }
                else if (successEmails.Count > 0)
                {
                    TempData[SD.Warning] = $"Emails sent successfully to {successEmails.Count} users. However, {failedEmails.Count} failed.";
                }
                else
                {
                    TempData[SD.Error] = "All email sending attempts failed!";
                }

                return RedirectToAction(nameof(Compose));
            }
            catch (Exception ex)
            {
                //  Log error in audit
                var currentUser = await _userManager.GetUserAsync(User);
                var requestIp = _requestIpHelper.GetRequestIp();
                await RecordAuditAsync(currentUser, requestIp, "Email Sending Error",
                    $"Error occurred while sending emails. Subject: {model.Subject}. Error: {ex.Message}", ct);

                TempData[SD.Error] = "An error occurred while sending the emails.";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchEmails(string query, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new List<object>());
            }

            var users = await _db.Users
                .Where(u => u.Email.Contains(query))
                .Select(u => new
                {
                    id = u.Id,
                    email = u.Email
                })
                .ToListAsync(ct);

            return Json(users);
        }

    }
}