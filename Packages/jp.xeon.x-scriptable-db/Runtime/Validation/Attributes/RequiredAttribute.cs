using System;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// 必須フィールドを指定する属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class RequiredAttribute : ValidationAttribute
    {
        /// <summary>空文字列を許可するか（デフォルト: false）</summary>
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
            return $"{fieldName} は必須です。";
        }
    }
}
