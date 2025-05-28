using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class OtherDonationDetailsViewModel
    {

        public int Id { get; set; }

        public string Summary { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? MinmumAmount { get; set; }

        [DataType(DataType.Currency)]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = true)]
        public decimal TargetAmount { get; set; }

        public bool IsActive { get; set; } = false;

        public bool IsDisplayable { get; set; } = false;

        public string CauseCampaignpRef { get; set; } = string.Empty;

        public DateTime DateCreated { get; set; }
        public int? DependentId { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? ClosedDate { get; set; }

        public string? Status { get; set; } = string.Empty;
        public string? ApprovedBy { get; set; } = string.Empty;

        public DateTime? ApprovedDate { get; set; }
        public string? DeclinedBy { get; set; } = string.Empty;

        public DateTime? DeclinedDate { get; set; }

        public string? ApprovalOrDeclinerNote { get; set; } = string.Empty;


        public string StartDateAsString { get; set; }
        public string ClosedDateAsString { get; set; }
        public string DateCreatedAsString { get; set; }
    }
}
