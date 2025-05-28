using System.Globalization;

namespace WebOptimus.Models
{
    public class AgeHelper
    {
        public static int CalculateAge(string dateOfBirth)
        {
            if (string.IsNullOrWhiteSpace(dateOfBirth)) return -1;

            if (int.TryParse(dateOfBirth, out int year))
            {
                var today = DateTime.Today;
                return today.Year - year;
            }

            DateTime dob;
            var formats = new[] { "d/M/yyyy" };
            if (DateTime.TryParseExact(dateOfBirth, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dob))
            {
                var today = DateTime.Today;
                var age = today.Year - dob.Year;
                if (dob.Date > today.AddYears(-age)) age--; // Adjust age if birthday hasn't occurred this year
                return age;
            }

            return -1;
        }
    }
}
