using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class ExemptDependantViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Reg Number")]
        public string PersonRegNumber { get; set; }

        [Display(Name = "Member Name")]
        public string PersonName { get; set; }

        [Display(Name = "Reduced Fees")]
        public decimal? ReduceFees { get; set; }

        [Display(Name = "Reason")]
        public string Reason { get; set; }

        [Display(Name = "Date Exempted")]
        public DateTime DateCreated { get; set; }
        [Display(Name = "Cause Ref")]
        public string CauseCampaignRef { get; set; }
        // Dropdown list for selecting dependents
        public List<SelectListItem> DependentsList { get; set; } = new();

    
    }
}
