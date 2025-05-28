using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models
{
    public class CustomPayment
    {
        [Key]
        public int Id { get; set; }
      
        public string PersonRegNumber { get; set; } 

        public Guid UserId { get; set; }  

        [Required]
        public string CauseCampaignpRef { get; set; }  

       
        [Column(TypeName = "decimal(18,2)")]
        public decimal ReduceFees { get; set; }
        public string Reason { get; set; }  

      
        public string CreatedBy { get; set; } 

        public string CreatedByName { get; set; }  

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    }
}
