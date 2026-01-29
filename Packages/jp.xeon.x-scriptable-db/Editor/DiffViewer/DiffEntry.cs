using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// 差分の種類。
    /// </summary>
    public enum DiffType
    {
        /// <summary>変更なし</summary>
        Unchanged,
        /// <summary>追加</summary>
        Added,
        /// <summary>削除</summary>
        Removed,
        /// <summary>変更</summary>
        Modified
    }

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

    /// <summary>
    /// レコードレベルの差分情報。
    /// </summary>
    [Serializable]
    public class RecordDiff
    {
        /// <summary>PrimaryKeyの値</summary>
        public object PrimaryKey { get; set; }

        /// <summary>差分の種類</summary>
        public DiffType DiffType { get; set; }

        /// <summary>変更前のレコード（削除・変更時）</summary>
        public object OldRecord { get; set; }

        /// <summary>変更後のレコード（追加・変更時）</summary>
        public object NewRecord { get; set; }

        /// <summary>レコードのインデックス（元のテーブル内）</summary>
        public int OldIndex { get; set; } = -1;

        /// <summary>レコードのインデックス（新しいデータ内）</summary>
        public int NewIndex { get; set; } = -1;

        /// <summary>フィールドレベルの差分リスト</summary>
        public List<FieldDiff> FieldDiffs { get; set; } = new();

        /// <summary>
        /// 変更されたフィールドの数。
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
        /// 差分の概要を取得する。
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

    /// <summary>
    /// テーブル全体の差分結果。
    /// </summary>
    [Serializable]
    public class TableDiffResult
    {
        /// <summary>テーブル名</summary>
        public string TableName { get; set; }

        /// <summary>レコードの型名</summary>
        public string RecordTypeName { get; set; }

        /// <summary>差分リスト</summary>
        public List<RecordDiff> Diffs { get; set; } = new();

        /// <summary>追加されたレコード数</summary>
        public int AddedCount { get; private set; }

        /// <summary>削除されたレコード数</summary>
        public int RemovedCount { get; private set; }

        /// <summary>変更されたレコード数</summary>
        public int ModifiedCount { get; private set; }

        /// <summary>変更なしのレコード数</summary>
        public int UnchangedCount { get; private set; }

        /// <summary>
        /// 差分があるかどうか。
        /// </summary>
        public bool HasDifferences => AddedCount > 0 || RemovedCount > 0 || ModifiedCount > 0;

        /// <summary>
        /// カウントを再計算する。
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
        /// 差分サマリーを取得する。
        /// </summary>
        public string GetSummary()
        {
            return $"{TableName}: +{AddedCount} -{RemovedCount} *{ModifiedCount} ={UnchangedCount}";
        }
    }
}
