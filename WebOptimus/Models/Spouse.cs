using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models
{
    public class Spouse
    {
        [Key]
        public int Id { get; set; }


        public Guid UserId { get; set; } = Guid.Empty;

        public string MaritalStatus { get; set; } = string.Empty;
        public string? SpouseName { get; set; } = string.Empty;

        public string? SpouseAddress { get; set;} = string.Empty;

        public string? SpouseCity { get; set; } = string.Empty;

        public string? SpouseRegion { get; set; } = string.Empty;

        public string? SpousePostcode { get; set;} = string.Empty;

        public string? SpouseEmail { get; set; } = string.Empty;   

        public bool? IsLivingTogether { get; set; }
    }
}
