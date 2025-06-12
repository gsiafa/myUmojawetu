using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models.ViewModel
{
    public class AccountViewModel
    {
        //public Guid UserId { get; set; } = Guid.Empty;

        public string PersonRegNumber { get; set; } = string.Empty;

        public Guid UserId { get; set; }


        [Required(ErrorMessage = "Email is required."), Display(Name = "Email")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

      
        [Required(ErrorMessage = "Current Password is required")]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "New Password is required")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "New Password and Confirm Password do not match")]
        public string ConfirmPassword { get; set; }

        public DeactivateAccountViewModel DeactivateAccountViewModel { get; set; } = new DeactivateAccountViewModel();
    }
}
