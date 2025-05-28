namespace WebOptimus.Models
{
    public class PollOption
    {
        public int PollOptionId { get; set; }

             public string OptionText { get; set; }

        // The poll this option belongs to
        public int PollId { get; set; }
        public Poll Poll { get; set; }
    }
}
