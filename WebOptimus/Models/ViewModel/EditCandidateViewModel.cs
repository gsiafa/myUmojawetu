using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebOptimus.Models.ViewModel
{
    public class EditCandidateViewModel
    {
        public int CandidateId { get; set; }
        public string UserId { get; set; }
        public string RegNumber { get; set; }
        public int YearOfBirth { get; set; }
        public int ElectionId { get; set; }
        public string CandidateDescription { get; set; }
        public string ExistingImagePath { get; set; }
        public string ExistingVideoPath { get; set; }

        public IEnumerable<SelectListItem> EditUsers { get; set; }
        public IEnumerable<SelectListItem> Elections { get; set; }

        public IFormFile ImageFile { get; set; }
        public IFormFile VideoFile { get; set; }
    }
}
