using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// Base class for table assets.
    /// Manages a record array sorted by PrimaryKey and provides fast lookup via binary search.
    /// </summary>
    /// <typeparam name="T">Record type</typeparam>
    /// <typeparam name="TKey">PrimaryKey type</typeparam>
    public abstract class TableAsset<T, TKey> : ScriptableObject, ITable<T>, ITableAsset, IImportable, IExportable
        where T : class, new()
        where TKey : IComparable<TKey>
    {
        [SerializeField]
        protected T[] records = Array.Empty<T>();

        [SerializeField]
        protected IndexContainer secondaryIndices = new();

        private PrimaryKeyAccessor<T> keyAccessor;
        private bool isSorted;

        /// <summary>
        /// Read-only access to all records.
        /// </summary>
        public IReadOnlyList<T> All => records;

        /// <summary>
        /// Direct access to the internal record array (for queries).
        /// </summary>
        internal T[] RecordsInternal => records;

        /// <summary>
        /// Number of records.
        /// </summary>
        public int Count => records.Length;

        /// <summary>
        /// For ITableAsset: returns all records.
        /// </summary>
        IEnumerable ITableAsset.Records => records;

        /// <summary>
        /// For ITableAsset: the record type.
        /// </summary>
        Type ITableAsset.RecordType => typeof(T);

        /// <summary>
        /// For ITableAsset: the PrimaryKey type.
        /// </summary>
        Type ITableAsset.KeyType => typeof(TKey);

        /// <summary>
        /// PrimaryKey accessor.
        /// </summary>
        protected PrimaryKeyAccessor<T> KeyAccessor
        {
            get
            {
                keyAccessor ??= new PrimaryKeyAccessor<T>();
                return keyAccessor;
            }
        }

        /// <summary>
        /// SecondaryKey index container.
        /// </summary>
        public IndexContainer SecondaryIndices => secondaryIndices;

        protected virtual void OnEnable()
        {
            EnsureSorted();
        }

        /// <summary>
        /// Ensures that records are sorted by PrimaryKey.
        /// </summary>
        public void EnsureSorted()
        {
            if (isSorted || records == null || records.Length <= 1)
            {
                isSorted = true;
                return;
            }

            Array.Sort(records, KeyAccessor.CreateComparer<TKey>());
            isSorted = true;
        }

        /// <summary>
        /// Searches for a record by PrimaryKey (binary search).
        /// </summary>
        /// <param name="key">The PrimaryKey to search for</param>
        /// <returns>The matching record, or null if not found</returns>
        public T FindByKey(TKey key)
        {
            EnsureSorted();

            if (records == null || records.Length == 0)
                return null;

            var index = BinarySearch(key);
            if (index < 0)
                return null;

            return records[index];
        }

        /// <summary>
        /// Searches for a record by PrimaryKey (binary search).
        /// </summary>
        /// <param name="key">The PrimaryKey to search for</param>
        /// <param name="record">The found record</param>
        /// <returns>True if found</returns>
        public bool TryFindByKey(TKey key, out T record)
        {
            record = FindByKey(key);
            return record != null;
        }

        /// <summary>
        /// Gets the record at the specified index.
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Record</returns>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= records.Length)
                    throw new IndexOutOfRangeException($"Index {index} is out of range [0, {records.Length})");
                return records[index];
            }
        }

        /// <summary>
        /// Searches for the index of a record using binary search.
        /// </summary>
        /// <param name="key">The PrimaryKey to search for</param>
        /// <returns>The index if found, or a negative value if not found</returns>
        protected int BinarySearch(TKey key)
        {
            if (records == null || records.Length == 0)
                return -1;

            var left = 0;
            var right = records.Length - 1;

            while (left <= right)
            {
                var mid = left + (right - left) / 2;
                var midKey = KeyAccessor.GetKey<TKey>(records[mid]);
                var comparison = midKey.CompareTo(key);

                if (comparison == 0)
                    return mid;
                if (comparison < 0)
                    left = mid + 1;
                else
                    right = mid - 1;
            }

            return -1;
        }

        /// <summary>
        /// Gets records whose keys fall within the specified range.
        /// </summary>
        /// <param name="minKey">Minimum key (inclusive)</param>
        /// <param name="maxKey">Maximum key (inclusive)</param>
        /// <returns>Records within the range</returns>
        public IEnumerable<T> FindInRange(TKey minKey, TKey maxKey)
        {
            EnsureSorted();

            if (records == null || records.Length == 0)
                yield break;

            var startIndex = FindLowerBound(minKey);
            if (startIndex < 0)
                yield break;

            for (var i = startIndex; i < records.Length; i++)
            {
                var recordKey = KeyAccessor.GetKey<TKey>(records[i]);
                if (recordKey.CompareTo(maxKey) > 0)
                    yield break;
                yield return records[i];
            }
        }

        /// <summary>
        /// Gets the first index whose key is greater than or equal to the specified key.
        /// </summary>
        private int FindLowerBound(TKey key)
        {
            if (records == null || records.Length == 0)
                return -1;

            var left = 0;
            var right = records.Length;

            while (left < right)
            {
                var mid = left + (right - left) / 2;
                var midKey = KeyAccessor.GetKey<TKey>(records[mid]);
                if (midKey.CompareTo(key) < 0)
                    left = mid + 1;
                else
                    right = mid;
            }

            return left < records.Length ? left : -1;
        }

        /// <summary>
        /// Searches for a record by SecondaryKey (O(1)).
        /// </summary>
        /// <typeparam name="TSecondaryKey">SecondaryKey type</typeparam>
        /// <param name="indexName">Index name</param>
        /// <param name="key">The key to search for</param>
        /// <returns>The matching record, or null if not found</returns>
        public T FindBySecondaryKey<TSecondaryKey>(string indexName, TSecondaryKey key)
        {
            var index = secondaryIndices.GetIndex(indexName);
            if (index == null)
            {
                Debug.LogWarning($"Index '{indexName}' not found");
                return null;
            }

            var recordIndices = index.FindByKey(key);
            if (recordIndices.Length == 0)
                return null;

            return records[recordIndices[0]];
        }

        /// <summary>
        /// Searches for a record by SecondaryKey (O(1)).
        /// </summary>
        /// <typeparam name="TSecondaryKey">SecondaryKey type</typeparam>
        /// <param name="indexName">Index name</param>
        /// <param name="key">The key to search for</param>
        /// <param name="record">The found record</param>
        /// <returns>True if found</returns>
        public bool TryFindBySecondaryKey<TSecondaryKey>(string indexName, TSecondaryKey key, out T record)
        {
            record = FindBySecondaryKey(indexName, key);
            return record != null;
        }

        /// <summary>
        /// Searches for multiple records by SecondaryKey (O(1)).
        /// </summary>
        /// <typeparam name="TSecondaryKey">SecondaryKey type</typeparam>
        /// <param name="indexName">Index name</param>
        /// <param name="key">The key to search for</param>
        /// <returns>Enumeration of matching records</returns>
        public IEnumerable<T> FindAllBySecondaryKey<TSecondaryKey>(string indexName, TSecondaryKey key)
        {
            var index = secondaryIndices.GetIndex(indexName);
            if (index == null)
            {
                Debug.LogWarning($"Index '{indexName}' not found");
                yield break;
            }

            var recordIndices = index.FindByKey(key);
            foreach (var i in recordIndices)
            {
                if (i >= 0 && i < records.Length)
                    yield return records[i];
            }
        }

        /// <summary>
        /// Searches for multiple records by SecondaryKey and returns them as an array (O(1)).
        /// </summary>
        /// <typeparam name="TSecondaryKey">SecondaryKey type</typeparam>
        /// <param name="indexName">Index name</param>
        /// <param name="key">The key to search for</param>
        /// <returns>Array of matching records</returns>
        public T[] FindAllBySecondaryKeyAsArray<TSecondaryKey>(string indexName, TSecondaryKey key)
        {
            var index = secondaryIndices.GetIndex(indexName);
            if (index == null)
            {
                Debug.LogWarning($"Index '{indexName}' not found");
                return Array.Empty<T>();
            }

            var recordIndices = index.FindByKey(key);
            if (recordIndices.Length == 0)
                return Array.Empty<T>();

            var result = new T[recordIndices.Length];
            for (var i = 0; i < recordIndices.Length; i++)
            {
                var recordIndex = recordIndices[i];
                if (recordIndex >= 0 && recordIndex < records.Length)
                    result[i] = records[recordIndex];
            }
            return result;
        }

        // ========================================
        // Composite SecondaryKey search methods
        // ========================================

        /// <summary>
        /// Searches for a record by composite SecondaryKey (O(1)).
        /// </summary>
        /// <param name="indexName">Index name</param>
        /// <param name="keyParts">Array of key values to search for</param>
        /// <returns>The matching record, or null if not found</returns>
        public T FindBySecondaryKey(string indexName, params object[] keyParts)
        {
            var index = secondaryIndices.GetIndex(indexName);
            if (index == null)
            {
                Debug.LogWarning($"Index '{indexName}' not found");
                return null;
            }

            var compositeString = CompositeKeyHelper.ComputeCompositeString(keyParts);
            var recordIndices = index.FindByString(compositeString);
            if (recordIndices.Length == 0)
                return null;

            return records[recordIndices[0]];
        }

        /// <summary>
        /// Searches for a record by composite SecondaryKey (O(1)).
        /// </summary>
        /// <param name="indexName">Index name</param>
        /// <param name="record">The found record</param>
        /// <param name="keyParts">Array of key values to search for</param>
        /// <returns>True if found</returns>
        public bool TryFindBySecondaryKey(string indexName, out T record, params object[] keyParts)
        {
            record = FindBySecondaryKey(indexName, keyParts);
            return record != null;
        }

        /// <summary>
        /// Searches for multiple records by composite SecondaryKey (O(1)).
        /// </summary>
        /// <param name="indexName">Index name</param>
        /// <param name="keyParts">Array of key values to search for</param>
        /// <returns>Enumeration of matching records</returns>
        public IEnumerable<T> FindAllBySecondaryKey(string indexName, params object[] keyParts)
        {
            var index = secondaryIndices.GetIndex(indexName);
            if (index == null)
            {
                Debug.LogWarning($"Index '{indexName}' not found");
                yield break;
            }

            var compositeString = CompositeKeyHelper.ComputeCompositeString(keyParts);
            var recordIndices = index.FindByString(compositeString);
            foreach (var i in recordIndices)
            {
                if (i >= 0 && i < records.Length)
                    yield return records[i];
            }
        }

        /// <summary>
        /// Searches for multiple records by composite SecondaryKey and returns them as an array (O(1)).
        /// </summary>
        /// <param name="indexName">Index name</param>
        /// <param name="keyParts">Array of key values to search for</param>
        /// <returns>Array of matching records</returns>
        public T[] FindAllBySecondaryKeyAsArray(string indexName, params object[] keyParts)
        {
            var index = secondaryIndices.GetIndex(indexName);
            if (index == null)
            {
                Debug.LogWarning($"Index '{indexName}' not found");
                return Array.Empty<T>();
            }

            var compositeString = CompositeKeyHelper.ComputeCompositeString(keyParts);
            var recordIndices = index.FindByString(compositeString);
            if (recordIndices.Length == 0)
                return Array.Empty<T>();

            var result = new T[recordIndices.Length];
            for (var i = 0; i < recordIndices.Length; i++)
            {
                var recordIndex = recordIndices[i];
                if (recordIndex >= 0 && recordIndex < records.Length)
                    result[i] = records[recordIndex];
            }
            return result;
        }

        /// <summary>
        /// Composite SecondaryKey search using two keys (type-safe overload).
        /// </summary>
        public T FindBySecondaryKey<TKey1, TKey2>(string indexName, TKey1 key1, TKey2 key2)
        {
            return FindBySecondaryKey(indexName, (object)key1, (object)key2);
        }

        /// <summary>
        /// Composite SecondaryKey search using three keys (type-safe overload).
        /// </summary>
        public T FindBySecondaryKey<TKey1, TKey2, TKey3>(string indexName, TKey1 key1, TKey2 key2, TKey3 key3)
        {
            return FindBySecondaryKey(indexName, (object)key1, (object)key2, (object)key3);
        }

        /// <summary>
        /// Composite SecondaryKey multi-record search using two keys (type-safe overload).
        /// </summary>
        public T[] FindAllBySecondaryKeyAsArray<TKey1, TKey2>(string indexName, TKey1 key1, TKey2 key2)
        {
            return FindAllBySecondaryKeyAsArray(indexName, (object)key1, (object)key2);
        }

        /// <summary>
        /// Composite SecondaryKey multi-record search using three keys (type-safe overload).
        /// </summary>
        public T[] FindAllBySecondaryKeyAsArray<TKey1, TKey2, TKey3>(string indexName, TKey1 key1, TKey2 key2, TKey3 key3)
        {
            return FindAllBySecondaryKeyAsArray(indexName, (object)key1, (object)key2, (object)key3);
        }

        /// <summary>
        /// Imports records from a CSV file.
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        public void Import(string filePath)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("Import failed: filePath is null or empty");
                return;
            }

            if (!File.Exists(filePath))
            {
                Debug.LogError($"Import failed: file not found at {filePath}");
                return;
            }

            try
            {
                var importedRecords = CsvParser.ParseRecordFile<T>(filePath);
                SetRecords(importedRecords.ToArray());
                Debug.Log($"Imported {importedRecords.Count} records from {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Import failed: {e.Message}");
                Debug.LogException(e);
            }
#else
            Debug.LogWarning("Import is only available in Unity Editor");
#endif
        }

        /// <summary>
        /// Exports records to a CSV file.
        /// </summary>
        /// <param name="filePath">Output file path</param>
        /// <param name="encoding">Encoding (defaults to UTF-8)</param>
        public void Export(string filePath, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("Export failed: filePath is null or empty");
                return;
            }

            try
            {
                encoding ??= Encoding.UTF8;
                var csv = CsvParser.ToCSV(records.ToList(), typeof(T));
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(filePath, csv, encoding);
            }
            catch (Exception e)
            {
                Debug.LogError($"Export failed: {e.Message}");
                Debug.LogException(e);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Sets records in the Editor.
        /// </summary>
        /// <param name="newRecords">New record array</param>
        public void SetRecords(T[] newRecords)
        {
            records = newRecords ?? Array.Empty<T>();
            isSorted = false;
            EnsureSorted();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Adds a record in the Editor.
        /// </summary>
        /// <param name="record">Record to add</param>
        public void AddRecord(T record)
        {
            if (record == null)
                return;

            var newRecords = new T[records.Length + 1];
            Array.Copy(records, newRecords, records.Length);
            newRecords[records.Length] = record;
            records = newRecords;
            isSorted = false;
            EnsureSorted();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Removes a record at the specified index in the Editor.
        /// </summary>
        /// <param name="index">Index of the record to remove</param>
        public void RemoveRecordAt(int index)
        {
            if (index < 0 || index >= records.Length)
                return;

            var newRecords = new T[records.Length - 1];
            if (index > 0)
                Array.Copy(records, 0, newRecords, 0, index);
            if (index < records.Length - 1)
                Array.Copy(records, index + 1, newRecords, index, records.Length - index - 1);
            records = newRecords;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Checks for duplicate PrimaryKeys.
        /// </summary>
        /// <returns>List of duplicate keys</returns>
        public List<TKey> FindDuplicateKeys()
        {
            var duplicates = new List<TKey>();
            var seen = new HashSet<TKey>();

            foreach (var record in records)
            {
                var key = KeyAccessor.GetKey<TKey>(record);
                if (!seen.Add(key))
                    duplicates.Add(key);
            }

            return duplicates;
        }

        /// <summary>
        /// Gets the internal record array (Editor only).
        /// </summary>
        public T[] GetRecordsForEditor() => records;

        /// <summary>
        /// Rebuilds the SecondaryKey indexes (Editor only).
        /// </summary>
        public void RebuildSecondaryIndices()
        {
            secondaryIndices = IndexBuilder.BuildIndices(records);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// For ITableAsset: creates a new empty record.
        /// </summary>
        object ITableAsset.CreateNewRecord() => new T();

        /// <summary>
        /// For ITableAsset: adds a record.
        /// </summary>
        void ITableAsset.AddRecordObject(object record)
        {
            if (record is T typedRecord)
                AddRecord(typedRecord);
        }

        /// <summary>
        /// For ITableAsset: checks for duplicate PrimaryKeys.
        /// </summary>
        IList ITableAsset.FindDuplicateKeysAsObjects()
        {
            var duplicates = FindDuplicateKeys();
            var result = new ArrayList(duplicates.Count);
            foreach (var key in duplicates)
                result.Add(key);
            return result;
        }
#endif
    }
}
