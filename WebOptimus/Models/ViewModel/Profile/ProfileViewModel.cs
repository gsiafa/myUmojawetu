using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models.ViewModel
{
    public class ProfileViewModel
    {
        public Guid UserId { get; set; } = Guid.Empty;

        public int DependentId { get; set; }

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
        [RegularExpression(@"^\(?([0-9]{4})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$", ErrorMessage = "Mobile number is invalid.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required"),
            EmailAddress(ErrorMessage = "Please enter a valid email address"),
            Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;



        public string? PersonYearOfBirth { get; set; } = string.Empty;
        public string? PersonRegNumber { get; set; } = string.Empty;


        [Required(ErrorMessage = "City is required"), Display(Name = "City")]
      
        public int CityId { get; set; }

        [Required(ErrorMessage = "Region is required"), Display(Name = "Region")]
        public int RegionId { get; set; }



        [NotMapped]
        public string OldEmail { get; set; } = string.Empty;


        [NotMapped]
        public string OldPhoneNumber { get; set; } = string.Empty;


        [NotMapped]
        public string TitleName { get; set; } = string.Empty;
        [NotMapped]
        public string RegionName { get; set; } = string.Empty;

        [NotMapped]
        public string CityName { get; set; }= string.Empty;
        [NotMapped]
        public string OutwardPostcode { get; set; } = string.Empty;

        [NotMapped]
        public string FullName { get; set; } = string.Empty;


        [NotMapped]
        public string DateJoined { get; set; } = string.Empty;


    }
}
