using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models.ViewModel
{
    public class GroupMemberViewModel
    {
        public int Id { get; set; }
        public int? DependentId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PersonRegNumber { get; set; }
        public string Status { get; set; }

        public bool IsAdmin { get; set; }
        public bool IsCreator { get; set; }
        public DateTime? DateConfirmed { get; set; }
        public DateTime DateInvited { get; set; }
        public int GroupId { get; set; }

        [NotMapped]
        public bool CanRevoke { get; set; }


        [NotMapped]
        public bool CanApproveOrDecline { get; set; }

        [NotMapped]
        public string GroupName { get; set; }
    }
}
