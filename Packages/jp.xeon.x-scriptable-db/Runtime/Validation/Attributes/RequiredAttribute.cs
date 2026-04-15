using System;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// Attribute to mark a field as required.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class RequiredAttribute : ValidationAttribute
    {
        /// <summary>Whether to allow empty strings (default: false)</summary>
        public bool AllowEmptyString { get; set; } = false;

        public override ValidationResult Validate(object value, string fieldName)
        {
            if (value == null)
                return ValidationResult.Error(fieldName, GetErrorMessage(fieldName));

            if (!AllowEmptyString && value is string str && string.IsNullOrEmpty(str))
                return ValidationResult.Error(fieldName, GetErrorMessage(fieldName));

            return ValidationResult.Success;
        }

        protected override string GetDefaultErrorMessage(string fieldName)
        {
            return $"{fieldName} is required.";
        }
    }
}
