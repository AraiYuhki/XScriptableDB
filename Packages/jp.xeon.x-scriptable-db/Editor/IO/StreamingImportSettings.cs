using System;
using System.Text;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Settings for a streaming import.
    /// </summary>
    public class StreamingImportSettings
    {
        /// <summary>Chunk size (number of lines processed at a time)</summary>
        public int ChunkSize { get; set; } = 1000;

        /// <summary>Encoding</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>Delimiter character</summary>
        public char Delimiter { get; set; } = ',';

        /// <summary>Whether a header row is present</summary>
        public bool HasHeader { get; set; } = true;

        /// <summary>Whether to continue on error</summary>
        public bool ContinueOnError { get; set; } = true;

        /// <summary>Maximum error count (processing stops when this is exceeded)</summary>
        public int MaxErrors { get; set; } = 100;

        /// <summary>Progress callback</summary>
        public Action<StreamingImportProgress> OnProgress { get; set; }
    }
}