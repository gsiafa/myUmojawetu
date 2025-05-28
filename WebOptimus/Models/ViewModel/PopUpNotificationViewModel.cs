using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class PopUpNotificationViewModel
    {
        [Key]
        public int Id { get; set; }


        [Required(ErrorMessage = "Please Add Details")]
        [MaxLength(10000 * 5, ErrorMessage = "The Description must be less than 10,000 words.")]
        public string Description { get; set; }

        public DateTime Date { get; set; }

      
        public string Author { get; set; }
        public bool IsActive { get; set; }
    }
}
