using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class BannerViewModel
    {
        public int Id { get; set; }
        public Guid? UserId { get; set; }    
        public string Title { get; set; } = string.Empty;

    
        public DateTime? Date { get; set; }

        public List<Announcement> News { get; set; } = new List<Announcement>();
        public string Author { get; set; } = string.Empty;

        public string? formatDate { get; set; } = string.Empty;
        public bool IsActive { get; set; }





    }
}
