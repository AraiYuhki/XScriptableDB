namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Details of a schema difference.
    /// </summary>
    public class SchemaDifference
    {
        public SchemaDifferenceType Type { get; }
        public string FieldName { get; }
        public string OldValue { get; }
        public string NewValue { get; }
        public string Description { get; }

        public SchemaDifference(SchemaDifferenceType type, string fieldName, string oldValue, string newValue) : this(type, fieldName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public SchemaDifference(SchemaDifferenceType type, string fieldName, bool sourceIsPrimary, bool targetIsPrimary) : this(type, fieldName)
        {
            OldValue = sourceIsPrimary ? fieldName : "(none)";
            NewValue = targetIsPrimary ? fieldName : "(none)";
        }

        public SchemaDifference(SchemaDifferenceType type, string fieldName)
        {
            Type = type;
            FieldName = fieldName;
        }

        public override string ToString()
        {
            return Type switch
            {
                SchemaDifferenceType.FieldAdded => $"Field Added: {FieldName} ({NewValue})",
                SchemaDifferenceType.FieldRemoved => $"Field Removed: {FieldName} ({OldValue})",
                SchemaDifferenceType.FieldTypeChanged => $"Type Changed: {FieldName} ({OldValue} -> {NewValue})",
                SchemaDifferenceType.FieldAttributeChanged => $"Attribute Changed: {FieldName} - {Description}",
                SchemaDifferenceType.PrimaryKeyChanged => $"PrimaryKey Changed: {OldValue} -> {NewValue}",
                SchemaDifferenceType.SecondaryKeyAdded => $"SecondaryKey Added: {FieldName}",
                SchemaDifferenceType.SecondaryKeyRemoved => $"SecondaryKey Removed: {FieldName}",
                _ => Description ?? "Unknown difference"
            };
        }
    }
}
