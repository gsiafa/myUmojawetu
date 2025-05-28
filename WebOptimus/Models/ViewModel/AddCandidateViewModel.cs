using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebOptimus.Models.ViewModel
{
    public class AddCandidateViewModel
    {
        public string UserId { get; set; }  // Selected candidate (from Users table)

        public int ElectionId { get; set; }  // Selected election (from Elections table)

        public string CandidateDescription { get; set; }  // Description of the candidate

        public IFormFile? ImageFile { get; set; }
        public IFormFile? VideoFile { get; set; }

        public DateTime DateRegistered { get; set; } = DateTime.Now;

        [BindNever]
        public IEnumerable<User> Users { get; set; }
        // List of elections for the Position dropdown
        public IEnumerable<SelectListItem> Elections { get; set; }

        public int YearOfBirth { get; set; }
        public string RegNumber { get; set; }
    }
}
