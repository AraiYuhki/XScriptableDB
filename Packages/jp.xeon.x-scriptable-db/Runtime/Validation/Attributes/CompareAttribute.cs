using System;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// 他のフィールドとの比較を指定する属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class CompareAttribute : ValidationAttribute
    {
        /// <summary>比較対象のフィールド名</summary>
        public string OtherField { get; }

        /// <summary>比較演算子</summary>
        public CompareOperator Operator { get; set; } = CompareOperator.Equal;

        public CompareAttribute(string otherField)
        {
            OtherField = otherField ?? throw new ArgumentNullException(nameof(otherField));
        }

        public override ValidationResult Validate(object value, string fieldName)
        {
            // フィールド間の比較はレコード単体では行えないため、
            // RecordValidatorで検証する
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
