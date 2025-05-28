using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models
{
    public class SeparationHistory
    {
        [Key]
        public int Id { get; set; }

      
        public Guid OldUserId { get; set; }

        public Guid NewUserId { get; set; }

    
        public int DependentId { get; set; }

     
        public string DependentName { get; set; }

      
        public DateTime SeparationDate { get; set; }

        public string SeparatedBy { get; set; }

        public string OldUserFirstName { get; set; }

     
        public string OldUserSurname { get; set; }

        public string OldUserEmail { get; set; }

   
        public string NewUserFirstName { get; set; }

   
        public string NewUserSurname { get; set; }

    
        public string NewUserEmail { get; set; }

        public int? OldNumberOfDependants { get; set; }

        public int? NewNumberOfDependants { get; set; }

        public string Notes { get; set; }
    }
}
