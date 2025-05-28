namespace WebOptimus.Models.ViewModel
{
    public class PaymentHistoryViewModel
    {
        public int Id { get; set; }
        public string CauseCampaignpRef { get; set; }
        public decimal Amount { get; set; }

        public decimal OriginalLatePaymentFee { get; set; }
        public decimal LatePaymentFee { get; set; }
        public decimal TransactionFees { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? OverPaid { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? EndDate { get; set; }
        public int DependentId { get; set; }
        public string PersonRegNumber { get; set; } = string.Empty;
        public string DependentName { get; set; }
        public decimal? TotalRaised { get; set; } // New field for total raised
        public string? OurRef { get; set; } // Ensure this property exists
        public decimal? GoodwillAmount { get; set; } // New property for Goodwill Amount

        // Properties for missed payments
        public string RegNumber { get; set; } // Dependent's registration number
        public string YearOfBirth { get; set; } // Dependent's year of birth
    }
}
