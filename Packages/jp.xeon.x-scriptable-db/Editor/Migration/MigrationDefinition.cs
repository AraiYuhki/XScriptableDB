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
    /// マイグレーション定義。
    /// </summary>
    [CreateAssetMenu(fileName = "NewMigration", menuName = "XScriptableDB/Migration Definition")]
    public class MigrationDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        public string MigrationName;
        public string Description;
        public int Version;

        [Header("Target Table")]
        public string SourceTableType;
        public string TargetTableType;

        [Header("Operations")]
        public List<MigrationOperation> Operations = new();

        [Header("Metadata")]
        public string CreatedAt;
        public string CreatedBy;
        public bool IsApplied;
        public string AppliedAt;
    }
}
