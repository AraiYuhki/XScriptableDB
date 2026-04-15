using System;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Batch processing settings.
    /// </summary>
    public class BatchProcessSettings
    {
        /// <summary>Whether to continue on error</summary>
        public bool ContinueOnError { get; set; } = true;

        /// <summary>Whether to auto-sort</summary>
        public bool AutoSort { get; set; } = true;

        /// <summary>Whether to allow duplicate keys</summary>
        public bool AllowDuplicateKeys { get; set; } = false;

        /// <summary>Progress callback</summary>
        public Action<int, int> OnProgress { get; set; }
    }
}