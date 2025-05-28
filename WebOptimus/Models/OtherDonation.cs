using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DocumentFormat.OpenXml.Math;

namespace WebOptimus.Models
{
    public class OtherDonation 
    {
        [Key]
        public int Id { get; set; }

        public string DonationType { get; set; } = string.Empty;     
    

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Target Amount")]
        [DataType(DataType.Currency)]
        public decimal TargetAmount { get; set; } 

       
     
        public DateTime StartDate { get; set; }

   
        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = false;  

        // Reference to the user who created the donation
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual User User { get; set; }

        public string Status { get; set; }
        public string? ApprovedBy { get; set; } = string.Empty;
        public DateTime? ApprovedByDate { get; set; }
        public string ApprovalNote { get; set; } = string.Empty;

        public string? DeclinedBy { get; set; } = string.Empty;
        public DateTime? DeclinedByDate { get; set; }
        public string DeclinedNote { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
       

        [Display(Name = "Last Updated")]
        public DateTime? DateUpdated { get; set; }
    }
}
