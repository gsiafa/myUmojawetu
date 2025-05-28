using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models.ViewModel
{
    public class DonorVM
    {
        public DonorViewModel DonorViewModel { get; set; }

        public List<Cause> Causes { get; set; } = new List<Cause>(); // Changed to List<Cause>
        public List<DonationForNonDeathRelated> Donations { get; set; } = new List<DonationForNonDeathRelated>(); // Add Donations list
         
        public decimal? MissedPaymentFees { get; set; }
        public User User { get; set; }

        [NotMapped]
        public decimal GoodWillAmount { get; set; }

        public Cause Cas { get; set; }

        public DonationForNonDeathRelated DonationForNonDeathRelatedModel { get; set; }
        public string DeceasedPhotoPath { get; set; } = string.Empty; // New property
        public decimal TransactionFees { get; set; }

        public decimal TotalAmount { get; set; }

        public string CauseCampaignpRef { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }
        //public bool IsAnonymous { get; set; }
        public string? Reason { get; set; } = string.Empty;
        // New property to hold the list of donors for each cause
        public List<Payment> Donors { get; set; } = new List<Payment>();
        public List<OtherDonationPayment> OtherDonationPayments { get; set; } = new List<OtherDonationPayment>();
        public List<DependentChecklistItem> DependentsChecklist { get; set; } = new List<DependentChecklistItem>();
        public List<DependentChecklistItem> GroupMembers { get; set; } = new List<DependentChecklistItem>();
        [NotMapped]
        public decimal? overpaid { get; set; }
        public string PersonRegNumber { get; set; } = string.Empty ;
        public bool IsDisplayable { get; set; }
        public bool AllEligibleDependentsPaid { get; set; }
    }

}
