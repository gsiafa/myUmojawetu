using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class CancelMembershipViewModel
    {
        public string PersonRegNumber { get; set; }
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Please provide a reason for cancelling your membership.")]
        public string Reason { get; set; }
    }

}
