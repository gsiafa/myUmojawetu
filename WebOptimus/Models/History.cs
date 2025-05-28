using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models
{
    public class History
    {
        [Key]
        public int Id { get; set; }

        public string? Name { get; set; }
        public string? Browser { get; set; }
        [Display(Name="OS")]
        public string? OperatingSystem { get; set; }
        public string? Device { get; set; }
        [Display(Name = "IP Address")]
        public string? PublicIP { get; set; }

         
        public DateTime CreatedDate { get; set; }
    }
}
