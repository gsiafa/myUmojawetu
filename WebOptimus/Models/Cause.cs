using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebOptimus.Models.ViewModel;

namespace WebOptimus.Models
{
    public class Cause
    {
        [Key]
        public int Id { get; set; }

        public Guid UserId { get; set; } = Guid.Empty;

        public string Summary { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? DependentId { get; set; }
        public int? DeathId { get; set; }

        [DataType(DataType.Currency)]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = true)]
        public decimal TargetAmount { get; set; }  
        public string PersonRegNumber { get; set; }
        [NotMapped]
        public decimal? Goodwill { get; set; }
        [NotMapped]
        public decimal? BaseAmount { get; set; }
        public decimal FullMemberAmount { get; set; }
        public int UnderAge { get; set; }
        public decimal UnderAgeAmount { get; set; }
        public bool IsDisplayable { get; set; } = true;

        public bool IsActive { get; set; } = false;

        public string CauseCampaignpRef { get; set; } = string.Empty;

        public DateTime DateCreated { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? CreatedBy { get; set; }
        public decimal MissPaymentAmount { get; set; }
        public bool IsClosed { get; set; } = false;
        public DateTime IsClosedDate { get; set; }

        [NotMapped]
        public DateTime? UpdatedOn { get; set; } // Consistent naming convention

        [NotMapped]
        public string DeceasedPhotoPath { get; set; } = string.Empty; // This property is not mapped to the database

        [NotMapped]
        public bool Allover18DependentsPaid { get; set; }

        [NotMapped]
        public decimal AmountRaised { get; set; }
    }
}
