using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class RegionViewModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Region name is required"), Display(Name = "Region Name")]
        public string Name { get; set; } = string.Empty;



        public DateTime DateCreated { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? UpdateOn { get; set; }


    }
}
