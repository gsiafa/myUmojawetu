namespace WebOptimus.Models.ViewModel
{
    public class DependentAccountViewModel
    {
        public int DependentId { get; set; }
        public string PersonName { get; set; }
        public string PersonYearOfBirth { get; set; }
        public string Telephone { get; set; }
        public string RegNumber { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public bool HasAccount { get; set; }
    }
}
