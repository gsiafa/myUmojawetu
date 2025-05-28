using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace WebOptimus.Models
{
    public class OtherDonationPayment
    {
        [Key]
        public int Id { get; set; }
        public Guid UserId { get; set; } = Guid.Empty;
        public int DependentId { get; set; }
        public string PersonRegNumber { get; set; } = string.Empty;
        public string CauseCampaignpRef { get; set; } = string.Empty;
        public decimal Amount { get; set; }


        public bool HasPaid { get; set; }
        public string? OurRef { get; set; }
        public string? Notes { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public string CreatedBy { get; set; }  

    }
}
