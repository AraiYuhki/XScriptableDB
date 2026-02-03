using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
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
}