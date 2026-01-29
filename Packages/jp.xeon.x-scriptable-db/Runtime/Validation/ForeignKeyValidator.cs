using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// 外部キー検証のコンテキスト。
    /// </summary>
    public class ForeignKeyValidationContext
    {
        private readonly Dictionary<Type, ITableAsset> tables = new();
        private readonly Dictionary<Type, HashSet<object>> keyCache = new();

        /// <summary>
        /// テーブルを登録する。
        /// </summary>
        /// <typeparam name="T">テーブルの型</typeparam>
        /// <param name="table">テーブルアセット</param>
        public void RegisterTable<T>(T table) where T : ITableAsset
        {
            tables[typeof(T)] = table;
        }

        /// <summary>
        /// テーブルを登録する。
        /// </summary>
        /// <param name="tableType">テーブルの型</param>
        /// <param name="table">テーブルアセット</param>
        public void RegisterTable(Type tableType, ITableAsset table)
        {
            tables[tableType] = table;
        }

        /// <summary>
        /// 登録されているテーブルを取得する。
        /// </summary>
        public ITableAsset GetTable(Type tableType)
        {
            return tables.TryGetValue(tableType, out var table) ? table : null;
        }

        /// <summary>
        /// テーブルのキーセットを取得する（キャッシュ付き）。
        /// </summary>
        public HashSet<object> GetKeySet(Type tableType)
        {
            if (keyCache.TryGetValue(tableType, out var cachedKeys))
            {
                return cachedKeys;
            }

            var table = GetTable(tableType);
            if (table == null) return null;

            var keySet = new HashSet<object>();
            var keyField = GetPrimaryKeyField(table.RecordType);

            if (keyField != null)
            {
                foreach (var record in table.Records)
                {
                    if (record != null)
                    {
                        var key = keyField.GetValue(record);
                        if (key != null)
                        {
                            keySet.Add(key);
                        }
                    }
                }
            }

            keyCache[tableType] = keySet;
            return keySet;
        }

        /// <summary>
        /// キーキャッシュをクリアする。
        /// </summary>
        public void ClearCache()
        {
            keyCache.Clear();
        }

        /// <summary>
        /// PrimaryKeyフィールドを取得する。
        /// </summary>
        private FieldInfo GetPrimaryKeyField(Type recordType)
        {
            return recordType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(f => f.GetCustomAttribute<PrimaryKeyAttribute>() != null);
        }
    }

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
        public static TableValidationResult ValidateForeignKeys<T>(
            ITableAsset tableAsset,
            ForeignKeyValidationContext context,
            Func<T, object> keySelector = null) where T : class
        {
            var result = new TableValidationResult
            {
                TableName = tableAsset?.GetType().Name ?? "Unknown"
            };

            if (tableAsset == null || context == null)
            {
                return result;
            }

            var type = typeof(T);
            var foreignKeyFields = GetForeignKeyFields(type);

            if (foreignKeyFields.Count == 0)
            {
                return result;
            }

            var index = 0;
            foreach (var record in tableAsset.Records)
            {
                if (record is T typedRecord)
                {
                    var recordResult = new RecordValidationResult
                    {
                        RecordIndex = index,
                        RecordKey = keySelector?.Invoke(typedRecord)
                    };

                    foreach (var (field, attr) in foreignKeyFields)
                    {
                        var value = field.GetValue(typedRecord);
                        if (value != null)
                        {
                            var keySet = context.GetKeySet(attr.ReferenceTableType);
                            if (keySet == null)
                            {
                                recordResult.AddError(
                                    field.Name,
                                    $"参照先テーブル {attr.ReferenceTableType.Name} が登録されていません。",
                                    ValidationErrorType.ForeignKey);
                            }
                            else if (!keySet.Contains(value))
                            {
                                recordResult.AddError(
                                    field.Name,
                                    attr.ErrorMessage ?? $"{field.Name} の値 '{value}' は {attr.ReferenceTableType.Name} に存在しません。",
                                    ValidationErrorType.ForeignKey);
                            }
                        }
                    }

                    result.RecordResults.Add(recordResult);
                }
                index++;
            }

            return result;
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
                if (attr != null)
                {
                    result.Add((field, attr));
                }
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
            {
                return report;
            }

            // ターゲットテーブルのキーを収集
            var targetKeys = new HashSet<object>();
            var targetKeyField = GetPrimaryKeyField(targetTable.RecordType);

            if (targetKeyField != null)
            {
                foreach (var record in targetTable.Records)
                {
                    if (record != null)
                    {
                        var key = targetKeyField.GetValue(record);
                        if (key != null)
                        {
                            targetKeys.Add(key);
                        }
                    }
                }
            }

            // ソーステーブルの外部キーをチェック
            var sourceType = sourceTable.RecordType;
            var fkField = sourceType.GetField(foreignKeyField, MemberFlags);

            if (fkField == null)
            {
                report.Errors.Add($"フィールド '{foreignKeyField}' が見つかりません。");
                return report;
            }

            foreach (var record in sourceTable.Records)
            {
                if (record == null) continue;

                var fkValue = fkField.GetValue(record);
                if (fkValue != null)
                {
                    report.TotalReferences++;

                    if (!targetKeys.Contains(fkValue))
                    {
                        report.InvalidReferences.Add(fkValue);
                    }
                }
            }

            return report;
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

    /// <summary>
    /// 外部キー参照レポート。
    /// </summary>
    public class ForeignKeyReport
    {
        /// <summary>ソーステーブル名</summary>
        public string SourceTableName { get; set; }

        /// <summary>ターゲットテーブル名</summary>
        public string TargetTableName { get; set; }

        /// <summary>外部キーフィールド名</summary>
        public string ForeignKeyField { get; set; }

        /// <summary>総参照数</summary>
        public int TotalReferences { get; set; }

        /// <summary>無効な参照値リスト</summary>
        public List<object> InvalidReferences { get; } = new();

        /// <summary>エラーメッセージリスト</summary>
        public List<string> Errors { get; } = new();

        /// <summary>有効かどうか</summary>
        public bool IsValid => InvalidReferences.Count == 0 && Errors.Count == 0;

        /// <summary>無効な参照数</summary>
        public int InvalidReferenceCount => InvalidReferences.Count;

        /// <summary>
        /// サマリーを取得する。
        /// </summary>
        public string GetSummary()
        {
            if (IsValid)
            {
                return $"{SourceTableName}.{ForeignKeyField} -> {TargetTableName}: " +
                       $"全 {TotalReferences} 件の参照が有効です。";
            }

            return $"{SourceTableName}.{ForeignKeyField} -> {TargetTableName}: " +
                   $"{InvalidReferenceCount}/{TotalReferences} 件の無効な参照があります。";
        }
    }
}
