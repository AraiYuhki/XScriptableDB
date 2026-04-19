using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// マイグレーション実行結果。
    /// </summary>
    public class MigrationResult
    {
        public bool IsSuccess { get; set; }
        public int ProcessedCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public TimeSpan ExecutionTime { get; set; }
    }
}