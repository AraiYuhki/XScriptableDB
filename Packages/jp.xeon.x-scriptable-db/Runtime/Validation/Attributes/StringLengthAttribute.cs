using System;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// 文字列の長さを指定する属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class StringLengthAttribute : ValidationAttribute
    {
        /// <summary>最小長</summary>
        public int MinimumLength { get; set; } = 0;

        /// <summary>最大長</summary>
        public int MaximumLength { get; }

        public StringLengthAttribute(int maximumLength)
        {
            MaximumLength = maximumLength;
        }

        public override ValidationResult Validate(object value, string fieldName)
        {
            if (value == null)
                return ValidationResult.Success;

            var str = value.ToString();
            var length = str.Length;

            if (length < MinimumLength || length > MaximumLength)
                return ValidationResult.Error(fieldName, GetErrorMessage(fieldName));

            return ValidationResult.Success;
        }

        protected override string GetDefaultErrorMessage(string fieldName)
        {
            if (MinimumLength > 0)
                return $"{fieldName} は {MinimumLength} 文字から {MaximumLength} 文字の間である必要があります。";

            return $"{fieldName} は {MaximumLength} 文字以内である必要があります。";
        }
    }
}
