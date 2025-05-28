using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace WebOptimus.Models.ViewModel { 
    public class UserViewModel
    {
        public Guid UserId { get; set; } = Guid.Empty;

        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Forname(s) required."), Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;


        [Required(ErrorMessage = "Surname is required."), Display(Name = "Surname")]
        public string Surname { get; set; } = string.Empty;
           public DateTime LastPasswordChangedDate { get; set; }
        public bool? ForcePasswordChange { get; set; }

        [Required(ErrorMessage = "Consent is required."), Display(Name = "Consent")]
        public bool IsConsent { get; set; }
        public string RegNumber { get; set; } = string.Empty;



        [Required(ErrorMessage = "Email is required."), Display(Name = "Surname")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Password is required.")]     

        public string Password { get; set; } = string.Empty;
        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        [NotMapped, Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Required(ErrorMessage = "Confirm Password is required.")]
        public string ConfirmPassword { get; set; } = string.Empty;


        [UKPhoneNumber]
        [Required(ErrorMessage = "Mobile Number is required.")]
        public string PhoneNumber { get; set; } = string.Empty;


        public string SponsorsMemberName { get; set; } = string.Empty;

        public string SponsorLocalAdminName { get; set; } = string.Empty;

        public int RegionId { get; set; }

        [ForeignKey("RegionId")]
        public virtual Region Region { get; set; }

        public int CityId { get; set; }

        [ForeignKey("CityId")]
        public virtual City City { get; set; }



        public DateTime DateCreated { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? UpdateOn { get; set; }

             
       

    }
}
