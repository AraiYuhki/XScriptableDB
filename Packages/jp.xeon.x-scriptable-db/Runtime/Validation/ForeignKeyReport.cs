using System.Collections.Generic;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// Foreign key reference report.
    /// </summary>
    public class ForeignKeyReport
    {
        /// <summary>Source table name</summary>
        public string SourceTableName { get; set; }

        /// <summary>Target table name</summary>
        public string TargetTableName { get; set; }

        /// <summary>Foreign key field name</summary>
        public string ForeignKeyField { get; set; }

        /// <summary>Total number of references</summary>
        public int TotalReferences { get; set; }

        /// <summary>List of invalid reference values</summary>
        public List<object> InvalidReferences { get; } = new();

        /// <summary>List of error messages</summary>
        public List<string> Errors { get; } = new();

        /// <summary>Whether the report is valid</summary>
        public bool IsValid => InvalidReferences.Count == 0 && Errors.Count == 0;

        /// <summary>Number of invalid references</summary>
        public int InvalidReferenceCount => InvalidReferences.Count;

        /// <summary>
        /// Returns a summary string.
        /// </summary>
        public string GetSummary()
        {
            if (IsValid)
            {
                return $"{SourceTableName}.{ForeignKeyField} -> {TargetTableName}: " +
                       $"All {TotalReferences} references are valid.";
            }

            return $"{SourceTableName}.{ForeignKeyField} -> {TargetTableName}: " +
                   $"{InvalidReferenceCount}/{TotalReferences} invalid references found.";
        }
    }
}