using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models
{
    public class DonationForNonDeathRelated
    {
        [Key]
        public int Id { get; set; }
    
        public string Summary { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? MinmumAmount { get; set; }
        public decimal TargetAmount { get; set; }

        public bool IsActive { get; set; } = false;

        public bool IsDisplayable { get; set; } = false;

        public string CauseCampaignpRef { get; set; } = string.Empty;

        public DateTime DateCreated { get; set; }
        public int? DependentId { get; set; }
        public string PersonRegNumber { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? ClosedDate { get; set; }

        public string? Status { get; set; } = string.Empty;
        public string? ApprovedBy { get; set; } = string.Empty;

        public DateTime? ApprovedDate { get; set; }
        public string? DeclinedBy { get; set; } = string.Empty;

        public DateTime? DeclinedDate { get; set; }

        public string? ApprovalOrDeclinerNote { get; set; } = string.Empty;

        public bool IsMandatory { get; set; } = false;
        [NotMapped] // Exclude from database mapping
        public decimal AmountRaised { get; set; } = 0m;
    }
}
