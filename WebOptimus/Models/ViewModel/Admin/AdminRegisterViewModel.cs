using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace WebOptimus.Models.ViewModel.Admin
{
    public class AdminRegisterViewModel
    {
        public AdminRegisterViewModel()
        {
            Roles = new List<SelectListItem>();
        }

        public Guid UserId { get; set; } = Guid.Empty;

        [Required(ErrorMessage = "First name is required"), Display(Name = "First Name")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 1)]
        [RegularExpression((@"^[a-zA-Z0-9""'\s- ÁáàÀâÂäÄãÃåÅæÆçÇéÉèÈêÊëËíÍÌìîÎïÏñÑóÓòÒôÔöÖõÕøØœŒßúÚùÙûÛüÜūŠ]*$"), ErrorMessage = "Invalid character in {0}")]
        public string FirstName { get; set; } = string.Empty;

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

        [Required(ErrorMessage = "Email is required"), Display(Name = "Email")]
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
        [YearOfBirthValidation]
        public string PersonYearOfBirth { get; set; } = string.Empty;

        [Required(ErrorMessage = "The Roles field is required.")]
        public string SelectedRole { get; set; }

        public List<SelectListItem> Roles { get; set; }

        public List<AdminDependentViewModel> Dependents { get; set; } = new List<AdminDependentViewModel>();

        [Required(ErrorMessage = "Region is required"), Display(Name = "Region")]
        public int RegionId { get; set; }

        [Required(ErrorMessage = "City is required"), Display(Name = "City")]
        public int CityId { get; set; }

        public string SponsorsMemberName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Mobile number is required")]
        [DataType(DataType.PhoneNumber)]
        [UKPhoneNumber]
        public string SponsorsMemberNumber { get; set; } = string.Empty;
        public string SponsorLocalAdminName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Mobile number is required")]
        [DataType(DataType.PhoneNumber)]
        [UKPhoneNumber]
        public string SponsorLocalAdminNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Outward Postcode is required"), Display(Name = "Outward Postcode")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{2,4}$", ErrorMessage = "Outward Postcode must contain at least one letter and one number, and be 2-4 characters in length.")]
        public string OutwardPostcode { get; set; } = string.Empty;
        public string? ApplicationStatus { get; set; } = string.Empty;

        [Display(Name = "Force password change at first log on")]
        public bool? ForcePasswordChange { get; set; }

        [NotMapped]
        public int DependentCount { get; set; } = 0;
    }

    public class AdminDependentViewModel
    {
        [Required(ErrorMessage = "Dependent name is required"), Display(Name = "Dependent Name")]
        public string PersonName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Year of Birth is required")]
        [YearOfBirthValidation]
        public string PersonYearOfBirth { get; set; } = string.Empty;
    }
}