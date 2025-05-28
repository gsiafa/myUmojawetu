using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class ForgetPasswordViewModel
    {

        [Display(Name = "Email Address"), EmailAddress(ErrorMessage = "Email address is not valid")]
        [Required(ErrorMessage = "Please enter your email")]
        public string Email { get; set; } = string.Empty;
                
    }
}
