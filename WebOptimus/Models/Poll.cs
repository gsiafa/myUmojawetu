namespace WebOptimus.Models
{
    public class Poll
    {
        public int PollId { get; set; }

       
        public string Question { get; set; }

        public string AnswerType { get; set; }

        public List<PollOption> Options { get; set; } = new List<PollOption>();

        public List<PollResponse> Responses { get; set; } = new List<PollResponse>();
    }
}
