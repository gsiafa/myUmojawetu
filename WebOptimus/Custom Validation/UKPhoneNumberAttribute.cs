using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

public class UKPhoneNumberAttribute : ValidationAttribute
{
    public override bool IsValid(object value)
    {
        if (value == null)
        {
            return true; // Not validating null values, use [Required] for that
        }


        var phoneNumber = value.ToString().Replace(" ", string.Empty); // Remove spaces for validation
        var regex = new Regex(@"^(\+447\d{9}|07\d{9})$");
        return regex.IsMatch(phoneNumber);
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field is not a valid UK phone number.";
    }
}
