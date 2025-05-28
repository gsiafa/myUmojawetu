namespace WebOptimus.Models
{
    using System.ComponentModel.DataAnnotations;

    public class Audit
    {
        [Key]    
        public int AuditID { get; set; }       
         [Display(Name = "Email")]    
        public string Email { get; set; } = string.Empty;
        [Display(Name = "User ID")]
        public Guid? UserID { get; set; }
        [Display(Name = "Error Type")]
        public string ErrorType { get; set; } = string.Empty;
        [Display(Name = "Action")]
        public string ActionType { get; set; } = string.Empty;
        [Display(Name = "Action Description")]
        public string ActionDesc { get; set; } = string.Empty;
        [Display(Name = "Date Created")]
        public DateTime DateCreated { get; set; }
        [Display(Name = "IP Address")]
        public string IPAddress { get; set; } = string.Empty;
    }
}
