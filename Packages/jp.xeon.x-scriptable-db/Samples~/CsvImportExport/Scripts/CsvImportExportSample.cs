using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Samples.CsvImportExport
{
    /// <summary>
    /// Logic for the CSV Import/Export sample.
    /// Intended to be called from a GUI.
    /// </summary>
    public class CsvImportExportSample : MonoBehaviour
    {
        [SerializeField]
        private CharacterTable characterTable;

        /// <summary>
        /// Imports a CSV file.
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <param name="encoding">Encoding (auto-detected if null)</param>
        /// <param name="delimiter">Delimiter character (default: comma)</param>
        /// <returns>Number of imported records</returns>
        public int ImportCsv(string filePath, Encoding encoding = null, string delimiter = ",")
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"File not found: {filePath}");
                return 0;
            }

            // Auto-detect encoding if not specified
            encoding ??= DetectEncoding(filePath);

            var csvText = File.ReadAllText(filePath, encoding);
            var records = CsvParser.Parse<CharacterRecord>(csvText, delimiter);

#if UNITY_EDITOR
            characterTable.SetRecords(records.ToArray());
            UnityEditor.EditorUtility.SetDirty(characterTable);
            Debug.Log($"Import complete: {records.Count} records");
#endif

            return records.Count;
        }

        /// <summary>
        /// Previews a CSV file without performing the actual import.
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <param name="encoding">Encoding (auto-detected if null)</param>
        /// <param name="delimiter">Delimiter character</param>
        /// <returns>Preview result</returns>
        public ImportPreviewResult PreviewImport(string filePath, Encoding encoding = null, string delimiter = ",")
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"File not found: {filePath}");
                return null;
            }

            encoding ??= DetectEncoding(filePath);

            var csvText = File.ReadAllText(filePath, encoding);
            var newRecords = CsvParser.Parse<CharacterRecord>(csvText, delimiter);

            var result = new ImportPreviewResult();

            // Compare with existing data
            var existingRecords = new Dictionary<int, CharacterRecord>();
            foreach (var record in characterTable.All)
                existingRecords[record.Id] = record;

            foreach (var newRecord in newRecords)
            {
                if (existingRecords.TryGetValue(newRecord.Id, out var existing))
                {
                    // Check for updates
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
                    // New addition
                    result.AddedRecords.Add(newRecord);
                }
            }

            // Deletions (existing records not present in CSV)
            foreach (var remaining in existingRecords.Values)
                result.DeletedRecords.Add(remaining);

            return result;
        }

        /// <summary>
        /// Exports the table to a CSV file.
        /// </summary>
        /// <param name="filePath">Output path</param>
        /// <param name="encoding">Encoding</param>
        /// <param name="delimiter">Delimiter character</param>
        /// <param name="sortByPrimaryKey">Whether to sort by primary key</param>
        public void ExportCsv(string filePath, Encoding encoding, string delimiter = ",", bool sortByPrimaryKey = true)
        {
            var records = new List<CharacterRecord>(characterTable.All);

            if (sortByPrimaryKey)
                records.Sort((a, b) => a.Id.CompareTo(b.Id));

            var csvText = CsvParser.ToCSV<CharacterRecord>(records, delimiter);
            File.WriteAllText(filePath, csvText, encoding);

            Debug.Log($"Export complete: {filePath} ({records.Count} records)");
        }

        /// <summary>
        /// Gets the number of records in the current table.
        /// </summary>
        public int RecordCount => characterTable?.Count ?? 0;

        /// <summary>
        /// Gets all records in the current table.
        /// </summary>
        public IEnumerable<CharacterRecord> AllRecords => characterTable?.All;

        /// <summary>
        /// Auto-detects the encoding of a file.
        /// </summary>
        private Encoding DetectEncoding(string filePath)
        {
            var bytes = File.ReadAllBytes(filePath);

            // BOM check
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8;

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode;

            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode;

            // If no BOM, try UTF-8
            try
            {
                var utf8 = new UTF8Encoding(false, true);
                utf8.GetString(bytes);
                return Encoding.UTF8;
            }
            catch
            {
                // If invalid as UTF-8, assume Shift-JIS
                return Encoding.GetEncoding("Shift_JIS");
            }
        }

        /// <summary>
        /// Compares two records and returns the changed fields.
        /// </summary>
        private List<FieldChange> CompareRecords(CharacterRecord oldRecord, CharacterRecord newRecord)
        {
            var changes = new List<FieldChange>();

            if (oldRecord.Name != newRecord.Name)
                changes.Add(new FieldChange("Name", oldRecord.Name, newRecord.Name));

            if (oldRecord.Level != newRecord.Level)
                changes.Add(new FieldChange("Level", oldRecord.Level.ToString(), newRecord.Level.ToString()));

            if (oldRecord.Hp != newRecord.Hp)
                changes.Add(new FieldChange("HP", oldRecord.Hp.ToString(), newRecord.Hp.ToString()));

            if (oldRecord.Attack != newRecord.Attack)
                changes.Add(new FieldChange("Attack", oldRecord.Attack.ToString(), newRecord.Attack.ToString()));

            if (oldRecord.Defense != newRecord.Defense)
                changes.Add(new FieldChange("Defense", oldRecord.Defense.ToString(), newRecord.Defense.ToString()));

            if (oldRecord.CharacterClass != newRecord.CharacterClass)
                changes.Add(new FieldChange("Class", oldRecord.CharacterClass, newRecord.CharacterClass));

            if (oldRecord.IsPlayable != newRecord.IsPlayable)
                changes.Add(new FieldChange("Playable", oldRecord.IsPlayable.ToString(), newRecord.IsPlayable.ToString()));

            return changes;
        }
    }

    /// <summary>
    /// Result of an import preview.
    /// </summary>
    public class ImportPreviewResult
    {
        public List<CharacterRecord> AddedRecords { get; } = new();
        public List<RecordChange> UpdatedRecords { get; } = new();
        public List<CharacterRecord> DeletedRecords { get; } = new();

        public int TotalChanges => AddedRecords.Count + UpdatedRecords.Count + DeletedRecords.Count;
    }

    /// <summary>
    /// Record change information.
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
    /// Field change information.
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
