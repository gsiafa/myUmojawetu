using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models
{
    public class GroupMember
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public Guid UserId { get; set; } // Link to User table

        public string PersonRegNumber { get; set; } = string.Empty;
        public int? DependentId { get; set; }
        public string Status { get; set; } // Invited, Confirmed
        public DateTime DateInvited { get; set; }
        public DateTime? DateConfirmed { get; set; }
        public bool IsAdmin { get; set; } = false;
        public bool IsFamilyInvited { get; set; } = false; // New field to track family invitation


        // Navigation Properties
        public PaymentGroup Group { get; set; }
    }
}
