namespace WebOptimus.Models.ViewModel
{
    public class EditDependentViewModel
    {
        public ManageDependentsViewModel Dependent { get; set; }
        public List<ManageDependentsViewModel> RelatedFamilyMembers { get; set; } = new List<ManageDependentsViewModel>();
    }
}
