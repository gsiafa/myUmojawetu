using System.ComponentModel.DataAnnotations;
using WebOptimus.Migrations;

namespace WebOptimus.Models.ViewModel
{
    public class UserRoleViewModel
    {
        public Guid UserId { get; set; }
        //public int DependentId { get;set; }
        public string PersonRegNumber { get; set; } = string.Empty;
        public string UserName { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public string SelectedRole { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string OldRole { get; set; }


    }
}
