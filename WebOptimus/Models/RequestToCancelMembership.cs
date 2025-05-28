using DocumentFormat.OpenXml.Bibliography;
using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models
{
    public class RequestToCancelMembership
    {
        [Key]
        public int Id { get; set; } 

        public Guid UserId { get; set; } = Guid.NewGuid();
       
        public string PersonRegNumber { get; set; } = string.Empty;      

        
        public bool CancelWithFamilyMembers { get; set; } = false;

    
        public string CancellationReason { get; set; } = string.Empty ;

    
        public DateTime DateRequested { get; set; } = DateTime.UtcNow;

        public string AdminApprovalNote { get; set; } = string.Empty;

        public DateTime? AdminApprovalDate { get; set; }


        public string Status { get; set; } = string.Empty;
    }
}
