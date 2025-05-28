using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models
{
    public class ConfirmedDeath
    {
        [Key]
        public int Id { get; set; }

        public Guid UserId { get; set; } = Guid.Empty;
        public int? DeathId { get; set; }
        public string PersonName { get; set; } = string.Empty;
        public string PersonYearOfBirth { get; set; } = string.Empty;
        public string PersonRegNumber { get; set; } = string.Empty;
        public bool IsConfirmedDead { get; set; }
        public int? Title { get; set; }
        public int? Gender { get; set; }
        public string? Telephone { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }

        public int? RegionId { get; set; }

        public int? CityId { get; set; }
        public string? CreatedBy { get; set; }

        public DateTime? UpdateOn { get; set; }

    }
}
