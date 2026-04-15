using System.Text;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Export settings.
    /// </summary>
    public class ExportSettings
    {
        /// <summary>Output file path</summary>
        public string FilePath { get; set; }

        /// <summary>Encoding</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>Delimiter character (defaults to comma)</summary>
        public char Delimiter { get; set; } = ',';

        /// <summary>Whether to sort by PrimaryKey</summary>
        public bool SortByPrimaryKey { get; set; } = true;

        /// <summary>Column order for export (definition order if null)</summary>
        public string[] ColumnOrder { get; set; }

        /// <summary>Columns to exclude</summary>
        public string[] ExcludeColumns { get; set; }

        /// <summary>Whether to write a BOM (for UTF-8)</summary>
        public bool WriteBom { get; set; } = false;
    }
}