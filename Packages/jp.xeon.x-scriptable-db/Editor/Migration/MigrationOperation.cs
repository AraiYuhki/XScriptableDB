using System;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Definition of a migration operation.
    /// </summary>
    [Serializable]
    public class MigrationOperation
    {
        public MigrationOperationType OperationType;
        public string FieldName;
        public string NewFieldName;
        public string NewTypeName;
        public string DefaultValue;
        public string TransformExpression;

        public override string ToString()
        {
            return OperationType switch
            {
                MigrationOperationType.AddField => $"Add: {FieldName} ({NewTypeName}) = {DefaultValue}",
                MigrationOperationType.RemoveField => $"Remove: {FieldName}",
                MigrationOperationType.RenameField => $"Rename: {FieldName} -> {NewFieldName}",
                MigrationOperationType.ChangeFieldType => $"ChangeType: {FieldName} -> {NewTypeName}",
                MigrationOperationType.SetDefaultValue => $"SetDefault: {FieldName} = {DefaultValue}",
                MigrationOperationType.TransformValue => $"Transform: {FieldName} ({TransformExpression})",
                MigrationOperationType.CopyField => $"Copy: {FieldName} -> {NewFieldName}",
                _ => "Unknown operation"
            };
        }
    }
}