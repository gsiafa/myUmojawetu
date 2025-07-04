﻿
using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class DonorViewModel
    {
        
        public Guid UserId { get; set; } = Guid.Empty;

        public string FirstName { get; set; }


        public string LastName { get; set; }

        public decimal Amount { get; set; }

        public string PhoneNumber { get; set; }

        public string Email { get; set; }

        [Required]
        public bool IsAnonymous { get;set; }
        public string? HasPaid { get; set; } = string.Empty;
        public string CauseCampaignpRef { get; set; } = string.Empty;

        public DateTime DateCreated { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? UpdateOn { get; set; }

    }
}
