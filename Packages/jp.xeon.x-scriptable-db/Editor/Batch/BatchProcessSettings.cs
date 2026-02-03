using System;

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
}