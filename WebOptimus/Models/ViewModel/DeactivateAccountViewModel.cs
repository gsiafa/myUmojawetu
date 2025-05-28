namespace WebOptimus.Models.ViewModel
{
    public class DeactivateAccountViewModel
    {
        public string PersonRegNumber { get; set; }
        public Guid UserId { get; set; }
        public string DeactivationReason { get; set; }
        public bool DeactivateWithDependents { get; set; } = false; 
    }

}
