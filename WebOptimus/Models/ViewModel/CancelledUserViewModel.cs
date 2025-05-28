namespace WebOptimus.Models.ViewModel
{
    public class CancelledUserViewModel
    {
        public RequestToCancelMembership User { get; set; }
        public List<Dependant> FamilyMembers { get; set; }

        public string FullName { get; set; }
        public string YearOfBirth { get; set; }
        public DateTime DateJoined { get; set; }
        public string Phone  { get; set; }
        public string Email { get; set; }
        public string OutwardPostcode { get; set; }
    }
}
