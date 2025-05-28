using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models
{
    public class PaymentSession
    {
        public int Id { get; set; }
        public string SessionId { get; set; }
        public string Email { get; set; }
        public decimal Amount { get; set; }

        public string? OurRef { get; set; }
        public Guid UserId { get; set; } = Guid.Empty;
        public List<DependentChecklistItem> DependentsChecklist { get; set; } = new List<DependentChecklistItem>();
        public string CauseCampaignpRef { get; set; } = string.Empty;
        public decimal TransactionFees { get; set; }

        public decimal? StripeActualFees { get; set; }
        public decimal? StripeNetAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Reason { get; set; } = string.Empty;
        public int? DependentId { get; set; }
        public bool IsPaid { get; set; }

        public string personRegNumber { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }

        [NotMapped]
        public int? CurrentDependentId { get; set; }

    }
}
