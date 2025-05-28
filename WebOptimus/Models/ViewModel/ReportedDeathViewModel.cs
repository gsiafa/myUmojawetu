using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class ReportedDeathViewModel
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        //public List<SelectListItem> Users { get; set; }
        public List<SelectListItem> Dependents { get; set; }
        public bool IsUser { get; set; }
        public int DependentId { get; set; }
        public string DependentName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Date Of Death is required"), Display(Name = "Date Of Death")]
        public DateTime DateOfDeath { get; set; }
        [Required(ErrorMessage = "Death Location is required"), Display(Name = "Death Location")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 1)]
        [RegularExpression((@"^[a-zA-Z0-9""'\s- ÁáàÀâÂäÄãÃåÅæÆçÇéÉèÈêÊëËíÍÌìîÎïÏñÑóÓòÒôÔöÖõÕøØœŒßúÚùÙûÛüÜūŠ]*$"), ErrorMessage = "Invalid character in {0}")]
        public string DeathLocation { get; set; } = string.Empty;
        public string? PlaceOfBurial { get; set; } = string.Empty;

        public string? OtherRelevantInformation { get; set; } = string.Empty;
        public int RegionId { get; set; }
        public int CityId { get; set; }
        public string PersonRegNumber { get; set; } = string.Empty; // New property
        public string PersonYearOfBirth { get; set; } = string.Empty; // New property
        [Required(ErrorMessage = "Relationship to deceased is required"), Display(Name = "Relationship")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 1)]
        [RegularExpression((@"^[a-zA-Z0-9""'\s- ÁáàÀâÂäÄãÃåÅæÆçÇéÉèÈêÊëËíÍÌìîÎïÏñÑóÓòÒôÔöÖõÕøØœŒßúÚùÙûÛüÜūŠ]*$"), ErrorMessage = "Invalid character in {0}")]
        public string RelationShipToDeceased { get; set; } = string.Empty;
        [Required(ErrorMessage = "Reporter's name is required"), Display(Name = "Reporter Name")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 1)]
        [RegularExpression((@"^[a-zA-Z0-9""'\s- ÁáàÀâÂäÄãÃåÅæÆçÇéÉèÈêÊëËíÍÌìîÎïÏñÑóÓòÒôÔöÖõÕøØœŒßúÚùÙûÛüÜūŠ]*$"), ErrorMessage = "Invalid character in {0}")]
        public string ReportedBy { get; set; } = string.Empty;

        [Required(ErrorMessage = "Reported Date is required"), Display(Name = "Reported Date")]
        public DateTime ReportedOn { get; set; }

        [Required(ErrorMessage = "Reporter contact is required"), Display(Name = "Reporter Contact")]
     
        public string ReporterContactNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? DonationStatus { get; set; } = string.Empty;// New field to track status
        public IFormFile? DeceasedPhoto { get; set; } // For file upload

        public DateTime? DateCreated { get; set; }
        public DateTime DateJoined { get; set; }
        public DateTime? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string CityName { get; set; } = string.Empty; // New property
        public string RegionName { get; set; } = string.Empty; // New property
        public IEnumerable<SelectListItem> Deps { get; set; }
        public IEnumerable<SelectListItem> Cities { get; set; }
        public IEnumerable<SelectListItem> Regions { get; set; }

        public bool IsApprovedByRegionalAdmin { get; set; }
        public bool IsRejectedByRegionalAdmin { get; set; }
        public bool IsApprovedByGeneralAdmin { get; set; }
        public bool IsRejectedByGeneralAdmin { get; set; }

    }
}
