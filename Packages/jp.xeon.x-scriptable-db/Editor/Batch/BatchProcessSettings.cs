using System;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// バッチ処理設定。
    /// </summary>
    public class BatchProcessSettings
    {
        /// <summary>エラー時に続行するかどうか</summary>
        public bool ContinueOnError { get; set; } = true;

        /// <summary>自動ソートするかどうか</summary>
        public bool AutoSort { get; set; } = true;

        /// <summary>重複キーを許可するかどうか</summary>
        public bool AllowDuplicateKeys { get; set; } = false;

        /// <summary>進捗コールバック</summary>
        public Action<int, int> OnProgress { get; set; }
    }
}