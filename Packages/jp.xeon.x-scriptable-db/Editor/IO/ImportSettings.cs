using System.Text;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// テーブルインポーターの設定。
    /// </summary>
    public class ImportSettings
    {
        /// <summary>ファイルパス</summary>
        public string FilePath { get; set; }

        /// <summary>エンコーディング</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>区切り文字（nullの場合は自動検出）</summary>
        public char? Delimiter { get; set; }

        /// <summary>プレビューを表示するかどうか</summary>
        public bool ShowPreview { get; set; } = true;

        /// <summary>変更されていないレコードをスキップするかどうか</summary>
        public bool SkipUnchangedRecords { get; set; } = false;
    }
}