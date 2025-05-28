using Microsoft.Extensions.DependencyModel;

namespace WebOptimus.Models.ViewModel
{
    public class UserDependentsViewModel
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public List<Dependant> Dependents { get; set; }


        public int TotalUsers { get; set; } // New property for total users
        public int TotalDependents { get; set; } // New property for total dependents
        public int under18DependentsCount { get; set; } // New property for under 25 dependents
    }
}
