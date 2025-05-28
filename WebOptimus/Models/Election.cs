using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models
{
    public class Election
    {
        [Key]
        public int ElectionId { get; set; }
        public string ElectionName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }

        public DateTime DateCreated { get; set; }

        public virtual ICollection<Candidate> Candidates { get; set; }
        public virtual ICollection<VoteCast> Votes { get; set; }
    }
}
