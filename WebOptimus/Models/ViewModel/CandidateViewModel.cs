using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models.ViewModel
{
    public class CandidateViewModel
    {
        public int CandidateId { get; set; }
        public int ElectionId { get; set; }
        public string UserId { get; set; }
        public string CandidateDescription { get; set; }

        public string ElectionName { get; set; }
        // Read-only fields
        public string RegNumber { get; set; }
        public int YearOfBirth { get; set; }

        // List of users for the dropdown
        [BindNever]
        public IEnumerable<User> Users { get; set; }
        // List of elections for the Position dropdown
        public IEnumerable<SelectListItem> Elections { get; set; }
        [BindNever]
        public IEnumerable<SelectListItem> EditUsers { get; set; }
        public IFormFile? ImageFile { get; set; }
        public IFormFile? VideoFile { get; set; }

        // Existing files
        public string ExistingImagePath { get; set; }
        public string ExistingVideoPath { get; set; }

        [NotMapped]
        public string FullName { get;set; }
    }
}
