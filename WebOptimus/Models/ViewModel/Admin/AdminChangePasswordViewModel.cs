using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel.Admin
{
    public class AdminChangePasswordViewModel
    {
        public Guid UserId { get; set; }

        public string PersonRegNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // The NewPassword field is required
        [Required]
        public string NewPassword { get; set; } = string.Empty;

        // The ConfirmPassword field is required and must match the NewPassword
        [Required]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        // Use a non-nullable boolean for ForcePasswordChange (unless you need null states)
        public bool ForcePasswordChange { get; set; }
    }
}
