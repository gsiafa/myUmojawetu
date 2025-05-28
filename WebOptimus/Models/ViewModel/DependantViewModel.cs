using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models.ViewModel
{
    public class DependantViewModel
    {
        [Key]
        public int Id { get; set; }

       
        public Guid UserId { get; set; } = Guid.Empty;

        public string PersonName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select Title")]
        public int DependentTitle { get; set; } // Store the ID of the Title

        [Required(ErrorMessage = "Please select Gender")]
        public int DependentGender { get; set; } // Store the ID of the Gender


        [Required(ErrorMessage ="Please select Title")]    
        public string Title { get; set; }
        [Required(ErrorMessage = "Please select Gender")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Year of Birth is required")]
        //[RegularExpression(@"^(\d{4})$", ErrorMessage = "Please enter a valid 4 digit Year of Birth")]
        [YearOfBirthValidation]
        public string PersonYearOfBirth { get; set; } = string.Empty;
        public string? PersonRegNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter name.")]
        public string Person11 { get; set; } = string.Empty;
        [YearOfBirthValidation]
        public string? Person11YearOfBirth { get; set; } = string.Empty;
        public string? Person11RegNumber { get; set; } = string.Empty;
        public string? Person2 { get; set; } = string.Empty;

        [DataType(DataType.PhoneNumber)]
        [UKPhoneNumber]
        public string? DependentPhoneNumber { get; set; } = string.Empty;

        public string? DependentEmail { get; set; }
        public IEnumerable<Dependant> dependants { get; set; }

        [NotMapped]
        public bool HasAccount { get; set; }


    }
}
