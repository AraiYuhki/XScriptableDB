using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Progress information for a streaming import.
    /// </summary>
    public class StreamingImportProgress
    {
        /// <summary>Current state</summary>
        public StreamingImportState State { get; set; } = StreamingImportState.NotStarted;

        /// <summary>Total line count</summary>
        public int TotalLines { get; set; }

        /// <summary>Number of processed lines</summary>
        public int ProcessedLines { get; set; }

        /// <summary>Number of successfully imported records</summary>
        public int SuccessCount { get; set; }

        /// <summary>Number of failed records</summary>
        public int ErrorCount { get; set; }

        /// <summary>Warning list</summary>
        public List<string> Warnings { get; } = new();

        /// <summary>Error list</summary>
        public List<string> Errors { get; } = new();

        /// <summary>Current chunk number</summary>
        public int CurrentChunk { get; set; }

        /// <summary>Total chunk count</summary>
        public int TotalChunks { get; set; }

        /// <summary>Processing progress (0.0 to 1.0)</summary>
        public float Progress
        {
            get
            {
                if (TotalLines <= 0)
                    return 0f;
                return (float)ProcessedLines / TotalLines;
            }
        }

        /// <summary>Error message (on failure)</summary>
        public string ErrorMessage { get; set; }

        /// <summary>Start time</summary>
        public DateTime StartTime { get; set; }

        /// <summary>End time</summary>
        public DateTime? EndTime { get; set; }

        /// <summary>Elapsed time</summary>
        public TimeSpan ElapsedTime => (EndTime ?? DateTime.Now) - StartTime;
    }
}