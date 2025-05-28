using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models.ViewModel
{
    public class DependentWithKinViewModel

    {
       
        public int Id { get; set; }
        public string NextOfKinName { get; set; } = string.Empty;

        public string PersonRegNumber { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

    }
}
