namespace WebOptimus.Models.ViewModel
{
    using Microsoft.AspNetCore.Mvc.Rendering;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class EditViewModel
    {
        public Guid UserId { get; set; } = Guid.Empty;

        public string Id { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please enter your First Name"), Display(Name ="First Name")]
        public string FirstName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please enter your Last Name"), Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;

        [Display(Name = "Email Address"), EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;
      
        [StringLength(15), Display(Name = "Phone Number"), RegularExpression("^[0-9 ]*$", ErrorMessage = "Contact Number must be numeric and cannot contain a space.")]
        [Required(ErrorMessage = "Please enter Telephone Number")]
        public string PhoneNumber { get; set; } = string.Empty;
        [NotMapped]
        public string OldEmail { get; set; } = string.Empty;

        public int Status { get; set; }

        public DateTime DateCreated { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? UpdateOn { get; set; }

        public DateTime LastPasswordChangedDate { get; set; }

        public bool? ForcePasswordChange { get; set; }




        [NotMapped]
        [Display(Name = "Select role")]
        public string? RoleId { get; set; } = string.Empty;
        public IEnumerable<SelectListItem>? RoleList { get; set; }
     

 
    }
}
