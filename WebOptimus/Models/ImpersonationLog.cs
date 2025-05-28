namespace WebOptimus.Models
{
    public class ImpersonationLog
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string AdminEmail { get; set; }
        public string ImpersonatedUserId { get; set; }
        public string ImpersonatedEmail { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
