using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebOptimus.Models.ViewModel
{
    public class MovePaymentsViewModel
    {
        public int Id { get; set; }
        public int PaymentSessionId { get; set; }
        public string OurRef { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CurrentEmail { get; set; } = string.Empty;
        public string CurrentPersonRegNumber { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public string CauseCampaignpRef { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public bool IsPaid { get; set; }
        public decimal TransactionFees { get; set; }
        public Guid UserId { get; set; }
        public string NewUserId { get; set; } = string.Empty;
        public string NewPersonRegNumber { get; set; } = string.Empty;
        public List<SelectListItem> Users { get; set; } = new();
    }
}
