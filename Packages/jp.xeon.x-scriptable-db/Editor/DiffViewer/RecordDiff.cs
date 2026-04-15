using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Diff information at the record level.
    /// </summary>
    [Serializable]
    public class RecordDiff
    {
        /// <summary>PrimaryKey value</summary>
        public object PrimaryKey { get; set; }

        /// <summary>Type of diff</summary>
        public DiffType DiffType { get; set; }

        /// <summary>Record before change (on deletion or modification)</summary>
        public object OldRecord { get; set; }

        /// <summary>Record after change (on addition or modification)</summary>
        public object NewRecord { get; set; }

        /// <summary>Record index in the original table</summary>
        public int OldIndex { get; set; } = -1;

        /// <summary>Record index in the new data</summary>
        public int NewIndex { get; set; } = -1;

        /// <summary>List of field-level diffs</summary>
        public List<FieldDiff> FieldDiffs { get; set; } = new();

        /// <summary>
        /// Number of changed fields.
        /// </summary>
        public int ChangedFieldCount
        {
            get
            {
                var count = 0;
                foreach (var diff in FieldDiffs)
                {
                    if (diff.HasChanged)
                        count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Gets the diff summary.
        /// </summary>
        public string Summary
        {
            get
            {
                return DiffType switch
                {
                    DiffType.Added => $"[+] Key={PrimaryKey}",
                    DiffType.Removed => $"[-] Key={PrimaryKey}",
                    DiffType.Modified => $"[*] Key={PrimaryKey} ({ChangedFieldCount} fields)",
                    _ => $"[=] Key={PrimaryKey}"
                };
            }
        }
    }
}
