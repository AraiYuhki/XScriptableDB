using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// スキーマ差分の種類。
    /// </summary>
    public enum SchemaDifferenceType
    {
        FieldAdded,
        FieldRemoved,
        FieldTypeChanged,
        FieldAttributeChanged,
        PrimaryKeyChanged,
        SecondaryKeyAdded,
        SecondaryKeyRemoved
    }

    /// <summary>
    /// スキーマ差分の詳細。
    /// </summary>
    public class SchemaDifference
    {
        public SchemaDifferenceType Type { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string Description { get; set; }

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

    /// <summary>
    /// スキーマ比較結果。
    /// </summary>
    public class SchemaComparisonResult
    {
        public Type SourceType { get; set; }
        public Type TargetType { get; set; }
        public List<SchemaDifference> Differences { get; set; } = new();
        public bool HasDifferences => Differences.Count > 0;
        public bool IsCompatible { get; set; } = true;
        public string CompatibilityNote { get; set; }

        public int AddedFieldCount => Differences.Count(d => d.Type == SchemaDifferenceType.FieldAdded);
        public int RemovedFieldCount => Differences.Count(d => d.Type == SchemaDifferenceType.FieldRemoved);
        public int ChangedFieldCount => Differences.Count(d => d.Type == SchemaDifferenceType.FieldTypeChanged);
    }

    /// <summary>
    /// フィールド情報。
    /// </summary>
    public class FieldSchemaInfo
    {
        public string Name { get; set; }
        public Type FieldType { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsSecondaryKey { get; set; }
        public bool IsReadOnly { get; set; }
        public List<Attribute> Attributes { get; set; } = new();

        public static FieldSchemaInfo FromFieldInfo(FieldInfo field)
        {
            var info = new FieldSchemaInfo
            {
                Name = field.Name,
                FieldType = field.FieldType,
                IsPrimaryKey = field.GetCustomAttribute<PrimaryKeyAttribute>() != null,
                IsSecondaryKey = field.GetCustomAttribute<SecondaryKeyAttribute>() != null,
                IsReadOnly = field.GetCustomAttribute<ReadOnlyAttribute>() != null,
                Attributes = field.GetCustomAttributes().ToList()
            };
            return info;
        }
    }

    /// <summary>
    /// スキーマ比較ユーティリティ。
    /// </summary>
    public static class SchemaComparer
    {
        /// <summary>
        /// 2つの型のスキーマを比較する。
        /// </summary>
        public static SchemaComparisonResult Compare(Type sourceType, Type targetType)
        {
            var result = new SchemaComparisonResult
            {
                SourceType = sourceType,
                TargetType = targetType
            };

            var sourceFields = GetFieldSchemas(sourceType);
            var targetFields = GetFieldSchemas(targetType);

            var sourceFieldNames = new HashSet<string>(sourceFields.Keys);
            var targetFieldNames = new HashSet<string>(targetFields.Keys);

            // 追加されたフィールド
            foreach (var name in targetFieldNames.Except(sourceFieldNames))
            {
                var field = targetFields[name];
                result.Differences.Add(new SchemaDifference
                {
                    Type = SchemaDifferenceType.FieldAdded,
                    FieldName = name,
                    NewValue = field.FieldType.Name
                });
            }

            // 削除されたフィールド
            foreach (var name in sourceFieldNames.Except(targetFieldNames))
            {
                var field = sourceFields[name];
                result.Differences.Add(new SchemaDifference
                {
                    Type = SchemaDifferenceType.FieldRemoved,
                    FieldName = name,
                    OldValue = field.FieldType.Name
                });
                result.IsCompatible = false;
                result.CompatibilityNote = "フィールドが削除されているため、データ損失の可能性があります";
            }

            // 共通フィールドの比較
            foreach (var name in sourceFieldNames.Intersect(targetFieldNames))
            {
                var sourceField = sourceFields[name];
                var targetField = targetFields[name];

                // 型の変更
                if (sourceField.FieldType != targetField.FieldType)
                {
                    result.Differences.Add(new SchemaDifference
                    {
                        Type = SchemaDifferenceType.FieldTypeChanged,
                        FieldName = name,
                        OldValue = sourceField.FieldType.Name,
                        NewValue = targetField.FieldType.Name
                    });

                    if (!IsTypeConvertible(sourceField.FieldType, targetField.FieldType))
                    {
                        result.IsCompatible = false;
                        result.CompatibilityNote = $"フィールド '{name}' の型変換が不可能です";
                    }
                }

                // PrimaryKeyの変更
                if (sourceField.IsPrimaryKey != targetField.IsPrimaryKey)
                {
                    result.Differences.Add(new SchemaDifference
                    {
                        Type = SchemaDifferenceType.PrimaryKeyChanged,
                        FieldName = name,
                        OldValue = sourceField.IsPrimaryKey ? name : "(none)",
                        NewValue = targetField.IsPrimaryKey ? name : "(none)"
                    });
                }

                // SecondaryKeyの変更
                if (sourceField.IsSecondaryKey && !targetField.IsSecondaryKey)
                {
                    result.Differences.Add(new SchemaDifference
                    {
                        Type = SchemaDifferenceType.SecondaryKeyRemoved,
                        FieldName = name
                    });
                }
                else if (!sourceField.IsSecondaryKey && targetField.IsSecondaryKey)
                {
                    result.Differences.Add(new SchemaDifference
                    {
                        Type = SchemaDifferenceType.SecondaryKeyAdded,
                        FieldName = name
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// 型からフィールドスキーマ情報を取得する。
        /// </summary>
        public static Dictionary<string, FieldSchemaInfo> GetFieldSchemas(Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var result = new Dictionary<string, FieldSchemaInfo>();

            foreach (var field in fields)
            {
                result[field.Name] = FieldSchemaInfo.FromFieldInfo(field);
            }

            return result;
        }

        /// <summary>
        /// 型間の変換が可能かどうかを判定する。
        /// </summary>
        private static bool IsTypeConvertible(Type from, Type to)
        {
            // 同じ型
            if (from == to)
                return true;

            // 数値型の変換
            var numericTypes = new HashSet<Type>
            {
                typeof(byte), typeof(sbyte),
                typeof(short), typeof(ushort),
                typeof(int), typeof(uint),
                typeof(long), typeof(ulong),
                typeof(float), typeof(double), typeof(decimal)
            };

            if (numericTypes.Contains(from) && numericTypes.Contains(to))
                return true;

            // 文字列への変換は常に可能
            if (to == typeof(string))
                return true;

            // Nullable型
            var underlyingFrom = Nullable.GetUnderlyingType(from);
            var underlyingTo = Nullable.GetUnderlyingType(to);

            if (underlyingFrom != null && underlyingTo != null)
                return IsTypeConvertible(underlyingFrom, underlyingTo);

            if (underlyingTo != null && from == underlyingTo)
                return true;

            return false;
        }

        /// <summary>
        /// テーブルアセット間のスキーマを比較する。
        /// </summary>
        public static SchemaComparisonResult Compare(ITableAsset source, ITableAsset target)
        {
            return Compare(source.RecordType, target.RecordType);
        }

        /// <summary>
        /// スキーマのサマリーを生成する。
        /// </summary>
        public static string GenerateSchemaSummary(Type type)
        {
            var fields = GetFieldSchemas(type);
            var lines = new List<string>
            {
                $"テーブル: {type.Name}",
                $"フィールド数: {fields.Count}",
                ""
            };

            var primaryKey = fields.Values.FirstOrDefault(f => f.IsPrimaryKey);
            if (primaryKey != null)
                lines.Add($"PrimaryKey: {primaryKey.Name} ({primaryKey.FieldType.Name})");

            var secondaryKeys = fields.Values.Where(f => f.IsSecondaryKey).ToList();
            if (secondaryKeys.Count > 0)
            {
                lines.Add($"SecondaryKeys: {string.Join(", ", secondaryKeys.Select(f => f.Name))}");
            }

            lines.Add("");
            lines.Add("フィールド一覧:");
            foreach (var field in fields.Values.OrderBy(f => f.Name))
            {
                var attrs = new List<string>();
                if (field.IsPrimaryKey)
                    attrs.Add("PK");
                if (field.IsSecondaryKey)
                    attrs.Add("SK");
                if (field.IsReadOnly)
                    attrs.Add("RO");

                var attrStr = attrs.Count > 0 ? $" [{string.Join(", ", attrs)}]" : "";
                lines.Add($"  {field.Name}: {field.FieldType.Name}{attrStr}");
            }

            return string.Join("\n", lines);
        }
    }
}
