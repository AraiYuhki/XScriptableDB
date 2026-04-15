using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Diff result for an entire table.
    /// </summary>
    [Serializable]
    public class TableDiffResult
    {
        /// <summary>Table name</summary>
        public string TableName { get; set; }

        /// <summary>Record type name</summary>
        public string RecordTypeName { get; set; }

        /// <summary>List of diffs</summary>
        public List<RecordDiff> Diffs { get; set; } = new();

        /// <summary>Number of added records</summary>
        public int AddedCount { get; private set; }

        /// <summary>Number of removed records</summary>
        public int RemovedCount { get; private set; }

        /// <summary>Number of modified records</summary>
        public int ModifiedCount { get; private set; }

        /// <summary>Number of unchanged records</summary>
        public int UnchangedCount { get; private set; }

        /// <summary>
        /// Whether there are any differences.
        /// </summary>
        public bool HasDifferences => AddedCount > 0 || RemovedCount > 0 || ModifiedCount > 0;

        /// <summary>
        /// Recalculates the counts.
        /// </summary>
        public void RecalculateCounts()
        {
            AddedCount = 0;
            RemovedCount = 0;
            ModifiedCount = 0;
            UnchangedCount = 0;

            foreach (var diff in Diffs)
            {
                switch (diff.DiffType)
                {
                    case DiffType.Added:
                        AddedCount++;
                        break;
                    case DiffType.Removed:
                        RemovedCount++;
                        break;
                    case DiffType.Modified:
                        ModifiedCount++;
                        break;
                    case DiffType.Unchanged:
                        UnchangedCount++;
                        break;
                }
            }
        }

        /// <summary>
        /// Gets the diff summary.
        /// </summary>
        public string GetSummary()
        {
            return $"{TableName}: +{AddedCount} -{RemovedCount} *{ModifiedCount} ={UnchangedCount}";
        }
    }
}
