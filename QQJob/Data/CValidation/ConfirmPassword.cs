using System.ComponentModel.DataAnnotations;

namespace QQJob.Data.CValidation
{
    public class ConfirmPassword(string otherPropertyName) : ValidationAttribute
    {
        private readonly string _otherPropertyName = otherPropertyName;

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var otherProperty = validationContext.ObjectType.GetProperty(_otherPropertyName);
            if(otherProperty == null)
            {
                return new ValidationResult($"Property '{_otherPropertyName}' not found.");
            }

            var otherPropertyValue = otherProperty.GetValue(validationContext.ObjectInstance);

            // Check if the value is null or empty
            if(string.IsNullOrEmpty(value?.ToString()))
            {
                return new ValidationResult("Confirm password is required.");
            }

            // Check if the values match
            if(!Equals(value.ToString(), otherPropertyValue?.ToString()))
            {
                return new ValidationResult("Password and confirmation password do not match.");
            }

            return ValidationResult.Success;
        }
    }
}