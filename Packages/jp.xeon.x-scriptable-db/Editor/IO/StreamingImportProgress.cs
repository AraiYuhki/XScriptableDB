using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// ストリーミングインポートの進捗情報。
    /// </summary>
    public class StreamingImportProgress
    {
        /// <summary>現在の状態</summary>
        public StreamingImportState State { get; set; } = StreamingImportState.NotStarted;

        /// <summary>総行数</summary>
        public int TotalLines { get; set; }

        /// <summary>処理済み行数</summary>
        public int ProcessedLines { get; set; }

        /// <summary>成功したレコード数</summary>
        public int SuccessCount { get; set; }

        /// <summary>失敗したレコード数</summary>
        public int ErrorCount { get; set; }

        /// <summary>警告リスト</summary>
        public List<string> Warnings { get; } = new();

        /// <summary>エラーリスト</summary>
        public List<string> Errors { get; } = new();

        /// <summary>現在のチャンク番号</summary>
        public int CurrentChunk { get; set; }

        /// <summary>総チャンク数</summary>
        public int TotalChunks { get; set; }

        /// <summary>処理の進捗（0.0〜1.0）</summary>
        public float Progress
        {
            get
            {
                if (TotalLines <= 0)
                    return 0f;
                return (float)ProcessedLines / TotalLines;
            }
        }

        /// <summary>エラーメッセージ（失敗時）</summary>
        public string ErrorMessage { get; set; }

        /// <summary>開始時刻</summary>
        public DateTime StartTime { get; set; }

        /// <summary>終了時刻</summary>
        public DateTime? EndTime { get; set; }

        /// <summary>経過時間</summary>
        public TimeSpan ElapsedTime => (EndTime ?? DateTime.Now) - StartTime;
    }
}