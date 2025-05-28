using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class AddNextOfKinViewModel
    {
        public int Id { get; set; }

        public Guid UserId { get; set; }

        //[Required(ErrorMessage = "Dependent is required")]
        //public int DependentId { get; set; }

        [Display(Name = "Dependent Reg Number")]
        public string DependentRegNumber { get; set; }

        [Required(ErrorMessage = "Next of Kin Name is required")]
        public string NextOfKinName { get; set; }

        [Required(ErrorMessage = "Relationship is required")]
        public string Relationship { get; set; }

        [Required(ErrorMessage = "Next of Kin Tel is required")]
        public string NextOfKinTel { get; set; }

        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string? NextOfKinEmail { get; set; } = string.Empty;

        public string NextOfKinAddress { get; set; } = string.Empty;

        public string PersonRegNumber { get; set; } = string.Empty;
        public string DependentName { get; set; } = string.Empty;
        public List<SelectListItem> Dependents { get; set; } = new List<SelectListItem>();
    }
}
