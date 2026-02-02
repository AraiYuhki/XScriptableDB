using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// マイグレーション実行結果。
    /// </summary>
    public class MigrationResult
    {
        public bool IsSuccess { get; set; }
        public int ProcessedCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public TimeSpan ExecutionTime { get; set; }
    }

    /// <summary>
    /// マイグレーション実行エンジン。
    /// </summary>
    public static class MigrationExecutor
    {
        /// <summary>
        /// スキーマ差分から自動的にマイグレーション定義を生成する。
        /// </summary>
        public static MigrationDefinition GenerateMigration(SchemaComparisonResult comparison, string migrationName = null)
        {
            var migration = ScriptableObject.CreateInstance<MigrationDefinition>();

            migration.MigrationName = migrationName ?? $"Migration_{DateTime.Now:yyyyMMdd_HHmmss}";
            migration.Description = $"Auto-generated migration from {comparison.SourceType?.Name} to {comparison.TargetType?.Name}";
            migration.Version = 1;
            migration.SourceTableType = comparison.SourceType?.FullName;
            migration.TargetTableType = comparison.TargetType?.FullName;
            migration.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            migration.Operations = new List<MigrationOperation>();

            foreach (var diff in comparison.Differences)
            {
                var operation = CreateOperationFromDifference(diff);
                if (operation != null)
                    migration.Operations.Add(operation);
            }

            return migration;
        }

        private static MigrationOperation CreateOperationFromDifference(SchemaDifference diff)
        {
            return diff.Type switch
            {
                SchemaDifferenceType.FieldAdded => new MigrationOperation
                {
                    OperationType = MigrationOperationType.AddField,
                    FieldName = diff.FieldName,
                    NewTypeName = diff.NewValue,
                    DefaultValue = GetDefaultValueForType(diff.NewValue)
                },
                SchemaDifferenceType.FieldRemoved => new MigrationOperation
                {
                    OperationType = MigrationOperationType.RemoveField,
                    FieldName = diff.FieldName
                },
                SchemaDifferenceType.FieldTypeChanged => new MigrationOperation
                {
                    OperationType = MigrationOperationType.ChangeFieldType,
                    FieldName = diff.FieldName,
                    NewTypeName = diff.NewValue
                },
                _ => null
            };
        }

        private static string GetDefaultValueForType(string typeName)
        {
            return typeName switch
            {
                "Int32" or "int" => "0",
                "Single" or "float" => "0.0",
                "Double" or "double" => "0.0",
                "Boolean" or "bool" => "false",
                "String" or "string" => "",
                _ => "null"
            };
        }

        /// <summary>
        /// マイグレーションを実行する。
        /// </summary>
        public static MigrationResult Execute(MigrationDefinition migration, ITableAsset sourceTable)
        {
            var result = new MigrationResult();
            var startTime = DateTime.Now;

            try
            {
                var records = sourceTable.Records.Cast<object>().ToList();
                var sourceType = sourceTable.RecordType;

                foreach (var record in records)
                {
                    try
                    {
                        ApplyMigration(record, sourceType, migration.Operations);
                        result.ProcessedCount++;
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Record migration failed: {ex.Message}");
                    }
                }

                migration.IsApplied = true;
                migration.AppliedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                EditorUtility.SetDirty(migration);

                result.IsSuccess = result.FailedCount == 0;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Errors.Add($"Migration failed: {ex.Message}");
            }

            result.ExecutionTime = DateTime.Now - startTime;
            return result;
        }

        private static void ApplyMigration(object record, Type recordType, List<MigrationOperation> operations)
        {
            foreach (var operation in operations)
            {
                ApplyOperation(record, recordType, operation);
            }
        }

        private static void ApplyOperation(object record, Type recordType, MigrationOperation operation)
        {
            var field = ReflectionUtility.GetSerializableField(recordType, operation.FieldName);

            switch (operation.OperationType)
            {
                case MigrationOperationType.SetDefaultValue:
                    if (field != null)
                    {
                        var value = ConvertValue(operation.DefaultValue, field.FieldType);
                        field.SetValue(record, value);
                    }
                    break;

                case MigrationOperationType.RenameField:
                    // フィールドのリネームはランタイムでは不可能
                    // コード生成が必要
                    break;

                case MigrationOperationType.CopyField:
                    if (field != null)
                    {
                        var targetField = ReflectionUtility.GetSerializableField(recordType, operation.NewFieldName);
                        if (targetField != null)
                        {
                            var value = field.GetValue(record);
                            var convertedValue = ConvertValue(value, targetField.FieldType);
                            targetField.SetValue(record, convertedValue);
                        }
                    }
                    break;

                case MigrationOperationType.TransformValue:
                    if (field != null)
                    {
                        var currentValue = field.GetValue(record);
                        var transformedValue = ApplyTransform(currentValue, operation.TransformExpression);
                        field.SetValue(record, transformedValue);
                    }
                    break;
            }
        }

        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return GetDefaultValue(targetType);

            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            try
            {
                if (targetType == typeof(string))
                    return value.ToString();

                if (targetType == typeof(int))
                    return Convert.ToInt32(value);

                if (targetType == typeof(float))
                    return Convert.ToSingle(value);

                if (targetType == typeof(double))
                    return Convert.ToDouble(value);

                if (targetType == typeof(bool))
                    return Convert.ToBoolean(value);

                if (targetType == typeof(long))
                    return Convert.ToInt64(value);

                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return GetDefaultValue(targetType);
            }
        }

        private static object ConvertValue(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
                return GetDefaultValue(targetType);

            try
            {
                if (targetType == typeof(string))
                    return value;

                if (targetType == typeof(int))
                    return int.Parse(value);

                if (targetType == typeof(float))
                    return float.Parse(value);

                if (targetType == typeof(double))
                    return double.Parse(value);

                if (targetType == typeof(bool))
                    return bool.Parse(value);

                if (targetType == typeof(long))
                    return long.Parse(value);

                return null;
            }
            catch
            {
                return GetDefaultValue(targetType);
            }
        }

        private static object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }

        private static object ApplyTransform(object value, string expression)
        {
            // シンプルな変換式をサポート
            if (string.IsNullOrEmpty(expression))
                return value;

            // 例: "UPPER" -> 大文字変換
            if (expression.Equals("UPPER", StringComparison.OrdinalIgnoreCase))
                return value?.ToString()?.ToUpper();

            // 例: "LOWER" -> 小文字変換
            if (expression.Equals("LOWER", StringComparison.OrdinalIgnoreCase))
                return value?.ToString()?.ToLower();

            // 例: "TRIM" -> 前後の空白除去
            if (expression.Equals("TRIM", StringComparison.OrdinalIgnoreCase))
                return value?.ToString()?.Trim();

            // 例: "*2" -> 2倍
            if (expression.StartsWith("*") && double.TryParse(expression[1..], out var multiplier))
            {
                if (value is int intVal)
                    return (int)(intVal * multiplier);
                if (value is float floatVal)
                    return floatVal * (float)multiplier;
                if (value is double doubleVal)
                    return doubleVal * multiplier;
            }

            // 例: "+10" -> 10を加算
            if (expression.StartsWith("+") && double.TryParse(expression[1..], out var addend))
            {
                if (value is int intVal)
                    return intVal + (int)addend;
                if (value is float floatVal)
                    return floatVal + (float)addend;
                if (value is double doubleVal)
                    return doubleVal + addend;
            }

            return value;
        }

        /// <summary>
        /// マイグレーションのドライラン（実際には適用しない）。
        /// </summary>
        public static MigrationResult DryRun(MigrationDefinition migration, ITableAsset sourceTable)
        {
            var result = new MigrationResult();
            var startTime = DateTime.Now;

            try
            {
                var records = sourceTable.Records.Cast<object>().ToList();
                result.ProcessedCount = records.Count;

                // 各操作の検証
                foreach (var operation in migration.Operations)
                {
                    ValidateOperation(operation, sourceTable.RecordType, result);
                }

                result.IsSuccess = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Errors.Add($"Dry run failed: {ex.Message}");
            }

            result.ExecutionTime = DateTime.Now - startTime;
            return result;
        }

        private static void ValidateOperation(MigrationOperation operation, Type recordType, MigrationResult result)
        {
            var field = ReflectionUtility.GetSerializableField(recordType, operation.FieldName);

            switch (operation.OperationType)
            {
                case MigrationOperationType.AddField:
                    if (field != null)
                        result.Warnings.Add($"Field '{operation.FieldName}' already exists");
                    break;

                case MigrationOperationType.RemoveField:
                    if (field == null)
                        result.Warnings.Add($"Field '{operation.FieldName}' does not exist");
                    break;

                case MigrationOperationType.RenameField:
                case MigrationOperationType.ChangeFieldType:
                case MigrationOperationType.SetDefaultValue:
                case MigrationOperationType.TransformValue:
                case MigrationOperationType.CopyField:
                    if (field == null)
                        result.Errors.Add($"Field '{operation.FieldName}' not found");
                    break;
            }
        }
    }
}
