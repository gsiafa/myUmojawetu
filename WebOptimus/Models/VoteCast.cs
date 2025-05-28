namespace WebOptimus.Models
{
    public class VoteCast
    {
        public int Id { get; set; }
        public int DependentId { get; set; }
        public int CandidateId { get; set; }
        public DateTime DateVoted { get; set; }

        public Guid UserId { get; set; }

        public string RegistrationNumber { get; set; } // Voter's registration number
        public int ElectionId { get; set; }
    }
}
