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
            // 一意性の検証はレコード単体では行えないため、
            // RecordValidatorで全レコードを対象に検証する
            return ValidationResult.Success;
        }

        protected override string GetDefaultErrorMessage(string fieldName)
        {
            return $"{fieldName} の値が重複しています。";
        }
    }
}
