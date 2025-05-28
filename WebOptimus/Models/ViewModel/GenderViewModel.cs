using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class GenderViewModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Gender is required"), Display(Name = "Gender")]
        public string GenderName { get; set; } = string.Empty;



        public DateTime DateCreated { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? UpdateOn { get; set; }


    }
}
