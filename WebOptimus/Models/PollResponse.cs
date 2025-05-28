namespace WebOptimus.Models
{
    public class PollResponse
    {
        public int PollResponseId { get; set; }
        public int PollId { get; set; }
        public string UserId { get; set; }  // Store user identifier (or use UserId based on your setup)
        public string Answer { get; set; }  // Store user's selected answer
        public DateTime ResponseDate { get; set; }

        // Relationships
        public Poll Poll { get; set; }
    }
}
