using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace WebOptimus.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }
        public Guid UserId { get; set; } = Guid.Empty;


        public string personRegNumber { get; set; } = string.Empty;
        public int DependentId { get; set; }
        public string CauseCampaignpRef { get; set; } = string.Empty;
        public decimal Amount { get; set; }
       
        //public decimal TransactionFees { get; set; }
        //public decimal TotalAmount { get; set; }

        public decimal GoodwillAmount { get; set; }
        public bool HasPaid { get; set; }
        public string? OurRef { get; set; }
        public string? Notes { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public string CreatedBy { get; set; }
        //public string DateCreatedASString { get; set; }
        [NotMapped]
        public string DateRaised { get; set; } = string.Empty;


        [NotMapped]
        public decimal? OverPaid { get; set; }

        [NotMapped]
        public string? MemberName { get; set; } = string.Empty ;



    }
}
