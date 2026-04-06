using System;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// 外部キー制約を指定する属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ForeignKeyAttribute : ValidationAttribute
    {
        /// <summary>参照先テーブルの型</summary>
        public Type ReferenceTableType { get; }

        /// <summary>参照先のキーフィールド名（省略時はPrimaryKey）</summary>
        public string ReferenceKeyField { get; set; }

        public ForeignKeyAttribute(Type referenceTableType)
        {
            ReferenceTableType = referenceTableType ?? throw new ArgumentNullException(nameof(referenceTableType));
        }

        public override ValidationResult Validate(object value, string fieldName)
        {
            // 外部キーの検証は参照先テーブルが必要なため、
            // RecordValidatorで検証する
            return ValidationResult.Success;
        }

        protected override string GetDefaultErrorMessage(string fieldName)
        {
            return $"The referenced record for {fieldName} does not exist.";
        }
    }
}
