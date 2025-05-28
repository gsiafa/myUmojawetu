namespace WebOptimus.Models.ViewModel
{
    public class RequestAccountDetailsViewModel
    {
        public int DependentId { get; set; }
        public string DependentName { get; set; }
        public string Email { get; set; }
        public string TemporaryPassword { get; set; }
        public string CurrentAccountHolderName { get; set; } // Add this property
        public Guid UserId { get; set; }
        public List<DependentsViewModel> Dependents { get; set; } = new List<DependentsViewModel>();
    }

    

}
