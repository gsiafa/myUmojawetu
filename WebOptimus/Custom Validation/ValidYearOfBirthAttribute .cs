using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

public class YearOfBirthValidation : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return new ValidationResult("Year of Birth is required.");
        }

        int year;
        bool isNumeric = int.TryParse(value.ToString(), out year);

        if (!isNumeric)
        {
            return new ValidationResult("Year of Birth must be valid.");
        }

        if (year < 1900 || year > DateTime.Now.Year)
        {
            return new ValidationResult("Year of Birth must be a valid 4-digit year.");
        }

        return ValidationResult.Success;
    }
}