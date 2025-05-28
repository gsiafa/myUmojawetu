using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models.ViewModel
{
    public class SettingViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public bool IsActive { get; set; }

        public int MinimumAge { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
