
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace WebOptimus.Models.ViewModel
{
    public class RegistrationViewModels
    {
        //public NextOfKinViewModels NextOfKin { get; set; }
        //public ChildrenViewModels Children { get; set; }
        //public SpouseViewModels Spouse { get; set; }

        //public AboutUserViewModel AboutUser { get; set; }

        //public UserViewModel User { get; set; }

        public RegisterViewModel Register { get; set; }

        public string PersonYearOfBirth { get; set; } = string.Empty;
        [Display(Name = "Force password change at first log on")]
        public bool? ForcePasswordChange { get; set; }
        public DependantViewModel Deps { get; set; }

        [NotMapped]
        public string OldEmail { get; set; } = string.Empty;

        [NotMapped]
        public string? RoleId { get; set; } = string.Empty;
        [NotMapped]
        [Display(Name = "Role")]
        public string? Role { get; set; } = string.Empty;
        [NotMapped]
        public IEnumerable<SelectListItem>? RoleList { get; set; }

        public CityViewModel CityVm { get; set; }

        [Required(ErrorMessage = "Region is required"), Display(Name = "Region")]
        public int RegionId { get; set; }

        [Required(ErrorMessage = "City is required"), Display(Name = "City")]
        public int CityId { get; set; }

        public IEnumerable<Region> RegionList { get; set; }

        public Region Regions { get; set; }



        [Required(ErrorMessage = "Sponsor Member Name is required"), Display(Name = "Sponsors Member Name")]
        public string SponsorsMemberName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sponsor Local Admin Name is required"), Display(Name = "Sponsors Member Name")]
        public string SponsorLocalAdminName { get; set; } = string.Empty;



        [Required(ErrorMessage = "Outward Postcode is required"), Display(Name = "Outward Postcode")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{2,4}$", ErrorMessage = "Outward Postcode must contain at least one letter and one number, and be 2-4 characters in length.")]
          public string OutwardPostcode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Member Number is required")]
        [DataType(DataType.PhoneNumber)]
        [UKPhoneNumber]

        public string SponsorsMemberNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Local Admin Number is required")]
        [DataType(DataType.PhoneNumber)]
        [UKPhoneNumber]
        public string SponsorLocalAdminNumber { get; set; } = string.Empty;


    }
}
