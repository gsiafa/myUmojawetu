using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebOptimus.Models.ViewModel;

namespace WebOptimus.Models
{
    public class NoteType
    {
        [Key]
        public int Id { get; set; }    

        [Required]
        [StringLength(100)]
        public string TypeName { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }

    }
}
