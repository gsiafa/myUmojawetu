using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class AnnouncementViewModel
    {
        public int Id { get; set; }
        public Guid? UserId { get; set; }    
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please Add Details")]
        [MaxLength(10000 * 5, ErrorMessage = "The Description must be less than 10,000 words.")]
        public string Content { get; set; } =string.Empty;

        public DateTime? Date { get; set; }

        public List<Announcement> News { get; set; } = new List<Announcement>();
        public string Author { get; set; } = string.Empty;

        public string? formatDate { get; set; } = string.Empty;
        public bool IsActiveToInternMember { get; set; }

        public bool IsActiveToPublic { get; set; }




    }
}
