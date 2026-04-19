using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
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

        /// <summary>変更前のレコード（削除または変更時）</summary>
        public object OldRecord { get; set; }

        /// <summary>変更後のレコード（追加または変更時）</summary>
        public object NewRecord { get; set; }

        /// <summary>元のテーブルでのレコードインデックス</summary>
        public int OldIndex { get; set; } = -1;

        /// <summary>新しいデータでのレコードインデックス</summary>
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
        /// 差分の概要を取得します。
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
