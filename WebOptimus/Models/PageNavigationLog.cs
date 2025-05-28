namespace WebOptimus.Models
{
    public class PageNavigationLog
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public string PageName { get; set; }
        public string ActionName { get; set; }
        public string ControllerName { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
