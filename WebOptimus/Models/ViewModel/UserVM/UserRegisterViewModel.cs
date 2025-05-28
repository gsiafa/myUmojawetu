using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



namespace WebOptimus.Models.ViewModel.UserVM
{
    public class UserRegisterViewModel
    {
       
        public Guid UserId { get; set; } = Guid.Empty;

        [Required(ErrorMessage = "Please enter First Name"), Display(Name = "First Name")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 1)]
        [RegularExpression(@"^[a-zA-Z0-9""'\s- ÁáàÀâÂäÄãÃåÅæÆçÇéÉèÈêÊëËíÍÌìîÎïÏñÑóÓòÒôÔöÖõÕøØœŒßúÚùÙûÛüÜūŠ]*$", ErrorMessage = "Invalid character in {0}")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Please enter Last Name"), Display(Name = "Surname")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 1)]
        [RegularExpression(@"^[a-zA-Z0-9""'\s- ÁáàÀâÂäÄãÃåÅæÆçÇéÉèÈêÊëËíÍÌìîÎïÏñÑóÓòÒôÔöÖõÕøØœŒßúÚùÙûÛüÜūŠ]*$", ErrorMessage = "Invalid character in {0}")]
        public string Surname { get; set; }

        [Required(ErrorMessage = "Please select your title"), Display(Name = "Title")]
        public int Title { get; set; }

        public int Gender { get; set; }

        [Required(ErrorMessage = "Please enter your mobile Number")]
        [DataType(DataType.PhoneNumber)]
        [UKPhoneNumber]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Consent is required")]
        public bool IsConsent { get; set; }
        [Required(ErrorMessage = "Please provide your Email Address"), Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required"), Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required"), Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Year of Birth is required")]
        //[RegularExpression(@"^(\d{4})$", ErrorMessage = "Please enter a valid 4 digit Year of Birth")]       
        public string PersonYearOfBirth { get; set; } = string.Empty;

        public string? PersonRegNumber { get; set; } = string.Empty;

        public List<UserDependentViewModel> Dependents { get; set; } = new List<UserDependentViewModel>();

        [Required(ErrorMessage = "Please select your Region"), Display(Name = "Region")]
        public int? RegionId { get; set; }

        [Required(ErrorMessage = "Please select your County"), Display(Name = "County")]
        public int CityId { get; set; }
        [Required(ErrorMessage = "Please provide a Referee Name")]
        public string RefereeMemberName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Referee Mobile number is required")]
        [DataType(DataType.PhoneNumber)]
        [UKPhoneNumber]
        public string RefereeMemberNumber { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please provide Local Admin Name")]
        public string RefereeLocalAdminName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Local Admin Mobile number is required")]
        [DataType(DataType.PhoneNumber)]
        [UKPhoneNumber]
     
        public string RefereeLocalAdminNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide your Outward Postcode"), Display(Name = "Outward Postcode")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{2,4}$", ErrorMessage = "Outward Postcode must contain at least one letter and one number, and be 2-4 characters in length.")]
        public string OutwardPostcode { get; set; } = string.Empty;
        public string? ApplicationStatus { get; set; } = string.Empty;
      
        [NotMapped]
        public int DependentCount { get; set; } = 0;
    }

    public class UserDependentViewModel
    {
        [Required(ErrorMessage = "Dependent name is required"), Display(Name = "Dependent Name")]
        public string PersonName { get; set; } = string.Empty;

        public int DependentTitle { get; set; }

        public int DependentGender { get; set; }

        public string? DependentEmail { get; set; }

       
        [DataType(DataType.PhoneNumber)]
        [UKPhoneNumber]
        public string? DependentPhoneNumber { get; set; } = string.Empty;
        [Required(ErrorMessage = "Year of Birth is required")]
        //[RegularExpression(@"^(\d{4})$", ErrorMessage = "Please enter a valid 4 digit Year of Birth")]       
        public string PersonYearOfBirth { get; set; } = string.Empty;
    }
}