namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// バッチ操作の種類。
    /// </summary>
    public enum BatchOperationType
    {
        /// <summary>追加</summary>
        Add,

        /// <summary>更新</summary>
        Update,

        /// <summary>削除</summary>
        Delete,

        /// <summary>アップサート（存在すれば更新、なければ追加）</summary>
        Upsert
    }

    /// <summary>
    /// バッチ操作の結果。
    /// </summary>
    public enum BatchOperationResult
    {
        /// <summary>成功</summary>
        Success,

        /// <summary>スキップ</summary>
        Skipped,

        /// <summary>失敗</summary>
        Failed
    }

    /// <summary>
    /// バッチ操作のエントリ。
    /// </summary>
    /// <typeparam name="TRecord">レコードの型</typeparam>
    public class BatchOperationEntry<TRecord>
    {
        /// <summary>操作の種類</summary>
        public BatchOperationType Type { get; set; }

        /// <summary>対象レコード</summary>
        public TRecord Record { get; set; }

        /// <summary>操作結果</summary>
        public BatchOperationResult Result { get; set; } = BatchOperationResult.Success;

        /// <summary>エラーメッセージ</summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 追加操作のエントリを作成する。
        /// </summary>
        public static BatchOperationEntry<TRecord> CreateAdd(TRecord record)
        {
            return new BatchOperationEntry<TRecord>
            {
                Type = BatchOperationType.Add,
                Record = record
            };
        }

        /// <summary>
        /// 更新操作のエントリを作成する。
        /// </summary>
        public static BatchOperationEntry<TRecord> CreateUpdate(TRecord record)
        {
            return new BatchOperationEntry<TRecord>
            {
                Type = BatchOperationType.Update,
                Record = record
            };
        }

        /// <summary>
        /// 削除操作のエントリを作成する。
        /// </summary>
        public static BatchOperationEntry<TRecord> CreateDelete(TRecord record)
        {
            return new BatchOperationEntry<TRecord>
            {
                Type = BatchOperationType.Delete,
                Record = record
            };
        }

        /// <summary>
        /// アップサート操作のエントリを作成する。
        /// </summary>
        public static BatchOperationEntry<TRecord> CreateUpsert(TRecord record)
        {
            return new BatchOperationEntry<TRecord>
            {
                Type = BatchOperationType.Upsert,
                Record = record
            };
        }
    }

    /// <summary>
    /// 非ジェネリックのバッチ操作エントリ。
    /// </summary>
    public class BatchOperationEntry
    {
        /// <summary>操作の種類</summary>
        public BatchOperationType Type { get; set; }

        /// <summary>対象レコード</summary>
        public object Record { get; set; }

        /// <summary>操作結果</summary>
        public BatchOperationResult Result { get; set; } = BatchOperationResult.Success;

        /// <summary>エラーメッセージ</summary>
        public string ErrorMessage { get; set; }
    }
}
