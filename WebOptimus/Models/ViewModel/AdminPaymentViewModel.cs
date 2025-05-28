namespace WebOptimus.Models.ViewModel
{
    public class AdminPaymentViewModel
    {
        public IEnumerable<PaymentDetailViewModel> Payments { get; set; } = new List<PaymentDetailViewModel>();
        public IEnumerable<AdminMissedPaymentViewModel> MissedPayments { get; set; } = new List<AdminMissedPaymentViewModel>(); 
        public string CauseFilter { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalTransactionFees { get; set; }
        public int TotalPayments { get; set; }
        public decimal? TotalOverPaid { get; set; }
        public int TotalDependents { get; set; }
        public int TotalDependentsInDb { get; set; }
        public decimal MissingPayment { get; set; }
        public IEnumerable<PaymentDetailViewModel> RecentPayments { get; set; } = new List<PaymentDetailViewModel>();
        public int over18DependentsInDb { get; set; }
        public int under18DependentsInDb { get; set; }
        public int NumberOfPeopleInRegion { get; set; }
        public int over18DependentsInRegion { get; set; }
        public int under18DependentsInRegion { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public int MissedPaymentCurrentPage { get; set; }
        public int MissedPaymentTotalPages { get; set; }

    }

    public class AdminMissedPaymentViewModel
    {
        public int DependentId { get; set; }
        public string DependentName { get; set; }
        public string YearOfBirth { get; set; }
        public string RegNumber { get; set; }
        public string PhoneNumber { get; set; }
        public string CauseCampaignpRef { get; set; }

        public string? Email { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime IsClosedDate { get; set; }

    }
}
