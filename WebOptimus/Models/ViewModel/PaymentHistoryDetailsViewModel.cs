namespace WebOptimus.Models.ViewModel
{
    public class PaymentHistoryDetailsViewModel
    {
        public List<PaymentSession> PaymentSessions { get; set; } = new List<PaymentSession>();
        public List<Payment> Payments { get; set; } = new List<Payment>();
        public List<OtherDonationPayment> OtherDonationPayments { get; set; } = new List<OtherDonationPayment>();
        
        public List<DependentChecklistItem> PaymentItems { get; set; } = new List<DependentChecklistItem>();
    }
}
