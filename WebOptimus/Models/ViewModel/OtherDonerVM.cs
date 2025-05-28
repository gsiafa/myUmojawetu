using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models.ViewModel
{
    public class OtherDonerVM
    {

        public List<DonationForNonDeathRelated> Donations { get; set; } = new List<DonationForNonDeathRelated>(); // Add Donations list

        public User User { get; set; }

  

        public DonationForNonDeathRelated Cas { get; set; }

        public decimal TransactionFees { get; set; }

        public decimal TotalAmount { get; set; }

        public string CauseCampaignpRef { get; set; } = string.Empty;
        [Required]
        public decimal Amount { get; set; }
        public string? Reason { get; set; } = string.Empty;
        public List<OtherDonationPayment> OtherDonationPayments { get; set; } = new List<OtherDonationPayment>();
        public List<DependentChecklistItem> DependentsChecklist { get; set; } = new List<DependentChecklistItem>();
        public List<DependentChecklistItem> GroupMembers { get; set; } = new List<DependentChecklistItem>();
       
        public bool IsDisplayable { get; set; }
        public bool AllEligibleDependentsPaid { get; set; }
    }

}
