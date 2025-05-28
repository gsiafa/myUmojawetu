using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models.ViewModel
{
    public class FinesViewModel
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int DependentId { get; set; }
        public string CauseCampaignpRef { get; set; }
        public decimal Amount { get; set; }

        public decimal MissedPaymentFees { get; set; }
     
        public string PersonName { get; set; }

        public string Email { get; set; }

        public string SearchIdentifier { get; set; }
        public string PersonRegNumber { get; set; }

        [Display(Name = "Transaction Fees (£)")]
        public decimal TransactionFees { get; set; }

        [Display(Name = "Total To Pay (£)")]
        public decimal TotalToPay { get; set; }
        public List<PaymentHistoryViewModel> MissedPayments { get; set; } = new List<PaymentHistoryViewModel>();

        public string MissedPaymentsJson { get; set; }

    }
}
