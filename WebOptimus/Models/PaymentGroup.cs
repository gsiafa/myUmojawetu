using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models
{
    public class PaymentGroup
    {
        public int Id { get; set; }
        public string GroupName { get; set; }

        [NotMapped]
        public int TotalMembers { get; set; }
        public int? DependentId { get; set; }

        public string personRegNumber { get; set; } = string.Empty;
        public string CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }   

        [NotMapped]
        public bool IsAdmin { get; set; }
        [NotMapped]
        public bool IsCreator { get; set; }

        [NotMapped]
        public bool IsConfirmedMember { get; set; }

        [NotMapped]
        public int PendingRequests { get; set; }
    }
}
