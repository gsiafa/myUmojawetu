using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class TitleViewModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required"), Display(Name = "Title")]
        public string Name { get; set; } = string.Empty;



        public DateTime DateCreated { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? UpdateOn { get; set; }


    }
}
