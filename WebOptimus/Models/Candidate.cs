using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models
{
    public class Candidate
    {
        public int CandidateId { get; set; }

        // Use string to match the User.Id primary key type
        public string UserId { get; set; }

        public string CandidateDescription { get; set; }

        // Foreign Key for Election
        [ForeignKey("Election")]
        public int ElectionId { get; set; }

        public string? ImagePath { get; set; }
        public string? VideoPath { get; set; }

        public DateTime DateRegistered { get; set; } = DateTime.Now;

        // Define the navigation property to User
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        // Navigation property for Election
        public virtual Election Election { get; set; }

        
      
    }
}
