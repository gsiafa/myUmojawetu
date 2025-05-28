namespace WebOptimus.Models.ViewModel
{
    public class PaymentDashboardViewModel
    {
        public IEnumerable<PaymentDetailViewModel> Payments { get; set; } = new List<PaymentDetailViewModel>();
        public decimal TotalAmount { get; set; }
        public decimal TotalTransactionFees { get; set; }
        public int over18DependentsInDb { get; set; }
        public int TotalPayments { get; set; }
        public decimal TotalOverPaid { get; set; }
        public int under18Dependents { get; set; }
        public decimal MissingPayment { get; set; }
        public int TotalDependents { get; set; }
        public DateTime DateJoined { get; set; } 
        public int TotalDependentsInDb { get; set; }
        public int under18DependentsInDb { get; set; }
        public int NumberOfPeopleInRegion { get; set; }
        public int over18DependentsInRegion { get; set; }

        public List<Dependant> Dependentsunder18 { get; set; } = new List<Dependant>();
        public List<Dependant> Dependentsover18 { get; set; } = new List<Dependant>();
        public int under18DependentsInRegion { get; set; }
        //public IEnumerable<PaymentDetailViewModel> RecentPayments { get; set; }
        public int over18Dependents { get; set; }
    }

    public class PaymentDetailViewModel
    {
        public int Id { get; set; }
        public string CauseCampaignpRef { get; set; }
        public decimal Amount { get; set; }
        public decimal TransactionFees { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal OverPaid { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime IsClosedDate { get; set; }
        public int DependentId { get; set; }
        public string YearOfBirth { get; set; }
        public string RegNumber { get; set; }   
        public string DependentName { get; set; }
        public decimal? TotalRaised { get; set; }  // New field for total raised
        public string? OurRef { get; set; } // Ensure this property exists
        public decimal GoodwillAmount { get; set; } // New property for Goodwill Amount
    }

    
}
