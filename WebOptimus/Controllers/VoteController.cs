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

namespace WebOptimus.Controllers
{
    public class VoteController : BaseController
    {
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly RequestIpHelper _requestIpHelper;
        private readonly IWebHostEnvironment _hostEnvironment;

        public VoteController(
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



        public async Task<IActionResult> Index()
        {
            // Step 1: Fetch all active elections (IsActive == true)
            var activeElections = await _db.Elections
                .Where(e => e.IsActive == true) // Fetch only active elections (IsActive == true)
                .ToListAsync();

            // Check if active elections exist
            if (activeElections == null || !activeElections.Any())
            {
                // Log a message or handle the case where there are no active elections
                ViewBag.isempty = true;
                return View(); // Return empty view
            }
            ViewBag.isempty = false;
            // Step 2: Fetch candidates that belong to active elections
            var votingViewModels = await _db.Candidates
                .Include(c => c.User) // Include the related User entity
                .Include(c => c.Election) // Include the related Election entity
                .Where(c => activeElections.Select(ae => ae.ElectionId).Contains(c.ElectionId)) // Filter candidates by active election IDs
                .Select(c => new VotingViewModel
                {
                    CandidateId = c.CandidateId,
                    ElectionId = c.ElectionId,
                    ElectionName = c.Election.ElectionName, // Fetch the ElectionName
                    CandidateDescription = c.CandidateDescription,
                    ExistingImagePath = c.ImagePath,
                    ExistingVideoPath = c.VideoPath,
                    UserId = c.UserId,
                    FullName = $"{c.User.FirstName} {c.User.Surname}" // Assumes User entity has FirstName and Surname
                })
                .ToListAsync();

            // Check if any candidates were found
            if (votingViewModels == null || !votingViewModels.Any())
            {
                // Log a message or handle the case where there are no candidates for active elections
                Console.WriteLine("No candidates found for active elections.");
            }

            // Return the view with the mapped view models
            return View(votingViewModels);
        }





        private async Task<List<VotingViewModel>> FetchVotingViewModelsAsync()
        {
            return await _db.Candidates
                .Include(c => c.User) // Include User details
                .Select(c => new VotingViewModel
                {
                    CandidateId = c.CandidateId,
                    CandidateDescription = c.CandidateDescription,
                    ExistingImagePath = c.ImagePath,
                    ExistingVideoPath = c.VideoPath,
                    UserId = c.UserId,
                    FullName = $"{c.User.FirstName} {c.User.Surname}" // Assuming User has FirstName and LastName
                })
                .ToListAsync();
        }

        // Map the fetched candidates to VotingViewModel
        private List<VotingViewModel> MapToVotingViewModel(List<Candidate> candidates)
        {
            return candidates.Select(c => new VotingViewModel
            {
                CandidateId = c.CandidateId,
                ElectionId = c.ElectionId, // Use ElectionId instead of Position
                ElectionName = c.Election?.ElectionName ?? "Unknown Election", // Fetch the ElectionName safely
                CandidateDescription = c.CandidateDescription,
                ExistingImagePath = c.ImagePath,
                ExistingVideoPath = c.VideoPath,
                UserId = c.UserId,
                FullName = $"{c.User.FirstName} {c.User.Surname}" // Assuming User has FirstName and Surname
            }).ToList();
        }







        [HttpPost]
        public async Task<IActionResult> ValidateVote(string regNumber, int candidateId)
        {
            try
            {


                // Find the candidate to get the associated ElectionId
                var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.CandidateId == candidateId);
                if (candidate == null)
                {
                    return Json(new { success = false, message = "Candidate not found." });
                }

                // Check if the registration number exists in the Dependants table
                var dependent = await _db.Dependants.FirstOrDefaultAsync(d => d.PersonRegNumber == regNumber);
                if (dependent == null)
                {
                    return Json(new { success = false, message = "Registration number not found." });
                }

                // Validate the user's age (must be 18 years or older)
                int age = CalculateAge(dependent.PersonYearOfBirth);
                if (age < 18)
                {
                    return Json(new { success = false, message = "You must be 18 years or older to vote." });
                }

                // Check if the user has already voted in this election
                var existingVote = await _db.VoteCasts.FirstOrDefaultAsync(v => v.RegistrationNumber == regNumber && v.ElectionId == candidate.ElectionId);
                if (existingVote != null)
                {
                    return Json(new { success = false, message = "You have already voted in this election." });
                }

                // Register the vote if validation is successful
                var vote = new VoteCast
                {
                    DependentId = dependent.Id,  
                    UserId = dependent.UserId,
                    CandidateId = candidateId,
                    ElectionId = candidate.ElectionId,
                    DateVoted = DateTime.Now,
                    RegistrationNumber = regNumber
                };

                _db.VoteCasts.Add(vote);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Vote successfully cast!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "failed to submit because: "+ ex.Message.ToString() });
            }
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


        //[HttpGet]
        //public async Task<IActionResult> ElectionResults()
        //{
        //    var voteResults = await _db.VoteCasts
        //        .GroupBy(v => v.CandidateId)
        //        .Select(g => new CandidateVotes
        //        {
        //            CandidateName = _db.Candidates.FirstOrDefault(c => c.CandidateId == g.Key).User.FirstName + " " + _db.Candidates.FirstOrDefault(c => c.CandidateId == g.Key).User.Surname,
        //            ElectionName = _db.Elections.FirstOrDefault(e => e.ElectionId == _db.Candidates.FirstOrDefault(c => c.CandidateId == g.Key).ElectionId).ElectionName,
        //            VoteCount = g.Count()
        //        }).ToListAsync();

        //    var model = new ElectionResultsViewModel
        //    {
        //        CandidateVotesList = voteResults
        //    };

        //    return View(model);
        //}

        //[HttpGet]
        //public async Task<IActionResult> ElectionResults()
        //{
        //    // Fetch vote results grouped by candidate
        //    var voteResults = await _db.VoteCasts
        //        .GroupBy(v => v.CandidateId)
        //        .Select(g => new CandidateVotes
        //        {
        //            CandidateName = _db.Candidates
        //                .Where(c => c.CandidateId == g.Key)
        //                .Select(c => c.User.FirstName + " " + c.User.Surname)
        //                .FirstOrDefault(),
        //            ElectionName = _db.Candidates
        //                .Where(c => c.CandidateId == g.Key)
        //                .Select(c => c.Election.ElectionName)
        //                .FirstOrDefault(),
        //            VoteCount = g.Count()
        //        }).ToListAsync();

        //    // Fetch voter details with candidate and election info
        //    var voterDetails = await _db.VoteCasts
        //        .Join(_db.Candidates, vc => vc.CandidateId, c => c.CandidateId, (vc, c) => new { vc, c })
        //        .Join(_db.Elections, temp => temp.c.ElectionId, e => e.ElectionId, (temp, e) => new VoterDetails
        //        {
        //            RegNumber = temp.vc.RegistrationNumber,
        //            CandidateName = temp.c.User.FirstName + " " + temp.c.User.Surname,
        //            Position = e.ElectionName
        //        })
        //        .ToListAsync();

        //    // Create the view model and add vote results and voter details
        //    var model = new ElectionResultsViewModel
        //    {
        //        CandidateVotesList = voteResults,
        //        VoterDetails = voterDetails // Add the voter details to the view model
        //    };

        //    return View(model);
        //}

        [HttpGet]
        public async Task<IActionResult> ElectionResults()
        {
            // Check if election results are enabled in the settings table
            //var setting = await _db.Settings.Where(a => a.EnableElectionResults == true).FirstOrDefaultAsync();



            //if (setting == null || !setting.EnableElectionResults)
            //{
            //    // If no setting is found or EnableElectionResults is false, redirect
            //    return RedirectToAction("Dashboard", "Home"); // Or show a message that it's disabled
            //}

            // Fetch vote results grouped by candidate
            var voteResults = await _db.VoteCasts
                .GroupBy(v => v.CandidateId)
                .Select(g => new CandidateVotes
                {
                    CandidateName = _db.Candidates
                        .Where(c => c.CandidateId == g.Key)
                        .Select(c => c.User.FirstName + " " + c.User.Surname)
                        .FirstOrDefault(),
                    ElectionName = _db.Candidates
                        .Where(c => c.CandidateId == g.Key)
                        .Select(c => c.Election.ElectionName)
                        .FirstOrDefault(),
                    VoteCount = g.Count()
                }).ToListAsync();

            // Fetch voter details with candidate and election info
            var voterDetails = await _db.VoteCasts
                .Join(_db.Candidates, vc => vc.CandidateId, c => c.CandidateId, (vc, c) => new { vc, c })
                .Join(_db.Elections, temp => temp.c.ElectionId, e => e.ElectionId, (temp, e) => new VoterDetails
                {
                    RegNumber = temp.vc.RegistrationNumber,
                    CandidateName = temp.c.User.FirstName + " " + temp.c.User.Surname,
                    Position = e.ElectionName
                })
                .ToListAsync();

            // Create the view model and add vote results and voter details
            var model = new ElectionResultsViewModel
            {
                CandidateVotesList = voteResults,
                VoterDetails = voterDetails // Add the voter details to the view model
            };

            return View(model);
        }
    }
}
