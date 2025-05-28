using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebOptimus.Data;
using WebOptimus.Helpers;
using WebOptimus.Models;
using WebOptimus.Models.ViewModel;
using WebOptimus.StaticVariables;
using System.Globalization;
using System.Linq;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Security.Cryptography;

namespace WebOptimus.Controllers
{
    public class CandidateController : BaseController
    {
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly RequestIpHelper _requestIpHelper;
        private readonly IWebHostEnvironment _hostEnvironment;

        public CandidateController(
            IMapper mapper,
            UserManager<User> userManager,
            IWebHostEnvironment hostEnvironment,
            ApplicationDbContext db,
            RequestIpHelper requestIpHelper)
            : base(userManager, db, requestIpHelper)
        {
            _hostEnvironment = hostEnvironment;
            _mapper = mapper;
            _userManager = userManager;
            _db = db;
            _requestIpHelper = requestIpHelper;
        }


        [HttpGet]
        public IActionResult Index()
        {
            // Fetch candidates including their related User and Election entities
            var candidates = _db.Candidates
                .Include(c => c.User)  // Include the related User entity
                .Include(c => c.Election)  // Include the related Election entity
                .Select(c => new CandidateViewModel
                {
                    CandidateId = c.CandidateId,
                    ElectionId = c.ElectionId,
                    ElectionName = c.Election.ElectionName, // Fetch ElectionName
                    CandidateDescription = c.CandidateDescription,
                    ExistingImagePath = c.ImagePath,
                    ExistingVideoPath = c.VideoPath,
                    UserId = c.UserId,
                    FullName = $"{c.User.FirstName} {c.User.Surname}" // Assuming User has FirstName and Surname
                })
                .ToList();

            return View(candidates); // Pass the list of candidates to the view
        }




        [HttpGet]
        [Authorize]
        public IActionResult AddCandidate()
        {
            // Fetch users for dropdown, using the correct Id and other fields
            var users = _db.Users
                .Select(u => new User
                {
                    Id = u.Id, // Use the IdentityUser Id, which is a string
                    FirstName = u.FirstName,
                    Surname = u.Surname
                })
                .ToList();

            // Fetch elections for the Position dropdown
            var elections = _db.Elections
                .Select(e => new SelectListItem
                {
                    Value = e.ElectionId.ToString(), // ElectionId is the value
                    Text = e.ElectionName // ElectionName is the text in the dropdown
                })
                .ToList();

            // Populate the ViewModel
            var model = new AddCandidateViewModel
            {
                Users = users, // List of Users
                Elections = elections // List of Elections for dropdown
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCandidate(AddCandidateViewModel model, CancellationToken ct)
        {
            // Fetch elections for the ElectionId dropdown
            var elections = _db.Elections
                .Select(e => new SelectListItem
                {
                    Value = e.ElectionId.ToString(),
                    Text = e.ElectionName
                })
                .ToList();

            model.Elections = elections;

            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

            try
            { 
          
                ModelState.Remove(nameof(model.Users));
                ModelState.Remove(nameof(model.Elections));
                ModelState.Remove(nameof(model.UserId));               
                ModelState.Remove(nameof(model.ImageFile));
                ModelState.Remove(nameof(model.VideoFile));
                ModelState.Remove(nameof(model.ElectionId));

             
                if (ModelState.IsValid)
                {
                    // Process file uploads (image and video) and save paths
                    var imagePath = await SaveFile(model.ImageFile, "images");
                    var videoPath = await SaveFile(model.VideoFile, "videos");

                    // Create the new candidate
                    var candidate = new Candidate
                    {
                        UserId = model.UserId,
                        ElectionId = model.ElectionId, // Use ElectionId from the dropdown
                        CandidateDescription = model.CandidateDescription,
                        ImagePath = imagePath,
                        VideoPath = videoPath,
                        DateRegistered = DateTime.Now
                    };

                    _db.Candidates.Add(candidate);
                    await _db.SaveChangesAsync();

                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "AddCandidate", "Candidate added successfully. ID# " + model.UserId, ct);

                    TempData[SD.Success] = "Candidate added successfully.";
                    return RedirectToAction(nameof(Index));
                }

                // Re-populate the dropdown if validation fails
                model.Users = _db.Users
                    .Select(u => new User
                    {
                        UserId = u.UserId,
                        FirstName = u.FirstName,
                        Surname = u.Surname
                    })
                    .ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Add Candidate", "ERROR: failed to add candidate because: " + ex.Message.ToString(), ct);
                TempData[SD.Error] = "Error adding Candidate.";

                return RedirectToAction(nameof(Index));
            }
        }


        private async Task<string> SaveFile(IFormFile file, string folder)
        {
            // Create a folder in the 'wwwroot/media' path
            string uploadFolder = Path.Combine(_hostEnvironment.WebRootPath, folder);

            // If the folder does not exist, create it
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // Generate a unique file name to prevent name collisions
            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(uploadFolder, uniqueFileName);

            // Save the file to the specified folder
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Return the relative path to the file (for later use in your database)
            return Path.Combine(folder, uniqueFileName).Replace("\\", "/"); // Ensure the path is web-friendly
        }
       

        public IActionResult GetUserDetails(string userId)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == userId.Trim());


            if (user == null)
            {
                return NotFound();
            }

            // Calculate age based on the YearOfBirth
            var age = CalculateAge(user.PersonYearOfBirth);

            return Json(new
            {
                regNumber = user.PersonRegNumber,
                yearofbirth = user.PersonYearOfBirth,
            });
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
        [HttpGet]
        [Authorize]
        public IActionResult EditCandidate(int id)
        {
            // Fetch the candidate by id
            var candidate = _db.Candidates.FirstOrDefault(c => c.CandidateId == id);
            if (candidate == null)
            {
                return NotFound();
            }

            // Fetch users for dropdown
            var users = _db.Users
                .Select(u => new SelectListItem
                {
                    Value = u.Id, // IdentityUser Id
                    Text = u.FirstName + " " + u.Surname,
                    Selected = u.Id == candidate.UserId // Mark the user as selected
                })
                .ToList();

            // Fetch the user associated with the candidate
            var user = _db.Users.FirstOrDefault(u => u.Id == candidate.UserId);

            // Fetch available elections for the dropdown
            var elections = _db.Elections
                .Select(e => new SelectListItem
                {
                    Value = e.ElectionId.ToString(),
                    Text = e.ElectionName,
                    Selected = e.ElectionId == candidate.ElectionId // Mark the election as selected
                })
                .ToList();

            // Populate the ViewModel with the existing data
            var model = new EditCandidateViewModel
            {
                CandidateId = candidate.CandidateId,
                UserId = candidate.UserId,
                ElectionId = candidate.ElectionId, // Ensure ElectionId is set
                CandidateDescription = candidate.CandidateDescription,
                RegNumber = user?.PersonRegNumber,  // Fetch and populate RegNumber
                YearOfBirth = int.Parse(user?.PersonYearOfBirth ?? "0"),  // Fetch and populate YearOfBirth
                ExistingImagePath = candidate.ImagePath, // Existing image
                ExistingVideoPath = candidate.VideoPath, // Existing video
                EditUsers = users, // Users dropdown
                Elections = elections // Elections dropdown
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditCandidate(EditCandidateViewModel model, CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

            try
            {
                ModelState.Remove(nameof(model.EditUsers));
                ModelState.Remove(nameof(model.Elections));
                ModelState.Remove(nameof(model.ExistingImagePath));
                ModelState.Remove(nameof(model.ExistingVideoPath));
                ModelState.Remove(nameof(model.ImageFile));
                ModelState.Remove(nameof(model.VideoFile));

                if (ModelState.IsValid)
                {
                    // Fetch the existing candidate from the database
                    var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.CandidateId == model.CandidateId);
                    if (candidate == null)
                    {
                        return NotFound();
                    }

                    // Update the candidate's information
                    candidate.UserId = model.UserId;
                    candidate.CandidateDescription = model.CandidateDescription;
                    candidate.ElectionId = model.ElectionId;

                    // Handle image upload, if any
                    if (model.ImageFile != null && model.ImageFile.Length > 0)
                    {
                        var imagePath = await SaveFile(model.ImageFile, "images");
                        candidate.ImagePath = imagePath; // Update image path if new image is uploaded
                    }
                    else
                    {
                        // Preserve existing image if no new image is uploaded
                        candidate.ImagePath = model.ExistingImagePath;
                    }

                    // Handle video upload, if any
                    if (model.VideoFile != null && model.VideoFile.Length > 0)
                    {
                        var videoPath = await SaveFile(model.VideoFile, "videos");
                        candidate.VideoPath = videoPath; // Update video path if new video is uploaded
                    }
                    else
                    {
                        // Preserve existing video if no new video is uploaded
                        candidate.VideoPath = model.ExistingVideoPath;
                    }

                    // Save changes to the database
                    _db.Candidates.Update(candidate);
                    await _db.SaveChangesAsync();

                    // Record the update in the audit log
                    await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "EditCandidate", $"Candidate edited successfully. ID# {model.UserId}", ct);

                    TempData[SD.Success] = "Candidate updated successfully.";
                    return RedirectToAction(nameof(Index));
                }

                // Re-populate the dropdowns if validation fails
                model.EditUsers = _db.Users
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id,
                        Text = u.FirstName + " " + u.Surname,
                        Selected = u.Id == model.UserId
                    })
                    .ToList();

                model.Elections = _db.Elections
                    .Select(e => new SelectListItem
                    {
                        Value = e.ElectionId.ToString(),
                        Text = e.ElectionName,
                        Selected = e.ElectionId == model.ElectionId
                    })
                    .ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "EditCandidate", "ERROR: failed to edit candidate because: " + ex.Message.ToString(), ct);
                TempData[SD.Error] = "Error editing Candidate.";
                return RedirectToAction(nameof(Index));
            }
        }


        // GET: Show the form to add an election
        [HttpGet]
        public async Task<IActionResult> AddElection()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Election()
        {
            var elections = await _db.Elections.ToListAsync();
            return View(elections);
        }

        // POST: Handle the form submission
        [HttpPost]
        public async Task<IActionResult> AddElection(ElectionViewModel model)
        {
            if (ModelState.IsValid)
            {
                var election = new Election
                {
                    ElectionName = model.ElectionName,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    IsActive = model.IsActive,
                    DateCreated = DateTime.UtcNow
                };

                _db.Elections.Add(election);
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = "Election created successfully!";
                return RedirectToAction(nameof(Election)); 
            }

            return View(model); 
        }

        // GET: Edit Election
        [HttpGet]
        public IActionResult EditElection(int id)
        {
            var election = _db.Elections.FirstOrDefault(e => e.ElectionId == id);

            if (election == null)
            {
                return NotFound();
            }

            var model = new ElectionViewModel
            {
                ElectionId = election.ElectionId,
                ElectionName = election.ElectionName,
                StartDate = election.StartDate,
                EndDate = election.EndDate,
                IsActive = election.IsActive
            };

            return View(model);
        }

        // POST: Edit Election
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditElection(ElectionViewModel model)
        {
            if (ModelState.IsValid)
            {
                var election = _db.Elections.FirstOrDefault(e => e.ElectionId == model.ElectionId);

                if (election == null)
                {
                    return NotFound();
                }

                // Update the election details
                election.ElectionName = model.ElectionName;
                election.StartDate = model.StartDate;
                election.EndDate = model.EndDate;
                election.IsActive = model.IsActive;

                _db.Elections.Update(election);
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = "Election updated successfully!";
                return RedirectToAction(nameof(Election));
            }

            return View(model);
        }

        // POST: Delete Candidate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCandidate(int id)
        {
            var candidate = await _db.Candidates.FindAsync(id);

            if (candidate == null)
            {
                return NotFound();
            }

            _db.Candidates.Remove(candidate);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Candidate deleted successfully!";
            return RedirectToAction(nameof(Election));
        }

    }
}
