using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class LoginViewModel
    {

        [Display(Name = "Email Address"), EmailAddress(ErrorMessage = "Email address is not valid")]
        [Required(ErrorMessage = "Please enter your email")]
        public string Email { get; set; } = string.Empty;


        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Please enter your password")]
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }

    }
}
