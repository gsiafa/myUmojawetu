namespace WebOptimus.Models.ViewModel
{
    public class UserWithDependentsViewModel
    {
        public User User { get; set; }
        public List<DependentWithAgeViewModel> Dependents { get; set; }
    }

    public class DependentWithAgeViewModel
    {
        public Dependant Dependent { get; set; }
        public int Age { get; set; }
    }

}
