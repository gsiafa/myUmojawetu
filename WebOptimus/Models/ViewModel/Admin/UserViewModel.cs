namespace WebOptimus.Models.ViewModel.Admin
{
    public class UserViewModel
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string RegionName { get; set; }
        public string CityName { get; set; }
        public string YearOfBirth { get; set; } // For Dependents
        public string RegistrationNumber { get; set; } // For Dependents
        public string Role { get; set; } // For Users
    }
}
