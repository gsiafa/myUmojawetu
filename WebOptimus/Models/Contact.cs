using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models
{
    public class Contact
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter your Full Name")]
        public string FullName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please enter your Email Address")]
        public string EmailAddress { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please enter your Phone Number")]
        [RegularExpression(@"^\(?([0-9]{4})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$", ErrorMessage = "Mobile number is invalid.")]

        public string PhoneNumber { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please select a Subject")]
        public string Subject { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please write your Message")]
        public string Message { get; set; } = string.Empty;

        public string? Status { get; set; } = string.Empty;

        public string? ApprovalNote { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public string? RepliedEmail { get; set; } = string.Empty;
        [NotMapped]
        public string DateReplied { get; set; } = string.Empty;

     

        public DateTime UpdateOn { get; set; }
    }
}
