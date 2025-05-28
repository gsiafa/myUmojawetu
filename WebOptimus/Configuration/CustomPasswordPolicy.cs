namespace WebOptimus.Configuration
{
    using WebOptimus.Models;
    using Microsoft.AspNetCore.Identity;
    using System.Text.RegularExpressions;

        public class CustomPasswordPolicy : PasswordValidator<User>
        {
            public override async Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user, string password)
            {
                if (password == null)
                {
                    throw new ArgumentNullException(nameof(password));
                }

                IdentityResult result = await base.ValidateAsync(manager, user, password);
                List<IdentityError> errors = result.Succeeded ? new List<IdentityError>() : result.Errors.ToList();

                //if (password.Contains(user!.UserName!, StringComparison.OrdinalIgnoreCase))
                //{
                //    errors.Add(new IdentityError
                //    {
                //        Description = "Password cannot contain username/email"
                //    });
                //}

                const int minLength = 8;
                //check if password is empty or less than 8 charachters minimum
                if (string.IsNullOrEmpty(password) || password.Length < minLength)
                {
                    errors.Add(new IdentityError
                    {
                        Description = $"Password must be {minLength} or more characters in length and contain at least 1 lowercase (a-z), 1 uppercase (A-Z) and 1 digit (0-9) or special character (!@#$%^&?*.+)"
                    
                    });
                }

                
                // if password is 8 characters or more, it should contains at least 3 characters from any of these groups
                int counter = 0;
                List<string> patterns = new()
				{
                @"[a-z]",          // lowercase
                @"[A-Z]",          // uppercase
                @"[0-9]",          // digits
                @"[!@#$%^&*\(\)_\+\-\={}<>,\.\|""'~`:;\\?\/\[\] ]" // special symbols, including white space
                };

                // count type of different chars in password
                foreach (string p in patterns)
                {
                    if (Regex.IsMatch(password, p, RegexOptions.None, TimeSpan.FromSeconds(1)))
                    {
                        counter++;
                    }
                }
                if (counter < 3)
                {
                    errors.Add(new IdentityError
                    {
                        Description = $"Password must be {minLength} or more characters in length and contain at least 1 lowercase (a-z), 1 uppercase (A-Z) and 1 digit (0-9) or special character (!@#$%^&?*.+)"

                    });                    
                }
                return errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray());
            }
        }
    
}
