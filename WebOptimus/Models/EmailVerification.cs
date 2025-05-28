namespace WebOptimus.Models
{
    public class EmailVerification
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string Token { get; set; }
        public DateTime ExpirationTime { get; set; }
        public bool Used { get; set; }
        public virtual User User { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string CreatedBy { get; set; }

        public string PersonRegNumber { get; set; } = string.Empty;
        public int? DependentId { get; set; }
    }
}
