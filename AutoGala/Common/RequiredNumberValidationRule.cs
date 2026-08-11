using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Controls;

namespace AutoGala.Common
{
    public class RequiredNumberValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (string.IsNullOrWhiteSpace(value?.ToString()))
            {
                return new ValidationResult(false, "Value is required");
            }

            if (!double.TryParse(value.ToString(), out _))
            {
                return new ValidationResult(false, "Value must be a number");
            }

            return ValidationResult.ValidResult;
        }
    }
}
