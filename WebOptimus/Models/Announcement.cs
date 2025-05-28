using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models
{
    public class Announcement
    {
        [Key]
        public int Id { get; set; }
        public Guid UserId { get; set; }
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Please Add Details")]
        [MaxLength(10000 * 5, ErrorMessage = "The Description must be less than 10,000 words.")]
        public string Content { get; set; }

        public DateTime Date { get; set; }

      
        public string Author { get; set; }
        public bool? IsActiveToInternMember { get; set; }

        public bool? IsActiveToPublic { get; set; }
    }
}
