using System;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// 一意性制約を指定する属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class UniqueAttribute : ValidationAttribute
    {
        public override ValidationResult Validate(object value, string fieldName)
        {
            // 一意性は単一レコードでは検証できないため、
            // RecordValidatorがすべてのレコードにわたって検証します
            return ValidationResult.Success;
        }

        protected override string GetDefaultErrorMessage(string fieldName)
        {
            return $"The value of {fieldName} is duplicated.";
        }
    }
}
