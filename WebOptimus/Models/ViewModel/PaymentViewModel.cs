using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class PaymentViewModel
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int DependentId { get; set; }
        public string PersonRegNumber { get; set; } = string.Empty;
        public string CauseCampaignpRef { get; set; }
        public decimal Amount { get; set; }
        public decimal TransactionFees { get; set; }
        public decimal TotalAmount { get; set; }
        public bool HasPaid { get; set; }
        public string? OurRef { get; set; }
        public string? Notes { get; set; }
        public DateTime DateCreated { get; set; }
        public string CreatedBy { get; set; }
        public string DateRaised { get; set; }





    }
}
