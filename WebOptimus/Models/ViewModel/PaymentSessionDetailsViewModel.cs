namespace WebOptimus.Models.ViewModel
{
    public class PaymentSessionDetailsViewModel
    {
        public PaymentSession PaymentSession { get; set; }
        public List<DependentChecklistItem> DependentsChecklist { get; set; } = new List<DependentChecklistItem>();
        public List<Payment> Payments { get; set; } = new List<Payment>();
    }
}
