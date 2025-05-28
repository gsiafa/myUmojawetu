using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models.ViewModel
{
    public class AboutUserViewModel
    {
        public Guid UserId { get; set; } = Guid.Empty;

        [Required(ErrorMessage = "Date Of Birth is required"), Display(Name = "Date Of Birth")]
        public DateTime DateOfBirth { get; set; }
        [Required(ErrorMessage = "First Line Of Address is required"), Display(Name = "First Line Of Address")]
        public string FirstLineOfAddress { get; set; } = string.Empty;
        [Required(ErrorMessage = "City is required"), Display(Name = "City/Town")]
        public string City { get; set; } = string.Empty;



        [Required(ErrorMessage = "Region is required"), Display(Name = "Region")]
        public string Region { get; set; } = string.Empty;
        [Required(ErrorMessage = "Post Code is required"), Display(Name = "PostCode")]
        public string PostalCode { get; set; } = string.Empty;


        public DateTime DateCreated { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? UpdateOn { get; set; }





        [Required(ErrorMessage = "First Name is required"), Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required"), Display(Name = "Surname")]
        public string Surname { get; set; } = string.Empty;
        [Display(Name = "Email Address"), EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please confirm your email address")]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Confirm Email")]
        [NotMapped, Compare("Email", ErrorMessage = "Email Addresses do not match.")]
        public string ConfirmEmail { get; set; } = string.Empty;

       
       


        [Required(ErrorMessage = "Marital Status is required"), Display(Name = "Marital Status")]
        public string MaritalStatus {  get; set; } = string.Empty;

        public string? Status { get; set; } = string.Empty;   

       
        public DateTime LastPasswordChangedDate { get; set; }
        public bool? ForcePasswordChange { get; set; }
        [Display(Name = "Consent")]
        public bool IsConsent { get; set; }

        public string? RegNumber { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        [NotMapped, Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
      
        [StringLength(15), Display(Name = "Phone Number"), RegularExpression("^[0-9 ]*$", ErrorMessage = "Contact Number must be numeric and cannot contain a space.")]
        [Required(ErrorMessage = "Please enter Telephone Number")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
