using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Backup manifest.
    /// </summary>
    [Serializable]
    public class BackupManifest
    {
        public List<BackupInfo> Backups = new();
        public string LastBackupAt;
        public int MaxBackupCount = 10;
        public bool AutoBackupEnabled = true;
    }
}