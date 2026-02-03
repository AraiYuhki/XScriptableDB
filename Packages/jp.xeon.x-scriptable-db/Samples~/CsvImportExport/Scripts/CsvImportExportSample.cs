using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Samples.CsvImportExport
{
    /// <summary>
    /// CSV Import/Exportサンプルのロジック部分。
    /// GUIから呼び出されることを想定。
    /// </summary>
    public class CsvImportExportSample : MonoBehaviour
    {
        [SerializeField]
        private CharacterTable characterTable;

        /// <summary>
        /// CSVファイルをインポートする。
        /// </summary>
        /// <param name="filePath">CSVファイルのパス</param>
        /// <param name="encoding">エンコーディング（nullの場合は自動検出）</param>
        /// <param name="delimiter">区切り文字（デフォルト: カンマ）</param>
        /// <returns>インポートしたレコード数</returns>
        public int ImportCsv(string filePath, Encoding encoding = null, char delimiter = ',')
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"ファイルが見つかりません: {filePath}");
                return 0;
            }

            // エンコーディングが指定されていない場合は自動検出
            encoding ??= DetectEncoding(filePath);

            var csvText = File.ReadAllText(filePath, encoding);
            var records = CsvParser.Parse<CharacterRecord>(csvText, delimiter);

#if UNITY_EDITOR
            characterTable.SetRecords(records);
            UnityEditor.EditorUtility.SetDirty(characterTable);
            Debug.Log($"インポート完了: {records.Count}件のレコード");
#endif

            return records.Count;
        }

        /// <summary>
        /// CSVファイルをプレビューする（実際のインポートは行わない）。
        /// </summary>
        /// <param name="filePath">CSVファイルのパス</param>
        /// <param name="encoding">エンコーディング（nullの場合は自動検出）</param>
        /// <param name="delimiter">区切り文字</param>
        /// <returns>プレビュー結果</returns>
        public ImportPreviewResult PreviewImport(string filePath, Encoding encoding = null, char delimiter = ',')
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"ファイルが見つかりません: {filePath}");
                return null;
            }

            encoding ??= DetectEncoding(filePath);

            var csvText = File.ReadAllText(filePath, encoding);
            var newRecords = CsvParser.Parse<CharacterRecord>(csvText, delimiter);

            var result = new ImportPreviewResult();

            // 既存データとの比較
            var existingRecords = new Dictionary<int, CharacterRecord>();
            foreach (var record in characterTable.All)
                existingRecords[record.Id] = record;

            foreach (var newRecord in newRecords)
            {
                if (existingRecords.TryGetValue(newRecord.Id, out var existing))
                {
                    // 更新チェック
                    var changes = CompareRecords(existing, newRecord);
                    if (changes.Count > 0)
                    {
                        result.UpdatedRecords.Add(new RecordChange
                        {
                            Id = newRecord.Id,
                            Name = newRecord.Name,
                            OldRecord = existing,
                            NewRecord = newRecord,
                            ChangedFields = changes
                        });
                    }
                    existingRecords.Remove(newRecord.Id);
                }
                else
                {
                    // 新規追加
                    result.AddedRecords.Add(newRecord);
                }
            }

            // 削除（CSVに含まれていない既存レコード）
            foreach (var remaining in existingRecords.Values)
                result.DeletedRecords.Add(remaining);

            return result;
        }

        /// <summary>
        /// テーブルをCSVファイルにエクスポートする。
        /// </summary>
        /// <param name="filePath">出力先のパス</param>
        /// <param name="encoding">エンコーディング</param>
        /// <param name="delimiter">区切り文字</param>
        /// <param name="sortByPrimaryKey">主キーでソートするか</param>
        public void ExportCsv(string filePath, Encoding encoding, char delimiter = ',', bool sortByPrimaryKey = true)
        {
            var records = new List<CharacterRecord>(characterTable.All);

            if (sortByPrimaryKey)
                records.Sort((a, b) => a.Id.CompareTo(b.Id));

            var csvText = CsvParser.ToCSV(records, delimiter);
            File.WriteAllText(filePath, csvText, encoding);

            Debug.Log($"エクスポート完了: {filePath} ({records.Count}件)");
        }

        /// <summary>
        /// 現在のテーブルのレコード数を取得する。
        /// </summary>
        public int RecordCount => characterTable?.RecordCount ?? 0;

        /// <summary>
        /// 現在のテーブルの全レコードを取得する。
        /// </summary>
        public IEnumerable<CharacterRecord> AllRecords => characterTable?.All;

        /// <summary>
        /// ファイルのエンコーディングを自動検出する。
        /// </summary>
        private Encoding DetectEncoding(string filePath)
        {
            var bytes = File.ReadAllBytes(filePath);

            // BOMチェック
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8;

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode;

            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode;

            // BOMがない場合はUTF-8を試行
            try
            {
                var utf8 = new UTF8Encoding(false, true);
                utf8.GetString(bytes);
                return Encoding.UTF8;
            }
            catch
            {
                // UTF-8として不正な場合はShift-JISと仮定
                return Encoding.GetEncoding("Shift_JIS");
            }
        }

        /// <summary>
        /// 2つのレコードを比較し、変更されたフィールドを返す。
        /// </summary>
        private List<FieldChange> CompareRecords(CharacterRecord oldRecord, CharacterRecord newRecord)
        {
            var changes = new List<FieldChange>();

            if (oldRecord.Name != newRecord.Name)
                changes.Add(new FieldChange("名前", oldRecord.Name, newRecord.Name));

            if (oldRecord.Level != newRecord.Level)
                changes.Add(new FieldChange("レベル", oldRecord.Level.ToString(), newRecord.Level.ToString()));

            if (oldRecord.Hp != newRecord.Hp)
                changes.Add(new FieldChange("HP", oldRecord.Hp.ToString(), newRecord.Hp.ToString()));

            if (oldRecord.Attack != newRecord.Attack)
                changes.Add(new FieldChange("攻撃力", oldRecord.Attack.ToString(), newRecord.Attack.ToString()));

            if (oldRecord.Defense != newRecord.Defense)
                changes.Add(new FieldChange("防御力", oldRecord.Defense.ToString(), newRecord.Defense.ToString()));

            if (oldRecord.CharacterClass != newRecord.CharacterClass)
                changes.Add(new FieldChange("職業", oldRecord.CharacterClass, newRecord.CharacterClass));

            if (oldRecord.IsPlayable != newRecord.IsPlayable)
                changes.Add(new FieldChange("プレイアブル", oldRecord.IsPlayable.ToString(), newRecord.IsPlayable.ToString()));

            return changes;
        }
    }

    /// <summary>
    /// インポートプレビューの結果。
    /// </summary>
    public class ImportPreviewResult
    {
        public List<CharacterRecord> AddedRecords { get; } = new();
        public List<RecordChange> UpdatedRecords { get; } = new();
        public List<CharacterRecord> DeletedRecords { get; } = new();

        public int TotalChanges => AddedRecords.Count + UpdatedRecords.Count + DeletedRecords.Count;
    }

    /// <summary>
    /// レコードの変更情報。
    /// </summary>
    public class RecordChange
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public CharacterRecord OldRecord { get; set; }
        public CharacterRecord NewRecord { get; set; }
        public List<FieldChange> ChangedFields { get; set; }
    }

    /// <summary>
    /// フィールドの変更情報。
    /// </summary>
    public class FieldChange
    {
        public string FieldName { get; }
        public string OldValue { get; }
        public string NewValue { get; }

        public FieldChange(string fieldName, string oldValue, string newValue)
        {
            FieldName = fieldName;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public override string ToString() => $"{FieldName}: {OldValue} → {NewValue}";
    }
}
