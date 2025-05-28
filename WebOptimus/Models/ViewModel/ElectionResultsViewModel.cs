namespace WebOptimus.Models.ViewModel
{
    public class ElectionResultsViewModel
    {
        public List<CandidateVotes> CandidateVotesList { get; set; }
        public List<VoterDetails> VoterDetails { get; set; } // New property to hold voter details
    }

    public class VoterDetails
    {
        public string RegNumber { get; set; }
        public string CandidateName { get; set; }
        public string Position { get; set; }
    }
    public class CandidateVotes
    {
        public string CandidateName { get; set; }
        public int VoteCount { get; set; }
        public string ElectionName { get; set; }
    }
}
