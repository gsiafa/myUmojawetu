namespace WebOptimus.Models.ViewModel
{
    public class UnverifiedUserDetailsViewModel
    {
        public User User { get; set; } // The unverified user
        public List<User> PossibleMatches { get; set; } // Possible matching users
    }
}
