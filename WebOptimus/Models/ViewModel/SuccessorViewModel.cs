using System.ComponentModel.DataAnnotations;


namespace WebOptimus.Models.ViewModel
{
    public class SuccessorViewModel
    {
        [Key]
        public int Id { get; set; }

        public Guid UserId { get; set; } = Guid.Empty;

        [Required(ErrorMessage = "Please select a member")]
        public int DependentId { get; set; }

        [Display(Name = "Successor's Name")]
        [Required(ErrorMessage = "Please enter the successor's name")]
        public string Name { get; set; } = string.Empty;

        public string SuccessorTo { get; set; } = string.Empty;

        [Display(Name = "Successor's Email")]
        [Required(ErrorMessage = "Please enter the successor's email")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string? Email { get; set; } = string.Empty;

        [Display(Name = "Successor Tel")]
        public string SuccessorTel { get; set; } = string.Empty;



        [Required(ErrorMessage = "Please enter the relationship")]
        public string Relationship { get; set; } = string.Empty;

        public string Status { get; set; }  
        public bool IsTakeOver { get; set; }
        public IEnumerable<Successor> Successors { get; set; }

      
    
  
    }

}
