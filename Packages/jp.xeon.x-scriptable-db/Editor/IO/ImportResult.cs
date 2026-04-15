using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Import result.
    /// </summary>
    public class ImportResult
    {
        /// <summary>Whether the import was successful</summary>
        public bool Success { get; set; }

        /// <summary>Number of imported records</summary>
        public int ImportedCount { get; set; }

        /// <summary>Number of added records</summary>
        public int AddedCount { get; set; }

        /// <summary>Number of updated records</summary>
        public int UpdatedCount { get; set; }

        /// <summary>Number of deleted records</summary>
        public int DeletedCount { get; set; }

        /// <summary>Error message</summary>
        public string ErrorMessage { get; set; }

        /// <summary>Warning list</summary>
        public List<string> Warnings { get; set; } = new();
    }
}