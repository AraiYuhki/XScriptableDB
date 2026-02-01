using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// スキーマ比較結果。
    /// </summary>
    public class SchemaComparisonResult
    {
        public Type SourceType { get; }
        public Type TargetType { get; }
        public List<SchemaDifference> Differences { get; } = new();
        public bool HasDifferences => Differences.Count > 0;
        public bool IsCompatible { get; set; } = true;
        public string CompatibilityNote { get; set; }

        public int AddedFieldCount => Differences.Count(d => d.Type == SchemaDifferenceType.FieldAdded);
        public int RemovedFieldCount => Differences.Count(d => d.Type == SchemaDifferenceType.FieldRemoved);
        public int ChangedFieldCount => Differences.Count(d => d.Type == SchemaDifferenceType.FieldTypeChanged);

        public GUIContent GetStatusIcon()
        {
            var name = IsCompatible ? "d_greenLight" : "d_redLight";
            return EditorGUIUtility.IconContent(name);
        }

        public string GetStatusLabel()
        {
            return IsCompatible ? "互換性あり" : "互換性なし";
        }

        public SchemaComparisonResult(Type sourceType, Type targetType)
        {
            SourceType = sourceType;
            TargetType = targetType;
        }
    }
}
