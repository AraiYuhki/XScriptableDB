namespace Xeon.XScriptableDB.Editor
{
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