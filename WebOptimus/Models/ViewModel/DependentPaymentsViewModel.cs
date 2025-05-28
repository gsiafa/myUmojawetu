using WebOptimus.Services;

namespace WebOptimus.Models.ViewModel
{
    public class DependentPaymentsViewModel
    {
        public int PaymentId { get; set; }
        public string DependentName { get; set; }
        public decimal Amount { get; set; }
        public decimal TransactionFees { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PaymentDate { get; set; }
    }

    public class DependentPaymentsDashboardViewModel
    {
        public int TotalPayments { get; set; }
        public decimal TotalAmount { get; set; }
        public List<DependentPaymentsViewModel> Payments { get; set; } = new List<DependentPaymentsViewModel>();
    }
}
