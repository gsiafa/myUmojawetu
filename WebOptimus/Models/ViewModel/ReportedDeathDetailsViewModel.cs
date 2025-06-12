using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models.ViewModel
{
    public class ReportedDeathDetailsViewModel
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
         public int DependentId { get; set; }
        public string PersonName { get; set; } = string.Empty;

        public string RegisterNumber { get; set; } = string.Empty;

        public string YearOfBirth { get; set; } = string.Empty;

        public string DateJoined { get; set; } 
        public string DateOfDeath { get; set; }
        public string DeathLocation { get; set; } = string.Empty;

        public string? PlaceOfBurial { get; set; } = string.Empty;

        public string RelationShipToDeceased { get; set; } = string.Empty;
       public string ReportedBy { get; set; } = string.Empty;

      public string ReportedOn { get; set; }

       public string ReporterContactNumber { get; set; } = string.Empty;

        public SuccessorViewModel Successor { get; set; }
        public string DeceasedPhotoPath { get; set; } = string.Empty; // New property

        public string DeceasedRegNumber { get; set; } = string.Empty;

        public string DeceasedYearOfBirth { get; set; } = string.Empty;

        public string DeceasedNextOfKinName { get; set; } = string.Empty;
        public string DeceasedNextOfKinPhoneNumber { get; set; } = string.Empty;
        public string DeceasedNextOfKinRelationship { get; set; } = string.Empty;
        public string? DeceasedNextOfKinEmail { get; set; } = string.Empty;
        public string? DateCreated { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }

        public IFormFile? DeceasedPhoto { get; set; } // For file upload

        public string RegionalAdminApprovalNote { get; set; } = string.Empty;

        public string GeneralAdminApprovalNote { get; set; } = string.Empty;
        public DateTime? GeneralAdminApprovalDate { get; set; }
        public DateTime? RegionalAdminApprovalDate { get; set; }


        public bool? IsRegionalAdmin { get; set; }
        public bool? IsGeneralAdmin { get; set; }
        public bool IsAccountHolder { get;set; }
        public bool? IsApprovedByRegionalAdmin { get; set; }
        public bool? IsApprovedByGeneralAdmin { get; set; }
        public string Status { get; set; }
        public string? OtherRelevantInformation { get; set; } = string.Empty;


        public Dependant Deps { get; set; }

        public IEnumerable<Dependant> DepsList { get; set; }

        [NotMapped]
        public bool isDependant { get; set; }

        public string? ApprovedByRegionalAdmin { get; set; }
        public string? RejectedByRegionalAdmin { get; set; }

        public string? ApprovedByGeneralAdmin { get; set; }
        public string? RejectedByGeneralAdmin { get; set; }

    }
}
