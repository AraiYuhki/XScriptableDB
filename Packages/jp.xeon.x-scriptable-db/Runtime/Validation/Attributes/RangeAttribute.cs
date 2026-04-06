using System;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// 数値の範囲を指定する属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class RangeAttribute : ValidationAttribute
    {
        /// <summary>最小値</summary>
        public double Minimum { get; }

        /// <summary>最大値</summary>
        public double Maximum { get; }

        public RangeAttribute(double minimum, double maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public RangeAttribute(int minimum, int maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public override ValidationResult Validate(object value, string fieldName)
        {
            if (value == null)
                return ValidationResult.Success; // nullは別のバリデーションで処理

            double numValue;
            try
            {
                numValue = Convert.ToDouble(value);
            }
            catch
            {
                return ValidationResult.Error(fieldName, $"{fieldName} must be a numeric value.");
            }

            if (numValue < Minimum || numValue > Maximum)
                return ValidationResult.Error(fieldName, GetErrorMessage(fieldName));

            return ValidationResult.Success;
        }

        protected override string GetDefaultErrorMessage(string fieldName)
        {
            return $"{fieldName} must be between {Minimum} and {Maximum}.";
        }
    }
}
