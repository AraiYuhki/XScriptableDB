using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Class for calculating diffs between table data.
    /// </summary>
    public static class DiffCalculator
    {
        private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// Calculates the diff between two record arrays.
        /// </summary>
        /// <typeparam name="T">Record type</typeparam>
        /// <typeparam name="TKey">PrimaryKey type</typeparam>
        /// <param name="oldRecords">Record array before changes</param>
        /// <param name="newRecords">Record array after changes</param>
        /// <param name="keySelector">Function to retrieve the PrimaryKey</param>
        /// <param name="tableName">Table name (optional)</param>
        /// <returns>Diff result</returns>
        public static TableDiffResult Calculate<T, TKey>(
            IReadOnlyList<T> oldRecords,
            IReadOnlyList<T> newRecords,
            Func<T, TKey> keySelector,
            string tableName = null)
            where T : class
            where TKey : IEquatable<TKey>
        {
            var result = new TableDiffResult
            {
                TableName = tableName ?? typeof(T).Name,
                RecordTypeName = typeof(T).FullName
            };

            // Create indexes by PrimaryKey
            var oldByKey = new Dictionary<TKey, (T record, int index)>();
            var newByKey = new Dictionary<TKey, (T record, int index)>();

            for (var i = 0; i < oldRecords.Count; i++)
            {
                var record = oldRecords[i];
                if (record == null) continue;
                var key = keySelector(record);
                oldByKey[key] = (record, i);
            }

            for (var i = 0; i < newRecords.Count; i++)
            {
                var record = newRecords[i];
                if (record == null) continue;
                var key = keySelector(record);
                newByKey[key] = (record, i);
            }

            // Detect deletions and modifications
            foreach (var (key, (oldRecord, oldIndex)) in oldByKey)
            {
                if (newByKey.TryGetValue(key, out var newEntry))
                {
                    // Exists in both → check for modification
                    var fieldDiffs = CompareRecords(oldRecord, newEntry.record);
                    var hasChanges = false;
                    foreach (var diff in fieldDiffs)
                    {
                        if (diff.HasChanged)
                        {
                            hasChanges = true;
                            break;
                        }
                    }

                    result.Diffs.Add(new RecordDiff
                    {
                        PrimaryKey = key,
                        DiffType = hasChanges ? DiffType.Modified : DiffType.Unchanged,
                        OldRecord = oldRecord,
                        NewRecord = newEntry.record,
                        OldIndex = oldIndex,
                        NewIndex = newEntry.index,
                        FieldDiffs = fieldDiffs
                    });
                    continue;
                }
                
                // Not in new data → deleted
                result.Diffs.Add(new RecordDiff
                {
                    PrimaryKey = key,
                    DiffType = DiffType.Removed,
                    OldRecord = oldRecord,
                    NewRecord = null,
                    OldIndex = oldIndex,
                    NewIndex = -1,
                    FieldDiffs = CreateFieldDiffsForRemoval(oldRecord)
                });
            }

            // Detect additions
            foreach (var (key, (newRecord, newIndex)) in newByKey)
            {
                if (oldByKey.ContainsKey(key))
                    continue;
                result.Diffs.Add(new RecordDiff
                {
                    PrimaryKey = key,
                    DiffType = DiffType.Added,
                    OldRecord = null,
                    NewRecord = newRecord,
                    OldIndex = -1,
                    NewIndex = newIndex,
                    FieldDiffs = CreateFieldDiffsForAddition(newRecord)
                });
            }

            // Sort by PrimaryKey
            result.Diffs.Sort((a, b) =>
            {
                if (a.PrimaryKey is IComparable ca && b.PrimaryKey is IComparable cb)
                    return ca.CompareTo(cb);
                return string.Compare(a.PrimaryKey?.ToString(), b.PrimaryKey?.ToString(), StringComparison.Ordinal);
            });

            result.RecalculateCounts();
            return result;
        }

        /// <summary>
        /// Calculates the diff between an ITableAsset and CSV data.
        /// </summary>
        /// <param name="tableAsset">Current table asset</param>
        /// <param name="importedRecords">Array of records to import</param>
        /// <returns>Diff result</returns>
        public static TableDiffResult Calculate(ITableAsset tableAsset, IList importedRecords)
        {
            if (tableAsset == null)
                throw new ArgumentNullException(nameof(tableAsset));

            var recordType = tableAsset.RecordType;
            var keyType = tableAsset.KeyType;

            // Create PrimaryKey accessor
            var keyAccessor = CreateKeyAccessor(recordType);
            if (keyAccessor == null)
            {
                Debug.LogWarning($"Could not find PrimaryKey accessor for type {recordType.Name}");
                return new TableDiffResult
                {
                    TableName = tableAsset.GetType().Name,
                    RecordTypeName = recordType.FullName
                };
            }

            var result = new TableDiffResult
            {
                TableName = tableAsset.GetType().Name,
                RecordTypeName = recordType.FullName
            };

            // Index current records in a dictionary
            var oldByKey = new Dictionary<object, (object record, int index)>();
            var index = 0;
            foreach (var record in tableAsset.Records)
            {
                if (record == null) continue;
                var key = keyAccessor(record);
                if (key != null)
                    oldByKey[key] = (record, index);
                index++;
            }

            // Index new records in a dictionary
            var newByKey = new Dictionary<object, (object record, int index)>();
            index = 0;
            foreach (var record in importedRecords)
            {
                if (record == null) continue;
                var key = keyAccessor(record);
                if (key != null)
                    newByKey[key] = (record, index);
                index++;
            }

            // Detect deletions and modifications
            foreach (var (key, (oldRecord, oldIndex)) in oldByKey)
            {
                if (newByKey.TryGetValue(key, out var newEntry))
                {
                    var fieldDiffs = CompareRecords(oldRecord, newEntry.record);
                    var hasChanges = false;
                    foreach (var diff in fieldDiffs)
                    {
                        if (diff.HasChanged)
                        {
                            hasChanges = true;
                            break;
                        }
                    }

                    result.Diffs.Add(new RecordDiff
                    {
                        PrimaryKey = key,
                        DiffType = hasChanges ? DiffType.Modified : DiffType.Unchanged,
                        OldRecord = oldRecord,
                        NewRecord = newEntry.record,
                        OldIndex = oldIndex,
                        NewIndex = newEntry.index,
                        FieldDiffs = fieldDiffs
                    });
                    continue;
                }
                result.Diffs.Add(new RecordDiff
                {
                    PrimaryKey = key,
                    DiffType = DiffType.Removed,
                    OldRecord = oldRecord,
                    NewRecord = null,
                    OldIndex = oldIndex,
                    NewIndex = -1,
                    FieldDiffs = CreateFieldDiffsForRemoval(oldRecord)
                });
            }

            // Detect additions
            foreach (var (key, (newRecord, newIndex)) in newByKey)
            {
                if (oldByKey.ContainsKey(key))
                    continue;
                result.Diffs.Add(new RecordDiff
                {
                    PrimaryKey = key,
                    DiffType = DiffType.Added,
                    OldRecord = null,
                    NewRecord = newRecord,
                    OldIndex = -1,
                    NewIndex = newIndex,
                    FieldDiffs = CreateFieldDiffsForAddition(newRecord)
                });
            }

            // Sort
            result.Diffs.Sort((a, b) =>
            {
                if (a.PrimaryKey is IComparable ca && b.PrimaryKey is IComparable cb)
                    return ca.CompareTo(cb);
                return string.Compare(a.PrimaryKey?.ToString(), b.PrimaryKey?.ToString(), StringComparison.Ordinal);
            });

            result.RecalculateCounts();
            return result;
        }

        /// <summary>
        /// Compares the fields of two records.
        /// </summary>
        public static List<FieldDiff> CompareRecords(object oldRecord, object newRecord)
        {
            var result = new List<FieldDiff>();

            if (oldRecord == null || newRecord == null)
                return result;

            var type = oldRecord.GetType();
            if (type != newRecord.GetType())
                return result;

            // Compare fields
            foreach (var field in type.GetFields(MemberFlags))
            {
                if (field.IsStatic || field.IsLiteral)
                    continue;

                var oldValue = field.GetValue(oldRecord);
                var newValue = field.GetValue(newRecord);
                var diffType = CompareValues(oldValue, newValue);

                result.Add(new FieldDiff(field.Name, oldValue, newValue, diffType));
            }

            // Compare properties (excluding backing fields)
            foreach (var property in type.GetProperties(MemberFlags))
            {
                if (!property.CanRead)
                    continue;

                // インデクサーはスキップ
                if (property.GetIndexParameters().Length > 0)
                    continue;

                try
                {
                    var oldValue = property.GetValue(oldRecord);
                    var newValue = property.GetValue(newRecord);
                    var diffType = CompareValues(oldValue, newValue);

                    result.Add(new FieldDiff(property.Name, oldValue, newValue, diffType));
                }
                catch
                {
                    // Skip if property access fails
                }
            }

            return result;
        }

        /// <summary>
        /// Compares two values.
        /// </summary>
        private static DiffType CompareValues(object oldValue, object newValue)
        {
            if (oldValue == null && newValue == null)
                return DiffType.Unchanged;

            if (oldValue == null)
                return DiffType.Added;

            if (newValue == null)
                return DiffType.Removed;

            // Compare using IEquatable
            if (oldValue.Equals(newValue))
                return DiffType.Unchanged;

            // For collections, compare elements
            if (oldValue is ICollection oldCol && newValue is ICollection newCol)
            {
                if (oldCol.Count != newCol.Count)
                    return DiffType.Modified;

                var oldEnumerator = oldCol.GetEnumerator();
                var newEnumerator = newCol.GetEnumerator();

                while (oldEnumerator.MoveNext() && newEnumerator.MoveNext())
                {
                    if (!Equals(oldEnumerator.Current, newEnumerator.Current))
                        return DiffType.Modified;
                }

                return DiffType.Unchanged;
            }

            return DiffType.Modified;
        }

        /// <summary>
        /// Creates field diffs for a removed record.
        /// </summary>
        private static List<FieldDiff> CreateFieldDiffsForRemoval(object record)
        {
            var result = new List<FieldDiff>();
            if (record == null)
                return result;

            var type = record.GetType();

            foreach (var field in type.GetFields(MemberFlags))
            {
                if (field.IsStatic || field.IsLiteral)
                    continue;

                var value = field.GetValue(record);
                result.Add(new FieldDiff(field.Name, value, null, DiffType.Removed));
            }

            return result;
        }

        /// <summary>
        /// Creates field diffs for an added record.
        /// </summary>
        private static List<FieldDiff> CreateFieldDiffsForAddition(object record)
        {
            var result = new List<FieldDiff>();
            if (record == null)
                return result;

            var type = record.GetType();

            foreach (var field in type.GetFields(MemberFlags))
            {
                if (field.IsStatic || field.IsLiteral)
                    continue;

                var value = field.GetValue(record);
                result.Add(new FieldDiff(field.Name, null, value, DiffType.Added));
            }

            return result;
        }

        /// <summary>
        /// Creates a PrimaryKey accessor.
        /// </summary>
        private static Func<object, object> CreateKeyAccessor(Type recordType)
        {
            // Search for fields/properties with PrimaryKeyAttribute
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

            // If the type implements IRecord<TKey>
            var recordInterface = recordType.GetInterface("IRecord`1");
            if (recordInterface != null)
            {
                var keyProperty = recordType.GetProperty("PrimaryKey");
                if (keyProperty != null && keyProperty.CanRead)
                    return record => keyProperty.GetValue(record);
            }

            // Search for fields/properties named "Id", "ID", "Key", or "PrimaryKey"
            var commonNames = new[] { "Id", "ID", "Key", "PrimaryKey", "id", "key" };
            foreach (var name in commonNames)
            {
                var field = recordType.GetField(name, MemberFlags);
                if (field != null)
                    return record => field.GetValue(record);

                var property = recordType.GetProperty(name, MemberFlags);
                if (property != null && property.CanRead)
                    return record => property.GetValue(record);
            }

            return null;
        }
    }
}
