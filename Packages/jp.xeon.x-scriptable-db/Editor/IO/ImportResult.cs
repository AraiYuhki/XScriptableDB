using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// インポート結果。
    /// </summary>
    public class ImportResult
    {
        /// <summary>インポートが成功したかどうか</summary>
        public bool Success { get; set; }

        /// <summary>インポートされたレコード数</summary>
        public int ImportedCount { get; set; }

        /// <summary>追加されたレコード数</summary>
        public int AddedCount { get; set; }

        /// <summary>更新されたレコード数</summary>
        public int UpdatedCount { get; set; }

        /// <summary>削除されたレコード数</summary>
        public int DeletedCount { get; set; }

        /// <summary>エラーメッセージ</summary>
        public string ErrorMessage { get; set; }

        /// <summary>警告リスト</summary>
        public List<string> Warnings { get; set; } = new();
    }
}