using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models
{
    public class Banner
    {
        [Key]
        public int Id { get; set; }
        public Guid UserId { get; set; }
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

  
        public DateTime Date { get; set; }

      
        public string Author { get; set; }
        public bool? IsActive { get; set; }

    }
}
