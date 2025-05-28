namespace WebOptimus.Models
{
    public class Constitution
    {
        public int Id { get; set; }
        public string FilePath { get; set; } = null!;
        public string UploadedBy { get; set; } = null!;
        public DateTime UploadedOn { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; }
        public string FileHash { get; set; } = null!;
    }
}
