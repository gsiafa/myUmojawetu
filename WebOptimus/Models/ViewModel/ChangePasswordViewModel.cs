
using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class ChangePasswordViewModel
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
        public string? GuidId { get; set; } = string.Empty;
        public int? DependentId { get; set; }
        public bool RememberMe { get; set; }
	
	}
}
