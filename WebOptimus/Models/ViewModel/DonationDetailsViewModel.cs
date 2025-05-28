using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models.ViewModel
{
    public class DonationDetailsViewModel
    {
        public int Id { get; set; }
        public string DonationType { get; set; }
        public decimal TargetAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Description { get; set; }
        [NotMapped]
        public string Status { get; set; }

        [NotMapped]
        public bool IsActive { get; set; }

        [NotMapped]
        public string ApprovalNote { get; set; }

        [NotMapped]
        public string DeclineReason { get; set; }


        [NotMapped]
        public string ApprovedBy { get; set; }

        [NotMapped]
        public DateTime? ApprovedByDate { get; set; }


        [NotMapped]
        public string DeclinedBy { get; set; }

        [NotMapped]
        public DateTime? DeclinedDate { get; set; }


        [NotMapped]
        public string DeclinedNote { get; set; }

        [NotMapped]
        public string CreatedBy { get; set; }

        [NotMapped]
        public string CreatedDate { get; set; }
    }
}
