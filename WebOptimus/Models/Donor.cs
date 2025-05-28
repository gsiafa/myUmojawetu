using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models
{
    public class Donor
    {
        [Key]
        public int Id { get; set; }

        public Guid UserId { get; set; } = Guid.Empty;

        public string FirstName { get; set; }


        public string LastName { get; set; }

        public decimal Amount { get; set; }

        public string Email { get; set; }

        public bool IsAnonymous { get;set; }
        public string? HasPaid { get; set; } = string.Empty;
        public string CauseCampaignpRef { get; set; } = string.Empty;


        public DateTime DateCreated { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? UpdateOn { get; set; }

    }
}
