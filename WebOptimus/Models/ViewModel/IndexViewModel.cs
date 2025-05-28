


namespace WebOptimus.Models.ViewModel
{
    public class IndexViewModel
    {
      
        public int visitors { get; set; }
        public IEnumerable<User> Users { get; set; }
        public IEnumerable<Dependant> dependants { get; set; }
        public List<under18ViewModel> Usersunder18 { get; set; }
        //public IEnumerable<User> LiveUsers { get; set; }

        public IEnumerable<Payment> Payments { get; set; }

        public string IsRole { get; set; }
    }

  
    public class under18ViewModel
    {
        public string Email { get; set; }
        public string FullName { get; set; }
        public string DependentName { get; set; }
        public string YearOfBirth { get; set; }
        public int Age { get; set; }
        public string Role { get; set; }

        public string RegionName { get; set; } // Add this property
        public string PersonRegNumber { get; set; } // Add this property
    }



}
