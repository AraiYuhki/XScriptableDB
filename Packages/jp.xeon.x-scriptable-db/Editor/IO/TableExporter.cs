using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// エクスポート設定。
    /// </summary>
    public class ExportSettings
    {
        /// <summary>出力先ファイルパス</summary>
        public string FilePath { get; set; }

        /// <summary>エンコーディング</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>区切り文字（デフォルトはカンマ）</summary>
        public char Delimiter { get; set; } = ',';

        /// <summary>PrimaryKeyでソートするかどうか</summary>
        public bool SortByPrimaryKey { get; set; } = true;

        /// <summary>エクスポートするカラムの順序（nullの場合は定義順）</summary>
        public string[] ColumnOrder { get; set; }

        /// <summary>除外するカラム</summary>
        public string[] ExcludeColumns { get; set; }

        /// <summary>BOMを出力するかどうか（UTF-8の場合）</summary>
        public bool WriteBom { get; set; } = false;
    }

    /// <summary>
    /// テーブルをCSV/TSVにエクスポートするクラス。
    /// </summary>
    public static class TableExporter
    {
        private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// テーブルをファイルにエクスポートする。
        /// </summary>
        /// <param name="table">エクスポートするテーブル</param>
        /// <param name="settings">エクスポート設定</param>
        public static void Export(ScriptableObject table, ExportSettings settings)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            if (string.IsNullOrEmpty(settings?.FilePath))
                throw new ArgumentException("File path is required", nameof(settings));

            var tableAsset = table as ITableAsset;
            if (tableAsset == null)
            {
                // IExportableを実装している場合
                if (table is IExportable exportable)
                {
                    exportable.Export(settings.FilePath, settings.Encoding);
                    return;
                }

                throw new InvalidOperationException("Table must implement ITableAsset or IExportable");
            }

            var content = GenerateCsvContent(tableAsset, settings);
            WriteToFile(settings.FilePath, content, settings.Encoding, settings.WriteBom);
        }

        /// <summary>
        /// テーブルからCSV文字列を生成する。
        /// </summary>
        /// <param name="tableAsset">テーブルアセット</param>
        /// <param name="settings">エクスポート設定</param>
        /// <returns>CSV文字列</returns>
        public static string GenerateCsvContent(ITableAsset tableAsset, ExportSettings settings)
        {
            var recordType = tableAsset.RecordType;
            var columns = GetColumns(recordType, settings);
            var delimiter = settings?.Delimiter ?? ',';

            var sb = new StringBuilder();

            // ヘッダー行
            var headerLine = string.Join(delimiter.ToString(), columns.Select(c => c.columnName));
            sb.AppendLine(headerLine);

            // ソートされたレコードを取得
            var records = GetSortedRecords(tableAsset, settings?.SortByPrimaryKey ?? true);

            // データ行
            foreach (var record in records)
            {
                var values = new List<string>();
                foreach (var column in columns)
                {
                    var value = column.getValue(record);
                    var formatted = FormatValue(value, delimiter);
                    values.Add(formatted);
                }

                var dataLine = string.Join(delimiter.ToString(), values);
                sb.AppendLine(dataLine);
            }

            return sb.ToString();
        }

        /// <summary>
        /// カラム情報を取得する。
        /// </summary>
        private static List<(string columnName, Func<object, object> getValue)> GetColumns(
            Type recordType,
            ExportSettings settings)
        {
            var columns = new List<(string columnName, Func<object, object> getValue)>();
            var excludeSet = settings?.ExcludeColumns != null
                ? new HashSet<string>(settings.ExcludeColumns, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>();

            // CsvColumnを持つフィールドを探す
            var fieldsWithAttribute = new List<(FieldInfo field, CsvColumn attr)>();
            foreach (var field in recordType.GetFields(MemberFlags))
            {
                var attr = field.GetCustomAttribute<CsvColumn>();
                if (attr != null)
                    fieldsWithAttribute.Add((field, attr));
            }

            if (fieldsWithAttribute.Count > 0)
            {
                // CsvColumnを使用
                foreach (var (field, attr) in fieldsWithAttribute)
                {
                    var columnName = attr.Name ?? field.Name;
                    if (excludeSet.Contains(columnName) || excludeSet.Contains(field.Name))
                        continue;

                    columns.Add((columnName, record => field.GetValue(record)));
                }
            }
            else
            {
                // 全てのシリアライズ可能なフィールドを使用
                foreach (var field in ReflectionUtility.GetSerializableFields(recordType))
                {
                    if (excludeSet.Contains(field.Name))
                        continue;

                    columns.Add((field.Name, record => field.GetValue(record)));
                }
            }

            // カラム順序を適用
            if (settings?.ColumnOrder != null && settings.ColumnOrder.Length > 0)
            {
                var orderDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < settings.ColumnOrder.Length; i++)
                    orderDict[settings.ColumnOrder[i]] = i;

                columns.Sort((a, b) =>
                {
                    var orderA = orderDict.TryGetValue(a.columnName, out var oA) ? oA : int.MaxValue;
                    var orderB = orderDict.TryGetValue(b.columnName, out var oB) ? oB : int.MaxValue;
                    return orderA.CompareTo(orderB);
                });
            }

            return columns;
        }

        /// <summary>
        /// ソートされたレコードを取得する。
        /// </summary>
        private static IEnumerable<object> GetSortedRecords(ITableAsset tableAsset, bool sortByPrimaryKey)
        {
            var records = new List<object>();
            foreach (var record in tableAsset.Records)
            {
                if (record != null)
                    records.Add(record);
            }

            if (!sortByPrimaryKey)
                return records;

            // PrimaryKeyAccessorを使用してソート
            var keyAccessor = CreateKeyAccessor(tableAsset.RecordType);
            if (keyAccessor == null)
                return records;

            records.Sort((a, b) =>
            {
                var keyA = keyAccessor(a);
                var keyB = keyAccessor(b);

                if (keyA is IComparable ca && keyB is IComparable cb)
                    return ca.CompareTo(cb);

                return string.Compare(keyA?.ToString(), keyB?.ToString(), StringComparison.Ordinal);
            });

            return records;
        }

        /// <summary>
        /// PrimaryKeyアクセサを作成する。
        /// </summary>
        private static Func<object, object> CreateKeyAccessor(Type recordType)
        {
            foreach (var field in recordType.GetFields(MemberFlags))
            {
                if (field.GetCustomAttribute<PrimaryKeyAttribute>() != null)
                    return record => field.GetValue(record);
            }

            foreach (var property in recordType.GetProperties(MemberFlags))
            {
                if (property.GetCustomAttribute<PrimaryKeyAttribute>() != null && property.CanRead)
                    return record => property.GetValue(record);
            }

            return null;
        }

        /// <summary>
        /// 値をCSV形式にフォーマットする。
        /// </summary>
        private static string FormatValue(object value, char delimiter)
        {
            if (value == null)
                return "";

            var str = value.ToString();

            // エスケープが必要かチェック
            var needsQuote = str.Contains(delimiter) ||
                             str.Contains('"') ||
                             str.Contains('\n') ||
                             str.Contains('\r');

            if (!needsQuote)
                return str;

            // ダブルクォートをエスケープ
            str = str.Replace("\"", "\"\"");
            return $"\"{str}\"";
        }

        /// <summary>
        /// ファイルに書き込む。
        /// </summary>
        private static void WriteToFile(string filePath, string content, Encoding encoding, bool writeBom)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (writeBom && encoding == Encoding.UTF8)
                encoding = new UTF8Encoding(true);
            else if (!writeBom && encoding == Encoding.UTF8)
                encoding = new UTF8Encoding(false);

            File.WriteAllText(filePath, content, encoding);
        }

        /// <summary>
        /// テーブルのカラム一覧を取得する。
        /// </summary>
        /// <param name="recordType">レコードの型</param>
        /// <returns>カラム名のリスト</returns>
        public static List<string> GetColumnNames(Type recordType)
        {
            var columns = new List<string>();

            foreach (var field in recordType.GetFields(MemberFlags))
            {
                var attr = field.GetCustomAttribute<CsvColumn>();
                if (attr != null)
                {
                    columns.Add(attr.Name ?? field.Name);
                }
            }

            if (columns.Count == 0)
            {
                foreach (var field in ReflectionUtility.GetSerializableFields(recordType))
                {
                    columns.Add(field.Name);
                }
            }

            return columns;
        }
    }
}
