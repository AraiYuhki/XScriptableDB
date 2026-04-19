using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SQL実行結果のフォーマットと値の取得のためのユーティリティクラス。
    /// </summary>
    public static class SqlResultFormatter
    {
        private const BindingFlags FieldBindingFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// レコードから列名のリストを取得します。
        /// </summary>
        /// <param name="record">レコード</param>
        /// <returns>列名のリスト</returns>
        public static List<string> GetColumnNamesFromRecord(object record)
        {
            if (record == null)
                return new List<string>();

            // ResultRowの場合は、Valuesディクショナリのキーを使用します
            if (record is ResultRow resultRow)
                return resultRow.Values.Keys.ToList();

            // JoinedRecordの場合は、すべてのテーブルからフィールドを取得します
            if (record is JoinedRecord joinedRecord)
            {
                var names = new List<string>();
                foreach (var kvp in joinedRecord.TableRecords)
                {
                    var tableAlias = kvp.Key;
                    var tableRecord = kvp.Value;
                    if (tableRecord == null)
                        continue;

                    var recordType = joinedRecord.GetRecordType(tableAlias);
                    if (recordType == null)
                        continue;

                    foreach (var field in recordType.GetFields(FieldBindingFlags))
                    {
                        names.Add($"{tableAlias}.{field.Name}");
                    }
                }
                return names;
            }

            // 通常のレコードの場合
            var type = record.GetType();
            return type.GetFields(FieldBindingFlags)
                .Select(f => f.Name)
                .ToList();
        }

        /// <summary>
        /// レコードから指定されたフィールドの値を取得します。
        /// </summary>
        /// <param name="record">レコード</param>
        /// <param name="recordType">レコードの型（ResultRowやJoinedRecordではない場合に使用）</param>
        /// <param name="fieldName">フィールド名</param>
        /// <returns>フィールドの値</returns>
        public static object GetFieldValue(object record, Type recordType, string fieldName)
        {
            if (record == null)
                return null;

            // ResultRowの場合は、Valuesディクショナリから取得します
            if (record is ResultRow resultRow)
            {
                if (resultRow.Values.TryGetValue(fieldName, out var value))
                    return value;
                return null;
            }

            // JoinedRecordの場合
            if (record is JoinedRecord joinedRecord)
                return GetJoinedFieldValue(joinedRecord, fieldName);

            if (recordType == null)
                return null;

            return GetFieldValueFromType(record, recordType, fieldName);
        }

        /// <summary>
        /// JoinedRecordからフィールド値を取得します。
        /// </summary>
        private static object GetJoinedFieldValue(JoinedRecord joinedRecord, string fieldName)
        {
            // テーブルエイリアスが含まれている場合（例: "t.Id"）
            if (fieldName.Contains('.'))
            {
                var parts = fieldName.Split('.');
                var tableAlias = parts[0];
                var column = parts[1];

                var tableRecord = joinedRecord.GetRecord(tableAlias);
                var tableType = joinedRecord.GetRecordType(tableAlias);
                if (tableRecord != null && tableType != null)
                    return GetFieldValueFromType(tableRecord, tableType, column);
                return null;
            }

            // テーブルエイリアスが存在しない場合、すべてのテーブルを検索します
            foreach (var kvp in joinedRecord.TableRecords)
            {
                var tableRecord = kvp.Value;
                if (tableRecord == null)
                    continue;

                var tableType = joinedRecord.GetRecordType(kvp.Key);
                if (HasField(tableType, fieldName))
                    return GetFieldValueFromType(tableRecord, tableType, fieldName);
            }
            return null;
        }

        /// <summary>
        /// 型情報を使用してフィールド値を取得します。
        /// </summary>
        private static object GetFieldValueFromType(object record, Type recordType, string fieldName)
        {
            if (record == null || recordType == null)
                return null;

            var field = recordType.GetField(fieldName, FieldBindingFlags);
            if (field != null)
                return field.GetValue(record);

            // 大文字と小文字を区別せずに検索します
            field = recordType.GetFields(FieldBindingFlags)
                .FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            if (field != null)
                return field.GetValue(record);

            var property = recordType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (property?.CanRead == true)
                return property.GetValue(record);

            return null;
        }

        /// <summary>
        /// 指定された型にフィールドが存在するかどうかを確認します。
        /// </summary>
        private static bool HasField(Type recordType, string fieldName)
        {
            if (recordType == null)
                return false;

            var field = recordType.GetField(fieldName, FieldBindingFlags);
            if (field != null)
                return true;

            field = recordType.GetFields(FieldBindingFlags)
                .FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            if (field != null)
                return true;

            var property = recordType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (property?.CanRead == true)
                return true;

            return false;
        }

        /// <summary>
        /// 値を表示文字列にフォーマットします。
        /// </summary>
        /// <param name="value">値</param>
        /// <returns>表示文字列</returns>
        public static string FormatValue(object value)
        {
            if (value == null)
                return "(null)";

            return value switch
            {
                string s => s,
                bool b => b ? "true" : "false",
                float f => f.ToString("F2"),
                double d => d.ToString("F2"),
                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
                _ => value.ToString()
            };
        }
    }
}
