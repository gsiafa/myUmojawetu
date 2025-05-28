using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyModel;
using WebOptimus.StaticVariables;


namespace WebOptimus.Models
{
    public class Successor 
    {
        [Key]
        public int Id { get; set; }
        public Guid UserId { get; set; } = Guid.NewGuid();
        public int? DependentId { get; set; }
        public string Relationship { get; set; } = string.Empty;
        public string Status { get; set; } = SuccessorStatus.AwaitingEmailConfirmation;

        public string Name { get;set; }
        public DateTime DateCreated { get; set; }
        public string? SuccessorTel { get; set; } = string.Empty;
        public string SuccessorEmail { get; set; } = string.Empty;
        public string? CreatedBy { get; set; }

        public DateTime? UpdateOn { get; set; }

        public bool IsTakeOver { get; set; } = false; // Flag to indicate if the successor has taken over
        public DateTime? TakeOverDate { get; set; } // Date when the successor takes over


    }
}
