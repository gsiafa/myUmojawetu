namespace WebOptimus.Models.ViewModel
{
    public class ManageDependentsViewModel
    {
        public int DependentId { get; set; }
        public string PersonName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string YearOfBirth { get; set; }
        public string RegNumber { get; set; }
        public bool HasAccount { get; set; }
    }
}
