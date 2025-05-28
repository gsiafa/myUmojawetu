using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models
{
    [Table("NoteChangeLogs")]
    public class NoteChangeLogs
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string PersonRegNumber { get; set; } // Tracks which person the note belongs to

        [Required]
        public string Action { get; set; } // Added, Edited, Deleted

        public string? FieldName { get; set; } // Name of the field changed
        public string? OldValue { get; set; } // Previous value before change
        public string? NewValue { get; set; } // New value after change
        public string? FieldChanged { get; set; } // Optional description of what changed

        [Required]
        public DateTime DateChanged { get; set; } // When the change occurred

        public string? ChangedBy { get; set; } // The user who made the change
    }
}
