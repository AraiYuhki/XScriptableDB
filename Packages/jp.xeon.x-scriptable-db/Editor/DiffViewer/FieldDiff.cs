using System;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// フィールドレベルの差分情報。
    /// </summary>
    [Serializable]
    public class FieldDiff
    {
        /// <summary>フィールド名</summary>
        public string FieldName { get; set; }

        /// <summary>変更前の値</summary>
        public object OldValue { get; set; }

        /// <summary>変更後の値</summary>
        public object NewValue { get; set; }

        /// <summary>差分の種類</summary>
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
        /// 値が変更されたかどうか。
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
