using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models
{
    [Table("Dependants")]
    public class Dependant
    {
        [Key]
        public int Id { get; set; }

        public Guid UserId { get; set; } = Guid.Empty;
        public int? DeathId { get; set; }

        public string PersonName { get; set; } = string.Empty;

        [Required (ErrorMessage = "Year of Birth is required")]
        [RegularExpression(@"^(\d{4})$", ErrorMessage = "Please enter a valid 4 digit Year of Birth")]
        public string PersonYearOfBirth { get; set; } = string.Empty;



        public string PersonRegNumber { get; set; } = string.Empty;

        public bool IsReportedDead { get; set; }
        public int? Title { get; set; }
        public int? Gender { get; set; }
        public string? Telephone { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }

        public int? RegionId { get; set; }
        public string OutwardPostcode { get; set; } = string.Empty;

        public int? CityId { get; set; }
        public string? CreatedBy { get; set; }

        public bool? IsActive { get; set; } = true;
        public bool? HasChangedFamily { get; set; } = false;

        public Guid OldUserId { get; set; } = Guid.Empty;
        public DateTime? UpdateOn { get; set; }

        public string? DeactivationReason { get; set; } = string.Empty;
        public DateTime? DeactivationDate { get; set; }
        [NotMapped]
        public virtual ICollection<NextOfKin> NextOfKins { get; set; }
        [NotMapped]
        public string TitleName { get; set; }

        [NotMapped]
        public string GenderName { get; set; }

        [NotMapped]
        public string RegionName { get; set; }

        [NotMapped]
        public string CityName { get; set; }

        [NotMapped]
        public string Role { get; set; }

        [NotMapped]
        public bool HasAccount { get; set; }

    }
}
