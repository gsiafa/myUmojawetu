using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebOptimus.Models.ViewModel;

namespace WebOptimus.Models
{
    public class NoteHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NoteTypeId { get; set; } // Foreign key to NoteType table

        [ForeignKey("NoteTypeId")]
        public virtual NoteType NoteType { get; set; }

        [Required]
        [StringLength(20)]
        public string PersonRegNumber { get; set; } // Links note to a user/dependent

        [Required]
        public string Description { get; set; } // The actual note

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(50)]
        public string CreatedBy { get; set; } // User who created the note

        public string? UpdatedBy { get; set; } = string.Empty;

        public string? CreatedByName { get; set; } = string.Empty;
        public DateTime? UpdatedOn { get; set; } // Last update timestamp

        public bool IsDeleted { get; set; } = false; // Soft delete option


    }
}
