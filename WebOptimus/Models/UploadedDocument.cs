using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models
{
    public class UploadedDocument
    {
        public int Id { get; set; }
        public string FileName { get; set; } // Name of the file, e.g., "policyV3.pdf"
        public string FilePath { get; set; } // Path to the file
        public DateTime UploadedAt { get; set; } // Date and time of upload

        public string? CreatedBy { get; set; }

    
    }
}
