using WebOptimus.StaticVariables;

namespace WebOptimus.Models
{
    public class ReportedDeath
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int DependentId { get; set; }

        public string PersonRegNumber { get; set; } = string.Empty;
        public DateTime DateOfDeath { get; set; }
        public DateTime DateOfBurial { get; set; }
        public int RegionId { get; set; }
        public int CityId { get; set; }

       
        public string? PlaceOfBurial { get; set; } = string.Empty;

        public string OtherRelevantInformation { get; set; } = string.Empty;
        public string DeathLocation { get; set; } = string.Empty;

        public string RelationShipToDeceased { get; set; } = string.Empty;
        public string ReporterContactNumber { get; set; } = string.Empty;
        public string? ReportedBy { get; set; } = string.Empty;
        public DateTime ReportedOn { get; set; }

        public string DeceasedName { get; set;} = string.Empty;

        public string DeceasedRegNumber { get; set; } = string.Empty;

        public string DeceasedYearOfBirth { get; set; } = string.Empty;

        public string DeceasedNextOfKinName { get; set; } = string.Empty;
        public string DeceasedNextOfKinPhoneNumber { get; set; } = string.Empty;
        public string DeceasedNextOfKinRelationship { get; set; } = string.Empty;
        public string DeceasedNextOfKinEmail { get; set; } = string.Empty;

        public DateTime DateJoined { get; set; }
        public DateTime DateCreated { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string Status { get; set; } = string.Empty;// New field to track status
        public string? DonationStatus { get; set; } = string.Empty;// New field to track status

        public string? DeceasedPhotoPath { get; set; } // Path to the photo file

        public string RegionalAdminNote { get; set; } = string.Empty;
        public DateTime RegionalAdminApprovalDate { get; set; }
   
        public string GeneralAdminNote { get; set; } = string.Empty;
        public DateTime GeneralAdminApprovalDate { get; set; }
        public string? ApprovedByRegionalAdmin { get; set; }
        public string? RejectedByRegionalAdmin { get; set; }

        public string? ApprovedByGeneralAdmin { get; set; }
        public string? RejectedByGeneralAdmin { get; set; }

        public bool? IsApprovedByRegionalAdmin { get; set; }
        public bool? IsRejectedByRegionalAdmin { get; set; }
        public bool? IsApprovedByGeneralAdmin { get; set; }
        public bool? IsRejectedByGeneralAdmin { get; set; }
    }
}
