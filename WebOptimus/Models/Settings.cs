using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models
{
    public class Settings
    {
        [Key]
        public int Id { get; set; }
        //public bool EnableElectionResults { get; set; }
        public string Name { get; set; }

        public bool IsActive { get; set; }
        public int MinimumAge { get; set; } = 18;

        public DateTime DateCreated { get; set; }

     
    }
}
