using System;

namespace Xeon.XScriptableDB.Performance
{
    /// <summary>
    /// Memory usage information for a table.
    /// </summary>
    public struct TableMemoryInfo
    {
        public string TableName;
        public Type TableType;
        public Type RecordType;
        public int RecordCount;
        public long EstimatedRecordSize;
        public long EstimatedTotalSize;

        public override string ToString()
        {
            return $"{TableName}: {RecordCount} records, ~{FormatBytes(EstimatedTotalSize)}";
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }
    }
}