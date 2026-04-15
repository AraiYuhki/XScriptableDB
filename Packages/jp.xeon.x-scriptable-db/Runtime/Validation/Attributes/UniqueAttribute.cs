using System;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// Attribute that specifies a uniqueness constraint.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class UniqueAttribute : ValidationAttribute
    {
        public override ValidationResult Validate(object value, string fieldName)
        {
            // Uniqueness cannot be validated on a single record,
            // so RecordValidator validates it across all records
            return ValidationResult.Success;
        }

        protected override string GetDefaultErrorMessage(string fieldName)
        {
            return $"The value of {fieldName} is duplicated.";
        }
    }
}
