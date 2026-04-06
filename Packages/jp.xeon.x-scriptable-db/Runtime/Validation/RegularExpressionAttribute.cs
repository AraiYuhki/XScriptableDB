using System;
using System.Text.RegularExpressions;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// 正規表現パターンを指定する属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class RegularExpressionAttribute : ValidationAttribute
    {
        /// <summary>正規表現パターン</summary>
        public string Pattern { get; }

        public RegularExpressionAttribute(string pattern)
        {
            Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        }

        public override ValidationResult Validate(object value, string fieldName)
        {
            if (value == null)
                return ValidationResult.Success;

            var str = value.ToString();
            if (!Regex.IsMatch(str, Pattern))
                return ValidationResult.Error(fieldName, GetErrorMessage(fieldName));

            return ValidationResult.Success;
        }

        protected override string GetDefaultErrorMessage(string fieldName)
        {
            return $"The format of {fieldName} is invalid.";
        }
    }
}
