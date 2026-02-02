using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// バッチ処理の設定。
    /// </summary>
    public class BatchProcessSettings
    {
        /// <summary>エラー時に継続するか</summary>
        public bool ContinueOnError { get; set; } = true;

        /// <summary>自動ソートを行うか</summary>
        public bool AutoSort { get; set; } = true;

        /// <summary>重複キーを許可するか</summary>
        public bool AllowDuplicateKeys { get; set; } = false;

        /// <summary>進捗コールバック</summary>
        public Action<int, int> OnProgress { get; set; }
    }

    /// <summary>
    /// バッチ処理の結果。
    /// </summary>
    public class BatchProcessResult
    {
        /// <summary>成功したかどうか</summary>
        public bool Success { get; set; }

        /// <summary>追加された件数</summary>
        public int AddedCount { get; set; }

        /// <summary>更新された件数</summary>
        public int UpdatedCount { get; set; }

        /// <summary>削除された件数</summary>
        public int DeletedCount { get; set; }

        /// <summary>スキップされた件数</summary>
        public int SkippedCount { get; set; }

        /// <summary>失敗した件数</summary>
        public int FailedCount { get; set; }

        /// <summary>合計処理件数</summary>
        public int TotalCount => AddedCount + UpdatedCount + DeletedCount + SkippedCount + FailedCount;

        /// <summary>エラーリスト</summary>
        public List<string> Errors { get; } = new();

        /// <summary>処理時間</summary>
        public TimeSpan ElapsedTime { get; set; }
    }

    /// <summary>
    /// テーブルに対するバッチ処理を行うクラス。
    /// TableAsset&lt;TRecord, TKey&gt;のジェネリック引数順序に対応。
    /// </summary>
    /// <typeparam name="TRecord">レコードの型</typeparam>
    /// <typeparam name="TKey">キーの型</typeparam>
    public class BatchProcessor<TRecord, TKey>
        where TRecord : class, new()
        where TKey : IComparable<TKey>
    {
        private readonly TableAsset<TRecord, TKey> table;
        private readonly Func<TRecord, TKey> keySelector;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="table">対象テーブル</param>
        /// <param name="keySelector">キー選択関数</param>
        public BatchProcessor(TableAsset<TRecord, TKey> table, Func<TRecord, TKey> keySelector)
        {
            this.table = table ?? throw new ArgumentNullException(nameof(table));
            this.keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        }

        /// <summary>
        /// バッチ操作を実行する。
        /// </summary>
        /// <param name="operations">操作リスト</param>
        /// <param name="settings">処理設定</param>
        /// <returns>処理結果</returns>
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

            // 現在のレコードをDictionaryにコピー
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

            // 結果を反映
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
        /// 複数のレコードを一括追加する。
        /// </summary>
        /// <param name="records">追加するレコード</param>
        /// <param name="settings">処理設定</param>
        /// <returns>処理結果</returns>
        public BatchProcessResult AddRange(IEnumerable<TRecord> records, BatchProcessSettings settings = null)
        {
            var operations = records.Select(r => BatchOperationEntry<TRecord>.CreateAdd(r));
            return Execute(operations, settings);
        }

        /// <summary>
        /// 複数のレコードを一括更新する。
        /// </summary>
        /// <param name="records">更新するレコード</param>
        /// <param name="settings">処理設定</param>
        /// <returns>処理結果</returns>
        public BatchProcessResult UpdateRange(IEnumerable<TRecord> records, BatchProcessSettings settings = null)
        {
            var operations = records.Select(r => BatchOperationEntry<TRecord>.CreateUpdate(r));
            return Execute(operations, settings);
        }

        /// <summary>
        /// 複数のレコードを一括削除する。
        /// </summary>
        /// <param name="records">削除するレコード</param>
        /// <param name="settings">処理設定</param>
        /// <returns>処理結果</returns>
        public BatchProcessResult DeleteRange(IEnumerable<TRecord> records, BatchProcessSettings settings = null)
        {
            var operations = records.Select(r => BatchOperationEntry<TRecord>.CreateDelete(r));
            return Execute(operations, settings);
        }

        /// <summary>
        /// キーを指定して複数のレコードを一括削除する。
        /// </summary>
        /// <param name="keys">削除するキー</param>
        /// <param name="settings">処理設定</param>
        /// <returns>処理結果</returns>
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
        /// 条件に一致するレコードを一括削除する。
        /// </summary>
        /// <param name="predicate">削除条件</param>
        /// <param name="settings">処理設定</param>
        /// <returns>処理結果</returns>
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
        /// 条件に一致するレコードを一括更新する。
        /// </summary>
        /// <param name="predicate">更新条件</param>
        /// <param name="updater">更新関数</param>
        /// <param name="settings">処理設定</param>
        /// <returns>処理結果</returns>
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
                if (predicate(record))
                {
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
            }

            // レコードは参照型なので、変更は自動的に反映される
            // 変更後にDirtyフラグをセット
            EditorUtility.SetDirty(table);

            // ただし、キーが変更された場合はソートが必要
            if (settings.AutoSort)
                table.EnsureSorted();

            result.ElapsedTime = DateTime.Now - startTime;
            return result;
        }

        /// <summary>
        /// 複数のレコードを一括アップサートする。
        /// </summary>
        /// <param name="records">アップサートするレコード</param>
        /// <param name="settings">処理設定</param>
        /// <returns>処理結果</returns>
        public BatchProcessResult UpsertRange(IEnumerable<TRecord> records, BatchProcessSettings settings = null)
        {
            var operations = records.Select(r => BatchOperationEntry<TRecord>.CreateUpsert(r));
            return Execute(operations, settings);
        }

        /// <summary>
        /// テーブルの全レコードを置換する。
        /// </summary>
        /// <param name="records">新しいレコード</param>
        /// <param name="settings">処理設定</param>
        /// <returns>処理結果</returns>
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
    /// バッチ処理の拡張メソッド。
    /// </summary>
    public static class BatchProcessorExtensions
    {
        /// <summary>
        /// テーブルからバッチプロセッサを作成する。
        /// </summary>
        /// <typeparam name="TRecord">レコードの型</typeparam>
        /// <typeparam name="TKey">キーの型</typeparam>
        /// <param name="table">対象テーブル</param>
        /// <param name="keySelector">キー選択関数</param>
        /// <returns>バッチプロセッサ</returns>
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
