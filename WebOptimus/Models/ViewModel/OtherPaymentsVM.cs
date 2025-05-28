namespace WebOptimus.Models.ViewModel
{
    public class OtherPaymentsVM
    {
        public List<PaymentSession> PaymentSessions { get; set; } = new List<PaymentSession>();
        public List<OtherDonationPayment> OtherDonationPayments { get; set; } = new List<OtherDonationPayment>();
        
        public List<DependentChecklistItem> PaymentItems { get; set; } = new List<DependentChecklistItem>();
    }
}
