using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// 外部キーバリデーター。
    /// </summary>
    public static class ForeignKeyValidator
    {
        private const BindingFlags MemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// 外部キー制約を検証する。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <param name="tableAsset">検証するテーブル</param>
        /// <param name="context">検証コンテキスト</param>
        /// <param name="keySelector">キーセレクター</param>
        /// <returns>検証結果</returns>
        public static TableValidationResult ValidateForeignKeys<T>(ITableAsset tableAsset,
            ForeignKeyValidationContext context,
            Func<T, object> keySelector = null) where T : class
        {
            var result = new TableValidationResult
            {
                TableName = tableAsset?.GetType().Name ?? "Unknown"
            };

            if (tableAsset == null || context == null)
                return result;

            var foreignKeyFields = GetForeignKeyFields(typeof(T));
            if (foreignKeyFields.Count == 0)
                return result;

            var index = 0;
            foreach (var record in tableAsset.Records)
            {
                if (record is T typedRecord)
                    result.RecordResults.Add(ValidateRecord(typedRecord, index, foreignKeyFields, context, keySelector));

                index++;
            }

            return result;
        }

        /// <summary>
        /// 単一レコードの外部キーを検証する。
        /// </summary>
        private static RecordValidationResult ValidateRecord<T>(
            T record,
            int index,
            List<(FieldInfo field, ForeignKeyAttribute attr)> foreignKeyFields,
            ForeignKeyValidationContext context,
            Func<T, object> keySelector) where T : class
        {
            var recordResult = new RecordValidationResult
            {
                RecordIndex = index,
                RecordKey = keySelector?.Invoke(record)
            };

            foreach (var (field, attr) in foreignKeyFields)
                ValidateFieldForeignKey(record, field, attr, context, recordResult);

            return recordResult;
        }

        /// <summary>
        /// 単一フィールドの外部キーを検証する。
        /// </summary>
        private static void ValidateFieldForeignKey<T>(
            T record,
            FieldInfo field,
            ForeignKeyAttribute attr,
            ForeignKeyValidationContext context,
            RecordValidationResult recordResult) where T : class
        {
            var value = field.GetValue(record);
            if (value == null)
                return;

            var keySet = context.GetKeySet(attr.ReferenceTableType);
            if (keySet == null)
            {
                recordResult.AddError(
                    field.Name,
                    $"The reference table {attr.ReferenceTableType.Name} is not registered.",
                    ValidationErrorType.ForeignKey);
                return;
            }

            if (!keySet.Contains(value))
            {
                recordResult.AddError(
                    field.Name,
                    attr.ErrorMessage ?? $"The value '{value}' of {field.Name} does not exist in {attr.ReferenceTableType.Name}.",
                    ValidationErrorType.ForeignKey);
            }
        }

        /// <summary>
        /// 外部キーフィールドを取得する。
        /// </summary>
        private static List<(FieldInfo field, ForeignKeyAttribute attr)> GetForeignKeyFields(Type type)
        {
            var result = new List<(FieldInfo, ForeignKeyAttribute)>();

            foreach (var field in type.GetFields(MemberFlags))
            {
                var attr = field.GetCustomAttribute<ForeignKeyAttribute>();
                if (attr == null)
                    continue;

                result.Add((field, attr));
            }

            return result;
        }

        /// <summary>
        /// 外部キー参照の整合性レポートを生成する。
        /// </summary>
        public static ForeignKeyReport GenerateReport(
            ITableAsset sourceTable,
            ITableAsset targetTable,
            string foreignKeyField)
        {
            var report = new ForeignKeyReport
            {
                SourceTableName = sourceTable?.GetType().Name,
                TargetTableName = targetTable?.GetType().Name,
                ForeignKeyField = foreignKeyField
            };

            if (sourceTable == null || targetTable == null)
                return report;

            var targetKeys = CollectTargetKeys(targetTable);
            var fkField = sourceTable.RecordType.GetField(foreignKeyField, MemberFlags);

            if (fkField == null)
            {
                report.Errors.Add($"Field '{foreignKeyField}' was not found.");
                return report;
            }

            ValidateForeignKeyReferences(sourceTable, fkField, targetKeys, report);
            return report;
        }

        /// <summary>
        /// ターゲットテーブルのキーを収集する。
        /// </summary>
        private static HashSet<object> CollectTargetKeys(ITableAsset targetTable)
        {
            var targetKeys = new HashSet<object>();
            var targetKeyField = GetPrimaryKeyField(targetTable.RecordType);

            if (targetKeyField == null)
                return targetKeys;

            foreach (var record in targetTable.Records)
            {
                if (record == null)
                    continue;

                var key = targetKeyField.GetValue(record);
                if (key != null)
                    targetKeys.Add(key);
            }

            return targetKeys;
        }

        /// <summary>
        /// 外部キー参照を検証してレポートに追加する。
        /// </summary>
        private static void ValidateForeignKeyReferences(
            ITableAsset sourceTable,
            FieldInfo fkField,
            HashSet<object> targetKeys,
            ForeignKeyReport report)
        {
            foreach (var record in sourceTable.Records)
            {
                if (record == null)
                    continue;

                var fkValue = fkField.GetValue(record);
                if (fkValue == null)
                    continue;

                report.TotalReferences++;
                if (!targetKeys.Contains(fkValue))
                    report.InvalidReferences.Add(fkValue);
            }
        }

        /// <summary>
        /// PrimaryKeyフィールドを取得する。
        /// </summary>
        private static FieldInfo GetPrimaryKeyField(Type recordType)
        {
            return recordType.GetFields(MemberFlags)
                .FirstOrDefault(f => f.GetCustomAttribute<PrimaryKeyAttribute>() != null);
        }
    }
}
