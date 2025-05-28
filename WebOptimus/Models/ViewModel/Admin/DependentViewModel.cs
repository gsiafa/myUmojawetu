using WebOptimus.Migrations;

namespace WebOptimus.Models.ViewModel.Admin
{
    public class DependentViewModel
    {
        public int DependentId { get; set; }
        public string DependentName { get; set; }
        public string YearOfBirth { get; set; }
        public string Telephone { get; set; }
        public string PersonRegNumber { get; set; }
        public string Email { get; set; }

        public int Age { get; set; }



    }
}
