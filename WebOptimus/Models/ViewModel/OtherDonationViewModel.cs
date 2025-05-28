using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class OtherDonationViewModel 
    {

        public int Id { get; set; }

      
        public string Summary { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? MinmumAmount { get; set; }
        public decimal TargetAmount { get; set; }

        public bool IsActive { get; set; } = true;

        public string CauseCampaignpRef { get; set; } = string.Empty;

        public DateTime DateCreated { get; set; }
        public bool IsDisplayable { get; set; }
        public string CreatedBy { get; set; }
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime? ClosedDate { get; set; }


        public string? Status { get; set; } = string.Empty;

    }
}
