using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebOptimus.Models
{
    public class DeletedUsers
    {
        [Key]
        public int Id { get; set; }
        public Guid UserId { get; set; }

        public int? DependentId { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; }
        public int Title { get; set; }

        [MaxLength(50)]
        public string ApplicationStatus { get; set; } = string.Empty;

        public int? SuccessorId { get; set; } // SuccessKey

        [MaxLength(500)]
        public string? Note { get; set; } = string.Empty;

        public DateTime? NoteDate { get; set; }

        [MaxLength(100)]
        public string? ApprovalDeclinerName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ApprovalDeclinerEmail { get; set; } = string.Empty;

        public bool IsConsent { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime DateDeleted { get; set; } = DateTime.UtcNow;


        [MaxLength(100)]
        public string SponsorsMemberName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string SponsorsMemberNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string SponsorLocalAdminName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string SponsorLocalAdminNumber { get; set; } = string.Empty;

        [MaxLength(10)]
        public string PersonYearOfBirth { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PersonRegNumber { get; set; } = string.Empty;

        public int? RegionId { get; set; }

        public bool? IsDeleted { get; set; }

        public bool? IsDeceased { get; set; }

        public int? CityId { get; set; }

        [MaxLength(20)]
        public string OutwardPostcode { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

    }
}
