using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class SpouseViewModels
    {
        [Key]
        public int Id { get; set; }


        public Guid UserId { get; set; } = Guid.Empty;



        [Required(ErrorMessage = "Marital Status is required"), Display(Name = "Marital Status")]
        public string MaritalStatus { get; set; } = string.Empty;

        [Display(Name = "Spouse Name")]
        public string? SpouseName { get; set; } = string.Empty;
        [Display(Name = "Spouse Address")]
        public string? SpouseAddress { get; set;} = string.Empty;
        [Display(Name = "Spouse City")]
        public string? SpouseCity { get; set; } = string.Empty;
        [Display(Name = "Spouse Region")]
        public string? SpouseRegion { get; set; } = string.Empty;
        [Display(Name = "Spouse Postcode")]
        public string? SpousePostcode { get; set;} = string.Empty;
        [Display(Name = "Spouse Email")]
        public string? SpouseEmail { get; set; } = string.Empty;
        [Display(Name = "Living together?")]
        public bool? IsLivingTogether { get; set; }
    }
}
