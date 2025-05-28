using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models
{
    public class DependentChecklistItem
    {
        [Key]
        public int Id { get; set; }
        public int DependentId { get; set; }
        public string Name { get; set; }=string.Empty;
        public string PersonRegNumber { get; set; } = string.Empty; 
        public decimal Price { get; set; }
        public bool IsSelected { get; set; }
        public int PaymentSessionId { get; set; }

        public string? SessionId { get; set; }
        public decimal? CustomAmount { get; set; }
        public decimal? MissedPayment { get; set; }
        public string CauseCampaignpRef { get; set; } = string.Empty;
        
        public Guid UserId { get; set; }

        [NotMapped]
        public int? currentLoginID { get; set; }

        [NotMapped]
        public int? GroupId { get;set; }

        [NotMapped]
        public bool Paid { get; set; }

        [NotMapped]
        public bool IsExempt { get; set; }

        [NotMapped]
        public string PriceLabel => IsExempt ? "Exempt" : $"£{Price:0.00}";


    }
}
