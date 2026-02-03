using System.Text;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// エクスポート設定。
    /// </summary>
    public class ExportSettings
    {
        /// <summary>出力先ファイルパス</summary>
        public string FilePath { get; set; }

        /// <summary>エンコーディング</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>区切り文字（デフォルトはカンマ）</summary>
        public char Delimiter { get; set; } = ',';

        /// <summary>PrimaryKeyでソートするかどうか</summary>
        public bool SortByPrimaryKey { get; set; } = true;

        /// <summary>エクスポートするカラムの順序（nullの場合は定義順）</summary>
        public string[] ColumnOrder { get; set; }

        /// <summary>除外するカラム</summary>
        public string[] ExcludeColumns { get; set; }

        /// <summary>BOMを出力するかどうか（UTF-8の場合）</summary>
        public bool WriteBom { get; set; } = false;
    }
}