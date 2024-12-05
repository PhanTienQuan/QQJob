using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace QQJob.Data.CValidation
{
    public class Password : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            var password = value as string;
            if (string.IsNullOrEmpty(value?.ToString()))
            {
                ErrorMessage = "Passwords is required.";
                return false;
            }

            // Check for minimum length
            if (password.Length < 6)
            {
                ErrorMessage = "Passwords must be at least 6 characters.";
                return false;
            }

            // Check for at least one uppercase letter
            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                ErrorMessage = "Passwords must have at least one uppercase ('A'-'Z').";
                return false;
            }

            // Check for at least one lowercase letter
            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                ErrorMessage = "Passwords must have at least one lowercase ('a'-'z').";
                return false;
            }

            // Check for at least one non-alphanumeric character
            if (!Regex.IsMatch(password, @"[\W_]"))
            {
                ErrorMessage = "Passwords must have at least one non-alphanumeric character.";
                return false;
            }

            return true;
        }
    }
}
