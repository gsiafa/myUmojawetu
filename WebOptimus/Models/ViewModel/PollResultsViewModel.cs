namespace WebOptimus.Models.ViewModel
{
    public class PollResultsViewModel
    {
        public int PollId { get; set; }
        public string Question { get; set; }
        public string AnswerType { get; set; }
        public List<string> Options { get; set; } // Poll options
        public Dictionary<string, int> ResponseCounts { get; set; } // Count of responses for each option
        public List<string> Responses { get; set; } // Individual responses from users

        public PollResultsViewModel()
        {
            Options = new List<string>();
            ResponseCounts = new Dictionary<string, int>();
            Responses = new List<string>();
        }
    }
}
