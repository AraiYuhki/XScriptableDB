using System;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// Attribute that specifies a foreign key constraint.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ForeignKeyAttribute : ValidationAttribute
    {
        /// <summary>Type of the referenced table</summary>
        public Type ReferenceTableType { get; }

        /// <summary>Name of the key field in the referenced table (defaults to PrimaryKey)</summary>
        public string ReferenceKeyField { get; set; }

        public ForeignKeyAttribute(Type referenceTableType)
        {
            ReferenceTableType = referenceTableType ?? throw new ArgumentNullException(nameof(referenceTableType));
        }

        public override ValidationResult Validate(object value, string fieldName)
        {
            // Foreign key validation requires the referenced table,
            // so it is validated by RecordValidator
            return ValidationResult.Success;
        }

        protected override string GetDefaultErrorMessage(string fieldName)
        {
            return $"The referenced record for {fieldName} does not exist.";
        }
    }
}
