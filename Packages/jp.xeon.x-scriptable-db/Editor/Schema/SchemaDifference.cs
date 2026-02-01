namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// スキーマ差分の詳細。
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
                SchemaDifferenceType.FieldAdded => $"フィールド追加: {FieldName} ({NewValue})",
                SchemaDifferenceType.FieldRemoved => $"フィールド削除: {FieldName} ({OldValue})",
                SchemaDifferenceType.FieldTypeChanged => $"型変更: {FieldName} ({OldValue} -> {NewValue})",
                SchemaDifferenceType.FieldAttributeChanged => $"属性変更: {FieldName} - {Description}",
                SchemaDifferenceType.PrimaryKeyChanged => $"PrimaryKey変更: {OldValue} -> {NewValue}",
                SchemaDifferenceType.SecondaryKeyAdded => $"SecondaryKey追加: {FieldName}",
                SchemaDifferenceType.SecondaryKeyRemoved => $"SecondaryKey削除: {FieldName}",
                _ => Description ?? "不明な差分"
            };
        }
    }
}
