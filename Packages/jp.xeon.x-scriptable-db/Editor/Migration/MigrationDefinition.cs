using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// マイグレーション操作の種類。
    /// </summary>
    public enum MigrationOperationType
    {
        AddField,
        RemoveField,
        RenameField,
        ChangeFieldType,
        SetDefaultValue,
        TransformValue,
        CopyField
    }

    /// <summary>
    /// マイグレーション操作の定義。
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

    /// <summary>
    /// マイグレーション定義。
    /// </summary>
    [CreateAssetMenu(fileName = "NewMigration", menuName = "XScriptableDB/Migration Definition")]
    public class MigrationDefinition : ScriptableObject
    {
        [Header("基本情報")]
        public string MigrationName;
        public string Description;
        public int Version;

        [Header("対象テーブル")]
        public string SourceTableType;
        public string TargetTableType;

        [Header("操作")]
        public List<MigrationOperation> Operations = new();

        [Header("メタデータ")]
        public string CreatedAt;
        public string CreatedBy;
        public bool IsApplied;
        public string AppliedAt;
    }
}
