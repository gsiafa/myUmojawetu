using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebOptimus.Models.ViewModel
{
    public class NewDependentViewModel
    {
        public Guid UserId { get; set; }
        public List<SelectListItem> Users { get; set; }
        public string PersonName { get; set; }
        public string PersonYearOfBirth { get; set; }
        public string? PersonRegNumber { get; set; } = string.Empty;
    }
}
