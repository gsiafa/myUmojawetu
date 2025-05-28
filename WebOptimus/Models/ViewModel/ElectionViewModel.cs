using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class ElectionViewModel
    {
        public int ElectionId { get; set; }

        [Required]
        [Display(Name = "Position")]
        public string ElectionName { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
    }
}
