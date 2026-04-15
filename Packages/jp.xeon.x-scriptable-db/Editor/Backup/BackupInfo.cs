using System;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Backup information.
    /// </summary>
    [Serializable]
    public class BackupInfo
    {
        public string Id;
        public string Name;
        public string Description;
        public string CreatedAt;
        public string TableName;
        public string TableType;
        public int RecordCount;
        public string FilePath;
        public long FileSize;
        public bool IsAutoBackup;
    }
}