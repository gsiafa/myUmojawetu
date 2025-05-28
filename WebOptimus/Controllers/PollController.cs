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
using System.Security.Claims;
using WebOptimus.Data;
using WebOptimus.Helpers;
using WebOptimus.Models;
using WebOptimus.Models.ViewModel;
using WebOptimus.Services;
using WebOptimus.StaticVariables;


namespace WebOptimus.Controllers
{
   
    public class PollController : BaseController
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
        public PollController(IMapper mapper, UserManager<User> userManager,
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
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            // Fetch all polls including their options
            var polls = await _db.Poll
                .Include(p => p.Options) // Include related PollOptions
                .ToListAsync(ct);

            // Pass the list of polls to the view
            return View(polls);
        }

        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public IActionResult CreatePoll()
        {
            var model = new PollViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePoll(PollViewModel model, CancellationToken ct)
        {
            ModelState.Remove(nameof(model.Options));
            ModelState.Remove(nameof(model.SelectedAnswer));
            // Custom validation: if the answer type is 'radio' or 'checkbox', ensure options are provided
            if (model.AnswerType == "radio" || model.AnswerType == "checkbox")
            {
                if (model.Options == null || !model.Options.Any())
                {
                    ModelState.AddModelError("Options", "Please provide at least one option for radio buttons or checkboxes.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var poll = new Poll
            {
                Question = model.Question,
                AnswerType = model.AnswerType
            };

            // Add poll options only if 'radio' or 'checkbox' is selected
            if (model.AnswerType == "radio" || model.AnswerType == "checkbox")
            {
                foreach (var option in model.Options)
                {
                    poll.Options.Add(new PollOption { OptionText = option });
                }
            }

            _db.Poll.Add(poll);
            await _db.SaveChangesAsync(ct);

            TempData["SuccessMessage"] = "Poll created successfully!";
            return RedirectToAction("PollList");
        }


        [HttpGet]
        public async Task<IActionResult> ShowPolls()
        {
            // Fetch all polls with options
            var polls = await _db.Poll
                .Include(p => p.Options)
                .ToListAsync();

            var model = polls.Select(poll => new PollViewModel
            {
                PollId = poll.PollId,
                Question = poll.Question,
                AnswerType = poll.AnswerType,
                Options = poll.Options.Select(o => o.OptionText).ToList()
            }).ToList();

            return View(model); // Render polls to the view
        }


        [HttpPost]
        public async Task<IActionResult> SubmitPollResponse(List<PollViewModel> model, CancellationToken ct)
        {
            try
            {
                // Loop through each poll question submitted in the model
                foreach (var poll in model)
                {
                    // Remove validation for options and selected answers
                    ModelState.Remove($"[{model.IndexOf(poll)}].Options");
                    ModelState.Remove($"[{model.IndexOf(poll)}].SelectedAnswer");
                    ModelState.Remove($"[{model.IndexOf(poll)}].SelectedAnswers");

                    // Check if the poll exists in the database
                    var existingPoll = await _db.Poll
                        .Include(p => p.Options)
                        .FirstOrDefaultAsync(p => p.PollId == poll.PollId, ct);

                    if (existingPoll == null)
                    {
                        continue; // Skip if poll doesn't exist
                    }

                    // For checkboxes (multiple answers)
                    if (poll.AnswerType == "checkbox" && poll.SelectedAnswers != null)
                    {
                        foreach (var selectedAnswer in poll.SelectedAnswers)
                        {
                            var response = new PollResponse
                            {
                                PollId = poll.PollId,
                                Answer = selectedAnswer,
                                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier), // Get the current user's ID
                                ResponseDate = DateTime.UtcNow
                            };

                            _db.PollResponse.Add(response);
                        }
                    }
                    // For radio buttons, input, or textarea types
                    else if (!string.IsNullOrEmpty(poll.SelectedAnswer))
                    {
                        var response = new PollResponse
                        {
                            PollId = poll.PollId,
                            Answer = poll.SelectedAnswer,
                            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier), // Get the current user's ID
                            ResponseDate = DateTime.UtcNow
                        };

                        _db.PollResponse.Add(response);
                    }
                }

                // Save all responses to the database at once
                await _db.SaveChangesAsync(ct);

                TempData[SD.Success] = "Poll responses submitted successfully!";
                return RedirectToAction("ResponseSubmitted");
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "An error occurred while submitting your poll responses.";
                return RedirectToAction("PollList");
            }
        }

        public IActionResult ResponseSubmitted()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> PollResults(CancellationToken ct)
        {
            // Fetch all polls and their responses
            var polls = await _db.Poll
                .Include(p => p.Options)
                .Include(p => p.Responses)
                .ToListAsync(ct);

            var pollResults = new List<PollResultsViewModel>();

            foreach (var poll in polls)
            {
                var result = new PollResultsViewModel
                {
                    PollId = poll.PollId,
                    Question = poll.Question,
                    AnswerType = poll.AnswerType,
                    Options = poll.Options.Select(o => o.OptionText).ToList(),
                    ResponseCounts = new Dictionary<string, int>(),
                    Responses = new List<string>() // Store individual user responses
                };

                // Initialize response counts for each option
                foreach (var option in result.Options)
                {
                    result.ResponseCounts[option] = 0;
                }

                // Count and collect individual responses based on the answer type
                foreach (var response in poll.Responses)
                {
                    // Collect individual responses
                    result.Responses.Add(response.Answer);

                    // Count responses for radio/checkbox type
                    if (poll.AnswerType == "radio" || poll.AnswerType == "checkbox")
                    {
                        if (result.ResponseCounts.ContainsKey(response.Answer))
                        {
                            result.ResponseCounts[response.Answer]++;
                        }
                    }
                }

                // For input/textarea types, we group custom answers under "Custom Answers"
                if (poll.AnswerType == "input" || poll.AnswerType == "textarea")
                {
                    result.ResponseCounts["Custom Answers"] = poll.Responses.Count;
                }

                pollResults.Add(result);
            }

            return View(pollResults);
        }


        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> PollList(CancellationToken ct)
        {
            // Retrieve all polls from the database
            var polls = await _db.Poll
                .Include(p => p.Options) // Include poll options
                .ToListAsync(ct);

            // Convert polls to view model
            var model = polls.Select(p => new PollViewModel
            {
                PollId = p.PollId,
                Question = p.Question,
                AnswerType = p.AnswerType,
                Options = p.Options.Select(o => o.OptionText).ToList()
            }).ToList();

            return View(model); // Pass the model to the view
        }
        [HttpGet]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> EditPoll(int id, CancellationToken ct)
        {
            // Fetch the poll by ID
            var poll = await _db.Poll
                .Include(p => p.Options)
                .FirstOrDefaultAsync(p => p.PollId == id, ct);

            // If poll not found, return NotFound result
            if (poll == null)
            {
                return NotFound();
            }

            // Map the poll to the PollViewModel
            var model = new PollViewModel
            {
                PollId = poll.PollId,
                Question = poll.Question,
                AnswerType = poll.AnswerType,
                Options = poll.Options.Select(o => o.OptionText).ToList()
            };

            // Return the view with the model
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleList.GeneralAdmin)]
        public async Task<IActionResult> EditPoll(PollViewModel model, CancellationToken ct)
        {
            try
            {
                // Remove unnecessary ModelState validations
                ModelState.Remove(nameof(model.Options));
                ModelState.Remove(nameof(model.SelectedAnswer));

                // Check if the model state is valid
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Fetch the poll to be updated
                var poll = await _db.Poll
                    .Include(p => p.Options)
                    .FirstOrDefaultAsync(p => p.PollId == model.PollId, ct);

                if (poll == null)
                {
                    return NotFound();
                }

                // Update poll question and answer type
                poll.Question = model.Question;
                poll.AnswerType = model.AnswerType;

                // Clear existing options and add new ones (only if AnswerType is not 'input' or 'textarea')
                if (model.AnswerType != "input" && model.AnswerType != "textarea")
                {
                    // Clear existing poll options
                    poll.Options.Clear();

                    // Add only non-empty options
                    foreach (var option in model.Options.Where(o => !string.IsNullOrWhiteSpace(o)))
                    {
                        poll.Options.Add(new PollOption { OptionText = option });
                    }
                }
                else
                {
                    // If the AnswerType is 'input' or 'textarea', ensure that Options are cleared
                    poll.Options.Clear();
                }

                // Save changes to the database
                _db.Poll.Update(poll);
                await _db.SaveChangesAsync(ct);

                // Redirect to PollList after editing
                TempData[SD.Success] = "Poll updated successfully!";
                return RedirectToAction("PollList");
            }
            catch (Exception ex)
            {
                TempData[SD.Error] = "Failed to update the poll.";
                return RedirectToAction("PollList");
            }
        }


    }
}