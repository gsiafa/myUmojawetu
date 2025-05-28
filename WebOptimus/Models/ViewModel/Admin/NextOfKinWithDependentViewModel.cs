namespace WebOptimus.Models.ViewModel.Admin
{
    public class NextOfKinWithDependentViewModel
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string DependentName { get; set; }
        public string NextOfKinName { get; set; }
        public string NextOfKinAddress { get; set; }
        public string NextOfKinEmail { get; set; }
        public string NextOfKinTel { get; set; }
        public string Relationship { get; set; }
        public string PersonRegNumber { get; set; } // Add this property
    }
}
