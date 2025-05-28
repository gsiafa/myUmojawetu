using System.ComponentModel.DataAnnotations;


namespace WebOptimus.Models.ViewModel
{
    public class NextOfKinViewModels
    {
        [Key]
        public int Id { get; set; }

        public Guid UserId { get; set; } = Guid.Empty;

       
        public int DependentId { get; set; }
        [Display(Name = "Next of Kin Name")]
        public string NextOfKinName { get; set; } = string.Empty;
        [Display(Name = "Next of Kin Address")]
        public string NextOfKinAddress { get; set;} = string.Empty;
        [Display(Name = "Next of Kin Email")]
        public string? NextOfKinEmail { get; set;} = string.Empty;
        [Display(Name = "Next of Kin Tel")]
        public string NextOfKinTel { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please select a member")]
        public string PersonRegNumber {  get; set; } = string.Empty;    

    
        public IEnumerable<NextOfKin> NextOfKins { get; set; }




    }
}
