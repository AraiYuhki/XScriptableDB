using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// バッチ処理結果。
    /// </summary>
    public class BatchProcessResult
    {
        /// <summary>操作が成功したかどうか</summary>
        public bool Success { get; set; }

        /// <summary>追加されたレコード数</summary>
        public int AddedCount { get; set; }

        /// <summary>更新されたレコード数</summary>
        public int UpdatedCount { get; set; }

        /// <summary>削除されたレコード数</summary>
        public int DeletedCount { get; set; }

        /// <summary>スキップされたレコード数</summary>
        public int SkippedCount { get; set; }

        /// <summary>失敗したレコード数</summary>
        public int FailedCount { get; set; }

        /// <summary>処理されたレコードの総数</summary>
        public int TotalCount => AddedCount + UpdatedCount + DeletedCount + SkippedCount + FailedCount;

        /// <summary>エラーリスト</summary>
        public List<string> Errors { get; } = new();

        /// <summary>処理時間</summary>
        public TimeSpan ElapsedTime { get; set; }
    }
}