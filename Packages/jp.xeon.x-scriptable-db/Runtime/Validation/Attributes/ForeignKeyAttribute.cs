using System;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// 外部キー制約を指定する属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ForeignKeyAttribute : ValidationAttribute
    {
        /// <summary>参照先のテーブルの型</summary>
        public Type ReferenceTableType { get; }

        /// <summary>参照先テーブルのキーフィールド名（デフォルトはPrimaryKey）</summary>
        public string ReferenceKeyField { get; set; }

        public ForeignKeyAttribute(Type referenceTableType)
        {
            ReferenceTableType = referenceTableType ?? throw new ArgumentNullException(nameof(referenceTableType));
        }

        public override ValidationResult Validate(object value, string fieldName)
        {
            // 外部キーの検証には参照先のテーブルが必要なため、
            // RecordValidatorによって検証されます
            return ValidationResult.Success;
        }

        protected override string GetDefaultErrorMessage(string fieldName)
        {
            return $"The referenced record for {fieldName} does not exist.";
        }
    }
}
