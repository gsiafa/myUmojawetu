using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyModel;


namespace WebOptimus.Models
{
    public class User : IdentityUser
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public int? DependentId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;

        public int Title { get; set; }
    
        public string ApplicationStatus { get; set; } = string.Empty;

        public int? SuccessorId { get; set; } //successKey

        public string? Note { get; set; } = string.Empty;
        public DateTime? NoteDate { get; set; }

        public string? ApprovalDeclinerName { get; set; } = string.Empty;
        public string? ApprovalDeclinerEmail { get; set; } = string.Empty;
        public bool IsConsent { get; set; }
        public DateTime DateCreated { get; set; }

       
        public string? CreatedBy { get; set; }

        public DateTime? UpdateOn { get; set; }       
        public DateTime LastPasswordChangedDate { get; set; }

        public bool? ForcePasswordChange { get; set; }

        public string SponsorsMemberName { get; set; } = string.Empty;

        public string SponsorsMemberNumber { get; set; } = string.Empty;

        public string SponsorLocalAdminName { get; set; } = string.Empty;
        public string SponsorLocalAdminNumber { get; set; } = string.Empty;
        public string PersonYearOfBirth { get; set; } = string.Empty;
        public string PersonRegNumber { get; set; } = string.Empty;

        public int? RegionId { get; set; }

        public bool IsActive { get; set; } = true;

        public bool? IsDeceased { get; set; }
        
        public int? CityId { get; set; }
        public string? DeactivationReason { get; set; } // Reason for deactivation
        public DateTime? DeactivationDate { get; set; } // Timestamp of deactivation
        public DateTime? ReactivationDate { get; set; }

        public string OutwardPostcode { get; set; } = string.Empty;

        [NotMapped]
        public string RoleId { get; set; }
        [NotMapped]
        [Display(Name = "Role")]
        public string Role { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem> RoleList { get; set; }

     
        [NotMapped]
        public IEnumerable<SelectListItem>? TitleList { get; set; }


        [NotMapped]
        public string OldEmail { get; set; } = string.Empty;

        [NotMapped]
        public string FullName { get; set; }

        // New properties for region and city names
        [NotMapped]
        public string RegionName { get; set; } = string.Empty;

        [NotMapped]
        public string CityName { get; set; } = string.Empty;




    }
}
