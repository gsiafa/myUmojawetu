using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace WebOptimus.Models.ViewModel
{
    public class RegisterViewModel 
    {
        public Guid UserId { get; set; } = Guid.Empty;

        [Required(ErrorMessage = "First name is required"), Display(Name = "First Name")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 1)]
        [RegularExpression((@"^[a-zA-Z0-9""'\s- ÁáàÀâÂäÄãÃåÅæÆçÇéÉèÈêÊëËíÍÌìîÎïÏñÑóÓòÒôÔöÖõÕøØœŒßúÚùÙûÛüÜūŠ]*$"), ErrorMessage = "Invalid character in {0}")]
        public string FirstName { get; set; } = string.Empty;
       

        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last name is required"), Display(Name = "Surname")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 1)]
        [RegularExpression((@"^[a-zA-Z0-9""'\s- ÁáàÀâÂäÄãÃåÅæÆçÇéÉèÈêÊëËíÍÌìîÎïÏñÑóÓòÒôÔöÖõÕøØœŒßúÚùÙûÛüÜūŠ]*$"), ErrorMessage = "Invalid character in {0}")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select your title"), Display(Name = "Title")]
        public int Title { get; set; }
        [Required(ErrorMessage = "Mobile number is required")]
        [DataType(DataType.PhoneNumber)]
        [UKPhoneNumber]
        public string PhoneNumber { get; set; } = string.Empty;

        public bool IsConsent { get; set; }
        [Required(ErrorMessage = "Email is required"),
            EmailAddress(ErrorMessage = "Please enter a valid email address"),
            Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please confirm your email address")]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Confirm Email")]
        [NotMapped, Compare("Email", ErrorMessage = "Email Addresses do not match.")]
        public string ConfirmEmail { get; set; } = string.Empty;


        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.{8,})(?=.*[a-z])(?=.*[A-Z])((?=.*[0-9])|(?=.*[@#$%^&+*!=])).*$", ErrorMessage = "Password must be 8 or more characters in length and contain at least 1 lowercase (a-z), 1 uppercase (A-Z) and 1 digit (0-9) or special character (!@#$%^&?*.+)")]
        [StringLength(255, ErrorMessage = "Password must be 8 or more characters in length and contain at least 1 lowercase (a-z), 1 uppercase (A-Z) and 1 digit (0-9) or special character (!@#$%^&?*.+)", MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;
        [Display(Name = "Confirm Password")]
        [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [NotMapped, Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
        public int? Status { get; set; }

        public DateTime DateCreated { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? UpdateOn { get; set; }

        public DateTime LastPasswordChangedDate { get; set; }

     
        public DateTime? DateExpired { get; set; }

        public string RoleSelected { get; set; }



        public Dependant Deps { get; set; }

        public IEnumerable<Dependant> DepsList { get; set; }

      
        [NotMapped]
        public string OldEmail { get; set; } = string.Empty;

        [NotMapped]
        public string? RoleId { get; set; } = string.Empty;
        [NotMapped]
        [Display(Name = "Role")]
        public string? Role { get; set; } = string.Empty;
        [NotMapped]
        public IEnumerable<SelectListItem>? RoleList { get; set; }

        public Title Titles { get; set; }

        public IEnumerable<Title> TitleList { get; set; }



        public string SponsorsMemberName { get; set; } = string.Empty;

        public string SponsorsMemberNumber { get; set; } = string.Empty;

        public string SponsorLocalAdminName { get; set; } = string.Empty;
        public string SponsorLocalAdminNumber { get; set; } = string.Empty;

        [NotMapped]
        public string Region { get; set; } = string.Empty;

        [NotMapped]
        public string City { get; set; }= string.Empty;
        [NotMapped]
        public string OutwardPostcode { get; set; } = string.Empty;

        [NotMapped]
        public bool isDependant { get; set; }

        [NotMapped]

        [Required(ErrorMessage = "Please enter note")]
        public string Note { get; set; } = string.Empty;
        public DateTime? NoteDate { get; set; }

        [NotMapped]
        public string? applicationStatus { get; set;} = string.Empty;

        [Display(Name = "Force password change at first log on")]
        public bool? ForcePasswordChange { get; set; }
        public User GetUserInfo { get; set; }

        [Required(ErrorMessage = "Region is required"), Display(Name = "Region")]

        public int RegionId { get; set; }

        [ForeignKey("RegionId")]
        public virtual Region RegionList { get; set; }

        public int CityId { get; set; }

        [ForeignKey("CityId")]
        public virtual City CityList { get; set; }

        public List<DependentWithKinViewModel> NextOfKins { get; set; } = new List<DependentWithKinViewModel>();

        public List<DependantViewModel> Dependents { get; set; } = new List<DependantViewModel>();

        [NotMapped]
        public string? DOB { get; set; } = string.Empty;

    

    }
}
