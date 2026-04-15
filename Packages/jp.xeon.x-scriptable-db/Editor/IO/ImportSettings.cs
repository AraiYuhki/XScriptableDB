using System.Text;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Settings for the table importer.
    /// </summary>
    public class ImportSettings
    {
        /// <summary>File path</summary>
        public string FilePath { get; set; }

        /// <summary>Encoding</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>Delimiter character (auto-detected if null)</summary>
        public char? Delimiter { get; set; }

        /// <summary>Whether to show a preview</summary>
        public bool ShowPreview { get; set; } = true;

        /// <summary>Whether to skip unchanged records</summary>
        public bool SkipUnchangedRecords { get; set; } = false;
    }
}