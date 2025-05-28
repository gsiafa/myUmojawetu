using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebOptimus.Models.ViewModel
{
    public class CauseViewModel
    {
        public int Id { get; set; }
        public string Summary { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please Select Person Name")]
        public int ReportedDeathId { get; set; }

        [Required(ErrorMessage = "Please enter full Description"), Display(Name = "Description")]
        [MaxLength(50000, ErrorMessage = "The Description must be less than 50,000 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Target Amount is required"), Display(Name = "Target Amount")]
        public decimal TargetAmount { get; set; }
        [Required(ErrorMessage = "Miss Payment Fine Amount is required"), Display(Name = "Missed Payment Amount")]
        public decimal MissPaymentAmount { get; set; }
        public decimal AmountRaised { get; set; }
        public bool IsActive { get; set; }
        public bool IsDisplayable { get; set; }
        public string CauseCampaignpRef { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Member Amount is required"), Display(Name = "Full Member Amount")]
        public decimal FullMemberAmount { get; set; }

        [Required(ErrorMessage = "Under Age Limit is required"), Display(Name = "UnderAge Limit")]
        public int UnderAge { get; set; }

        [Required(ErrorMessage = "Under Age Amount is required"), Display(Name = "Under Age Amount")]
        public decimal UnderAgeAmount { get; set; }

        public DateTime? DateCreated { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? UpdateOn { get; set; }

        // List of Reported Deaths for dropdown
        public List<SelectListItem> ReportedDeaths { get; set; }
      
        // Properties to hold additional details from the reported death
        public string RegistrationNumber { get; set; } = string.Empty;
        public string YearOfBirth { get; set; }
        public DateTime DateJoined { get; set; }
        public DateTime DateOfDeath { get; set; }

        public string DateJoinedAsString { get; set; }
        public string DateOfDeathAsString { get; set; }

        // Properties to hold additional details from the reported death

        public int? over18DependentsCount { get; set; }

        public int? totalmembers { get; set; }

        public int? under18DependentsCount { get; set; }
        public string DependentName { get; set; } = string.Empty;

        public string DeathLocation { get; set; } = string.Empty;

        public string PersonRegNumber { get; set; } = string.Empty; // New property
        public string PersonYearOfBirth { get; set; } = string.Empty; // New property

        public string OtherRelevantInformation { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        //public bool IsDeathRelated { get; set; } = true;


        //[NotMapped]
        //public ReportedDeath GetDeathDetails { get; set; }
    }
}
