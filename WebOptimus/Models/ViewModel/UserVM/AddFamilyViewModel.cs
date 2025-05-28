using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel.UserVM
{
    public class AddFamilyViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [YearOfBirthValidation]
       
        public string YOB { get; set; } = string.Empty;
    }
}
