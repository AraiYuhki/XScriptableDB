using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Batch processing result.
    /// </summary>
    public class BatchProcessResult
    {
        /// <summary>Whether the operation succeeded</summary>
        public bool Success { get; set; }

        /// <summary>Number of added records</summary>
        public int AddedCount { get; set; }

        /// <summary>Number of updated records</summary>
        public int UpdatedCount { get; set; }

        /// <summary>Number of deleted records</summary>
        public int DeletedCount { get; set; }

        /// <summary>Number of skipped records</summary>
        public int SkippedCount { get; set; }

        /// <summary>Number of failed records</summary>
        public int FailedCount { get; set; }

        /// <summary>Total number of processed records</summary>
        public int TotalCount => AddedCount + UpdatedCount + DeletedCount + SkippedCount + FailedCount;

        /// <summary>Error list</summary>
        public List<string> Errors { get; } = new();

        /// <summary>Processing time</summary>
        public TimeSpan ElapsedTime { get; set; }
    }
}