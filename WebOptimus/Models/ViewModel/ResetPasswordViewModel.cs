
using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class ResetPasswordViewModel
    {

        [Required(ErrorMessage = "Password is required")]
        [StringLength(255, ErrorMessage = "Password must be 8 Characters or more", MinimumLength = 8)]       
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Confirm Password is required")]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "Password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string EmailAddress { get; set; } = string.Empty;
        public string personRegNumber { get; set; } = string.Empty;

		[Required(ErrorMessage = "Current Password is required")]
		[StringLength(255, ErrorMessage = "Password must be 8 or more characters in length and contain at least 1 lowercase (a-z), 1 uppercase (A-Z) and 1 digit (0-9) or special character (!@#$%^&?*.+)", MinimumLength = 8)]
		[DataType(DataType.Password)]
		[Display(Name = "OldPassword")]
		public string OldPassword { get; set; }

        public string code { get; set; }
    }
}
