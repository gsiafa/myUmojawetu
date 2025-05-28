namespace WebOptimus.Models
{
    public class UserLoginSession
    {
        public int Id { get; set; }

        public string SessionId { get; set; }
        public int? DependentId { get; set; }
        public DateTime LoginDate { get; set; }
        public DateTime LastActivityDate { get; set; }

        public bool IsActive { get; set; }  
    }
}
