namespace WebOptimus.Models.ViewModel
{
    public class VotingViewModel
    {
        public int CandidateId { get; set; } // The unique ID of the candidate
        public string UserId { get; set; } // The UserId associated with the candidate
        public string Position { get; set; } // The position the candidate is running for
        public string CandidateDescription { get; set; } // Description of the candidate
        public string? ExistingImagePath { get; set; } // Path to the candidate's image
        public string? ExistingVideoPath { get; set; } // Path to the candidate's video
        public string FullName { get; set; }
        public int ElectionId { get; set; }
        public string ElectionName { get; set; }
    }
}
