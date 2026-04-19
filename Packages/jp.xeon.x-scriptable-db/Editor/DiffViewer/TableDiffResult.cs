using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// テーブル全体の差分結果。
    /// </summary>
    [Serializable]
    public class TableDiffResult
    {
        /// <summary>テーブル名</summary>
        public string TableName { get; set; }

        /// <summary>レコード型名</summary>
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
        /// カウントを再計算します。
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
        /// 差分の概要を取得します。
        /// </summary>
        public string GetSummary()
        {
            return $"{TableName}: +{AddedCount} -{RemovedCount} *{ModifiedCount} ={UnchangedCount}";
        }
    }
}
