using System;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// Attribute that specifies a comparison constraint against another field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class CompareAttribute : ValidationAttribute
    {
        /// <summary>Name of the field to compare against</summary>
        public string OtherField { get; }

        /// <summary>Comparison operator</summary>
        public CompareOperator Operator { get; set; } = CompareOperator.Equal;

        public CompareAttribute(string otherField)
        {
            OtherField = otherField ?? throw new ArgumentNullException(nameof(otherField));
        }

        public override ValidationResult Validate(object value, string fieldName)
        {
            // Cross-field comparison cannot be performed on a single record,
            // so it is validated by RecordValidator
            return ValidationResult.Success;
        }

        protected override string GetDefaultErrorMessage(string fieldName)
        {
            var opStr = Operator switch
            {
                CompareOperator.Equal => "equal to",
                CompareOperator.NotEqual => "not equal to",
                CompareOperator.LessThan => "less than",
                CompareOperator.LessThanOrEqual => "less than or equal to",
                CompareOperator.GreaterThan => "greater than",
                CompareOperator.GreaterThanOrEqual => "greater than or equal to",
                _ => "?"
            };
            return $"{fieldName} must be {opStr} {OtherField}.";
        }
    }
}
