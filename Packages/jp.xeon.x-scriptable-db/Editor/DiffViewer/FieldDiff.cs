using System;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Diff information at the field level.
    /// </summary>
    [Serializable]
    public class FieldDiff
    {
        /// <summary>Field name</summary>
        public string FieldName { get; set; }

        /// <summary>Value before change</summary>
        public object OldValue { get; set; }

        /// <summary>Value after change</summary>
        public object NewValue { get; set; }

        /// <summary>Type of diff</summary>
        public DiffType DiffType { get; set; }

        public FieldDiff() { }

        public FieldDiff(string fieldName, object oldValue, object newValue, DiffType diffType)
        {
            FieldName = fieldName;
            OldValue = oldValue;
            NewValue = newValue;
            DiffType = diffType;
        }

        /// <summary>
        /// Whether the value has changed.
        /// </summary>
        public bool HasChanged => DiffType != DiffType.Unchanged;

        public override string ToString()
        {
            return DiffType switch
            {
                DiffType.Unchanged => $"{FieldName}: {OldValue} (unchanged)",
                DiffType.Added => $"{FieldName}: + {NewValue}",
                DiffType.Removed => $"{FieldName}: - {OldValue}",
                DiffType.Modified => $"{FieldName}: {OldValue} -> {NewValue}",
                _ => $"{FieldName}: ?"
            };
        }
    }
}
