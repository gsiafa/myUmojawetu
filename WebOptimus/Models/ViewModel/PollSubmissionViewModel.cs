namespace WebOptimus.Models.ViewModel
{
    public class PollAnswerSubmissionViewModel
    {
        public int PollId { get; set; }
        public string Question { get; set; }

        // User's answer for the poll
        public string AnswerText { get; set; }
    }

    public class PollSubmissionViewModel
    {
        public List<PollAnswerSubmissionViewModel> Polls { get; set; } = new List<PollAnswerSubmissionViewModel>();
    }

}
