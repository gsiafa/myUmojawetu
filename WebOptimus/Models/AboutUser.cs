using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models
{
    public class AboutUser 
    {
        [Key]
        public int Id { get; set; }
        public Guid UserId { get; set; } = Guid.Empty;        
        public DateTime DateOfBirth { get; set; }

        public string FirstLineOfAddress { get; set;} = string.Empty;
        public string City { get; set;} = string.Empty;

        public string Region { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;
        

        public DateTime DateCreated { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? UpdateOn { get; set; }

    }
}
