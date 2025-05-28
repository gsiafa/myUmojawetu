using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace WebOptimus.Models.ViewModel.Admin;
public class AdminUserViewModel
{
    public AdminUserViewModel()
    {
        Roles = new List<SelectListItem>();
        Dependents = new List<AdminEditDependentViewModel>();
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

    [Required(ErrorMessage = "The Roles field is required.")]
    public string SelectedRole { get; set; }

    public List<SelectListItem> Roles { get; set; }

    public List<AdminEditDependentViewModel> Dependents { get; set; }

    [Required(ErrorMessage = "Region is required"), Display(Name = "Region")]
    public int RegionId { get; set; }

    [Required(ErrorMessage = "City is required"), Display(Name = "City")]
    public int CityId { get; set; }

    public string SponsorsMemberName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sponsor Member Number is required")]
    [DataType(DataType.PhoneNumber)]
    [UKPhoneNumber]
    public string SponsorsMemberNumber { get; set; } = string.Empty;

    public string SponsorLocalAdminName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sponsor Local Admin Number is required")]
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

    [NotMapped]
    public string FullName { get; set; } = string.Empty;
}

public class AdminEditDependentViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Dependent name is required"), Display(Name = "Dependent Name")]
    public string PersonName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Year of Birth is required")]
    [RegularExpression(@"^(\d{4})$", ErrorMessage = "Please enter a valid 4 digit Year of Birth")]
    public string PersonYearOfBirth { get; set; } = string.Empty;

    public int? Title { get; set; }
    public int? Gender { get; set; }

    public string? Email { get; set; }


    [DataType(DataType.PhoneNumber)]
    [UKPhoneNumber]
    public string? PhoneNumber { get; set; } = string.Empty;
    public string PersonRegNumber { get; set; } = string.Empty;

    public List<NextOfKinViewModel> NextOfKins { get; set; } = new List<NextOfKinViewModel>();
}

public class NextOfKinViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Next of Kin Name is required")]
    [Display(Name = "Next of Kin Name")]
    public string NextOfKinName { get; set; } = string.Empty;

    [Display(Name = "Next of Kin Address")]
    public string NextOfKinAddress { get; set; } = string.Empty;

    [Display(Name = "Next of Kin Email")]
    [EmailAddress]
    public string? NextOfKinEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Next of Kin Tel is required")]
    [Display(Name = "Next of Kin Tel")]
    [Phone]
    public string NextOfKinTel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Relationship is required")]
    public string Relationship { get; set; } = string.Empty;
}
