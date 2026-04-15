using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Class that performs batch operations on a table.
    /// Supports the generic argument order of TableAsset&lt;TRecord, TKey&gt;.
    /// </summary>
    /// <typeparam name="TRecord">Record type</typeparam>
    /// <typeparam name="TKey">Key type</typeparam>
    public class BatchProcessor<TRecord, TKey>
        where TRecord : class, new()
        where TKey : IComparable<TKey>
    {
        private readonly TableAsset<TRecord, TKey> table;
        private readonly Func<TRecord, TKey> keySelector;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="table">Target table</param>
        /// <param name="keySelector">Key selector function</param>
        public BatchProcessor(TableAsset<TRecord, TKey> table, Func<TRecord, TKey> keySelector)
        {
            this.table = table ?? throw new ArgumentNullException(nameof(table));
            this.keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        }

        /// <summary>
        /// Executes batch operations.
        /// </summary>
        /// <param name="operations">List of operations</param>
        /// <param name="settings">Processing settings</param>
        /// <returns>Processing result</returns>
        public BatchProcessResult Execute(
            IEnumerable<BatchOperationEntry<TRecord>> operations,
            BatchProcessSettings settings = null)
        {
            settings ??= new BatchProcessSettings();
            var result = new BatchProcessResult { Success = true };
            var startTime = DateTime.Now;

            var opList = operations.ToList();
            var current = 0;
            var total = opList.Count;

            // Copy current records into a dictionary
            var recordDict = new Dictionary<TKey, TRecord>();
            foreach (var record in table.All)
            {
                var key = keySelector(record);
                recordDict[key] = record;
            }

            foreach (var op in opList)
            {
                try
                {
                    ProcessOperation(op, recordDict, settings, result);
                }
                catch (Exception e)
                {
                    op.Result = BatchOperationResult.Failed;
                    op.ErrorMessage = e.Message;
                    result.FailedCount++;
                    result.Errors.Add($"{op.Type}: {e.Message}");

                    if (!settings.ContinueOnError)
                    {
                        result.Success = false;
                        break;
                    }
                }

                current++;
                settings.OnProgress?.Invoke(current, total);
            }

            // Apply results
            var newRecords = recordDict.Values.ToArray();
            table.SetRecords(newRecords);

            if (settings.AutoSort)
                table.EnsureSorted();

            result.ElapsedTime = DateTime.Now - startTime;
            return result;
        }

        private void ProcessOperation(
            BatchOperationEntry<TRecord> op,
            Dictionary<TKey, TRecord> recordDict,
            BatchProcessSettings settings,
            BatchProcessResult result)
        {
            var key = keySelector(op.Record);

            switch (op.Type)
            {
                case BatchOperationType.Add:
                    ProcessAdd(op, key, recordDict, settings, result);
                    break;

                case BatchOperationType.Update:
                    ProcessUpdate(op, key, recordDict, result);
                    break;

                case BatchOperationType.Delete:
                    ProcessDelete(op, key, recordDict, result);
                    break;

                case BatchOperationType.Upsert:
                    ProcessUpsert(op, key, recordDict, result);
                    break;
            }
        }

        private void ProcessAdd(
            BatchOperationEntry<TRecord> op,
            TKey key,
            Dictionary<TKey, TRecord> recordDict,
            BatchProcessSettings settings,
            BatchProcessResult result)
        {
            if (recordDict.ContainsKey(key))
            {
                if (!settings.AllowDuplicateKeys)
                {
                    op.Result = BatchOperationResult.Failed;
                    op.ErrorMessage = $"Key already exists: {key}";
                    result.FailedCount++;
                    result.Errors.Add(op.ErrorMessage);
                    return;
                }
            }

            recordDict[key] = op.Record;
            op.Result = BatchOperationResult.Success;
            result.AddedCount++;
        }

        private void ProcessUpdate(
            BatchOperationEntry<TRecord> op,
            TKey key,
            Dictionary<TKey, TRecord> recordDict,
            BatchProcessResult result)
        {
            if (!recordDict.ContainsKey(key))
            {
                op.Result = BatchOperationResult.Skipped;
                result.SkippedCount++;
                return;
            }

            recordDict[key] = op.Record;
            op.Result = BatchOperationResult.Success;
            result.UpdatedCount++;
        }

        private void ProcessDelete(
            BatchOperationEntry<TRecord> op,
            TKey key,
            Dictionary<TKey, TRecord> recordDict,
            BatchProcessResult result)
        {
            if (!recordDict.Remove(key))
            {
                op.Result = BatchOperationResult.Skipped;
                result.SkippedCount++;
                return;
            }

            op.Result = BatchOperationResult.Success;
            result.DeletedCount++;
        }

        private void ProcessUpsert(
            BatchOperationEntry<TRecord> op,
            TKey key,
            Dictionary<TKey, TRecord> recordDict,
            BatchProcessResult result)
        {
            var existed = recordDict.ContainsKey(key);
            recordDict[key] = op.Record;
            op.Result = BatchOperationResult.Success;

            if (existed)
                result.UpdatedCount++;
            else
                result.AddedCount++;
        }

        /// <summary>
        /// Adds multiple records in bulk.
        /// </summary>
        /// <param name="records">Records to add</param>
        /// <param name="settings">Processing settings</param>
        /// <returns>Processing result</returns>
        public BatchProcessResult AddRange(IEnumerable<TRecord> records, BatchProcessSettings settings = null)
        {
            var operations = records.Select(r => BatchOperationEntry<TRecord>.CreateAdd(r));
            return Execute(operations, settings);
        }

        /// <summary>
        /// Updates multiple records in bulk.
        /// </summary>
        /// <param name="records">Records to update</param>
        /// <param name="settings">Processing settings</param>
        /// <returns>Processing result</returns>
        public BatchProcessResult UpdateRange(IEnumerable<TRecord> records, BatchProcessSettings settings = null)
        {
            var operations = records.Select(r => BatchOperationEntry<TRecord>.CreateUpdate(r));
            return Execute(operations, settings);
        }

        /// <summary>
        /// Deletes multiple records in bulk.
        /// </summary>
        /// <param name="records">Records to delete</param>
        /// <param name="settings">Processing settings</param>
        /// <returns>Processing result</returns>
        public BatchProcessResult DeleteRange(IEnumerable<TRecord> records, BatchProcessSettings settings = null)
        {
            var operations = records.Select(r => BatchOperationEntry<TRecord>.CreateDelete(r));
            return Execute(operations, settings);
        }

        /// <summary>
        /// Deletes multiple records in bulk by specifying keys.
        /// </summary>
        /// <param name="keys">Keys to delete</param>
        /// <param name="settings">Processing settings</param>
        /// <returns>Processing result</returns>
        public BatchProcessResult DeleteByKeys(IEnumerable<TKey> keys, BatchProcessSettings settings = null)
        {
            settings ??= new BatchProcessSettings();
            var result = new BatchProcessResult { Success = true };
            var startTime = DateTime.Now;

            var keyList = keys.ToList();
            var keySet = new HashSet<TKey>(keyList);

            var remainingRecords = table.All
                .Where(r => !keySet.Contains(keySelector(r)))
                .ToArray();

            result.DeletedCount = table.Count - remainingRecords.Length;
            table.SetRecords(remainingRecords);

            if (settings.AutoSort)
                table.EnsureSorted();

            result.ElapsedTime = DateTime.Now - startTime;
            return result;
        }

        /// <summary>
        /// Deletes all records matching a condition in bulk.
        /// </summary>
        /// <param name="predicate">Deletion condition</param>
        /// <param name="settings">Processing settings</param>
        /// <returns>Processing result</returns>
        public BatchProcessResult DeleteWhere(Func<TRecord, bool> predicate, BatchProcessSettings settings = null)
        {
            settings ??= new BatchProcessSettings();
            var result = new BatchProcessResult { Success = true };
            var startTime = DateTime.Now;

            var remainingRecords = table.All
                .Where(r => !predicate(r))
                .ToArray();

            result.DeletedCount = table.Count - remainingRecords.Length;
            table.SetRecords(remainingRecords);

            if (settings.AutoSort)
                table.EnsureSorted();

            result.ElapsedTime = DateTime.Now - startTime;
            return result;
        }

        /// <summary>
        /// Updates all records matching a condition in bulk.
        /// </summary>
        /// <param name="predicate">Update condition</param>
        /// <param name="updater">Update function</param>
        /// <param name="settings">Processing settings</param>
        /// <returns>Processing result</returns>
        public BatchProcessResult UpdateWhere(
            Func<TRecord, bool> predicate,
            Action<TRecord> updater,
            BatchProcessSettings settings = null)
        {
            settings ??= new BatchProcessSettings();
            var result = new BatchProcessResult { Success = true };
            var startTime = DateTime.Now;

            var records = table.All.ToArray();
            foreach (var record in records)
            {
                if (!predicate(record))
                    continue;
                try
                {
                    updater(record);
                    result.UpdatedCount++;
                }
                catch (Exception e)
                {
                    result.FailedCount++;
                    result.Errors.Add(e.Message);

                    if (!settings.ContinueOnError)
                    {
                        result.Success = false;
                        break;
                    }
                }
            }

            // Records are reference types, so changes are reflected automatically
            // Set the Dirty flag after changes
            EditorUtility.SetDirty(table);

            // However, re-sorting is required if a key was changed
            if (settings.AutoSort)
                table.EnsureSorted();

            result.ElapsedTime = DateTime.Now - startTime;
            return result;
        }

        /// <summary>
        /// Upserts multiple records in bulk.
        /// </summary>
        /// <param name="records">Records to upsert</param>
        /// <param name="settings">Processing settings</param>
        /// <returns>Processing result</returns>
        public BatchProcessResult UpsertRange(IEnumerable<TRecord> records, BatchProcessSettings settings = null)
        {
            var operations = records.Select(r => BatchOperationEntry<TRecord>.CreateUpsert(r));
            return Execute(operations, settings);
        }

        /// <summary>
        /// Replaces all records in the table.
        /// </summary>
        /// <param name="records">New records</param>
        /// <param name="settings">Processing settings</param>
        /// <returns>Processing result</returns>
        public BatchProcessResult ReplaceAll(IEnumerable<TRecord> records, BatchProcessSettings settings = null)
        {
            settings ??= new BatchProcessSettings();
            var result = new BatchProcessResult { Success = true };
            var startTime = DateTime.Now;

            var newRecords = records.ToArray();
            var oldCount = table.Count;

            table.SetRecords(newRecords);

            if (settings.AutoSort)
                table.EnsureSorted();

            result.DeletedCount = oldCount;
            result.AddedCount = newRecords.Length;
            result.ElapsedTime = DateTime.Now - startTime;
            return result;
        }
    }

    /// <summary>
    /// Extension methods for batch processing.
    /// </summary>
    public static class BatchProcessorExtensions
    {
        /// <summary>
        /// Creates a batch processor from a table.
        /// </summary>
        /// <typeparam name="TRecord">Record type</typeparam>
        /// <typeparam name="TKey">Key type</typeparam>
        /// <param name="table">Target table</param>
        /// <param name="keySelector">Key selector function</param>
        /// <returns>Batch processor</returns>
        public static BatchProcessor<TRecord, TKey> CreateBatchProcessor<TRecord, TKey>(
            this TableAsset<TRecord, TKey> table,
            Func<TRecord, TKey> keySelector)
            where TRecord : class, new()
            where TKey : IComparable<TKey>
        {
            return new BatchProcessor<TRecord, TKey>(table, keySelector);
        }
    }
}
