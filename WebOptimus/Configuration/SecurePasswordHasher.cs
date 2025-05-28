


namespace WebOptimus.Configuration
{
    public sealed class SecurePasswordHasher
    {
        /// <returns>the hash</returns>
        public static string Hash(string password, int iterations)
        {
            string savedPasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

            return savedPasswordHash;
        }

        public static string Hash(string password)
        {
            return Hash(password, 10000);
        }

        public static bool Verify(string password, string hashedPassword)
        {
            bool verified = BCrypt.Net.BCrypt.Verify(password, hashedPassword);

            if (verified is true)
            {
                return true;
            }
            else
            {
                return false;
            }

        }
    }
}