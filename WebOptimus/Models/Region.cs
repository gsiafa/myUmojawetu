using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models
{
    public class Region
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;



        public DateTime DateCreated { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? UpdateOn { get; set; }


    }
}
