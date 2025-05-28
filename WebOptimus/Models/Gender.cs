using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models
{
    public class Gender
    {
        [Key]
        public int Id { get; set; }
        public string GenderName { get; set; } = string.Empty;



        public DateTime DateCreated { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? UpdateOn { get; set; }


    }
}
