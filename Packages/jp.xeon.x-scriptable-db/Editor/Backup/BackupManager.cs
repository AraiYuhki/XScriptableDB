using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Backup management class.
    /// </summary>
    public static class BackupManager
    {
        private const string BackupFolderName = "XScriptableDB_Backups";
        private const string ManifestFileName = "backup_manifest.json";

        private static string BackupFolder => Path.Combine(Application.dataPath, "..", BackupFolderName);
        private static string ManifestPath => Path.Combine(BackupFolder, ManifestFileName);

        /// <summary>
        /// Creates a backup of the table.
        /// </summary>
        public static BackupInfo CreateBackup(ITableAsset table, string name = null, string description = null, bool isAuto = false)
        {
            EnsureBackupFolder();

            var manifest = LoadManifest();
            var backupId = Guid.NewGuid().ToString("N")[..8];
            var timestamp = DateTime.Now;
            var tableName = table.GetType().Name;

            var backupInfo = new BackupInfo
            {
                Id = backupId,
                Name = name ?? $"{tableName}_{timestamp:yyyyMMdd_HHmmss}",
                Description = description ?? (isAuto ? "Auto backup" : "Manual backup"),
                CreatedAt = timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                TableName = tableName,
                TableType = table.RecordType.FullName,
                RecordCount = table.Count,
                IsAutoBackup = isAuto
            };

            // Create the backup file
            var fileName = $"{backupInfo.Name}_{backupId}.json";
            var filePath = Path.Combine(BackupFolder, fileName);

            var backupData = SerializeTable(table);
            File.WriteAllText(filePath, backupData);

            backupInfo.FilePath = filePath;
            backupInfo.FileSize = new FileInfo(filePath).Length;

            // Update the manifest
            manifest.Backups.Insert(0, backupInfo);
            manifest.LastBackupAt = timestamp.ToString("yyyy-MM-dd HH:mm:ss");

            // Delete old backups
            CleanupOldBackups(manifest);

            SaveManifest(manifest);

            Debug.Log($"[BackupManager] Backup created: {backupInfo.Name} ({backupInfo.RecordCount} records)");
            return backupInfo;
        }

        /// <summary>
        /// Restores from a backup.
        /// </summary>
        public static bool RestoreBackup(BackupInfo backupInfo, ITableAsset targetTable)
        {
            if (!File.Exists(backupInfo.FilePath))
            {
                Debug.LogError($"[BackupManager] Backup file not found: {backupInfo.FilePath}");
                return false;
            }

            try
            {
                var backupData = File.ReadAllText(backupInfo.FilePath);
                DeserializeToTable(backupData, targetTable);

                EditorUtility.SetDirty(targetTable as UnityEngine.Object);
                AssetDatabase.SaveAssets();

                Debug.Log($"[BackupManager] Restored from backup: {backupInfo.Name}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BackupManager] Restore failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes a backup.
        /// </summary>
        public static bool DeleteBackup(BackupInfo backupInfo)
        {
            var manifest = LoadManifest();
            var backup = manifest.Backups.FirstOrDefault(b => b.Id == backupInfo.Id);

            if (backup == null)
                return false;

            if (File.Exists(backup.FilePath))
                File.Delete(backup.FilePath);

            manifest.Backups.Remove(backup);
            SaveManifest(manifest);

            Debug.Log($"[BackupManager] Backup deleted: {backupInfo.Name}");
            return true;
        }

        /// <summary>
        /// Gets all backup information.
        /// </summary>
        public static List<BackupInfo> GetAllBackups()
        {
            var manifest = LoadManifest();
            return manifest.Backups;
        }

        /// <summary>
        /// Gets backups for a specific table.
        /// </summary>
        public static List<BackupInfo> GetBackupsForTable(string tableName)
        {
            var manifest = LoadManifest();
            return manifest.Backups.Where(b => b.TableName == tableName).ToList();
        }

        /// <summary>
        /// Runs an automatic backup.
        /// </summary>
        public static void RunAutoBackup(ITableAsset table)
        {
            var manifest = LoadManifest();
            if (!manifest.AutoBackupEnabled)
                return;

            CreateBackup(table, isAuto: true);
        }

        /// <summary>
        /// Gets the total size of the backup folder.
        /// </summary>
        public static long GetTotalBackupSize()
        {
            if (!Directory.Exists(BackupFolder))
                return 0;

            return Directory.GetFiles(BackupFolder, "*.json")
                .Sum(f => new FileInfo(f).Length);
        }

        /// <summary>
        /// Deletes all backups.
        /// </summary>
        public static void ClearAllBackups()
        {
            if (Directory.Exists(BackupFolder))
            {
                foreach (var file in Directory.GetFiles(BackupFolder, "*.json"))
                {
                    if (Path.GetFileName(file) != ManifestFileName)
                        File.Delete(file);
                }
            }

            var manifest = LoadManifest();
            manifest.Backups.Clear();
            SaveManifest(manifest);

            Debug.Log("[BackupManager] All backups cleared");
        }

        private static void EnsureBackupFolder()
        {
            if (!Directory.Exists(BackupFolder))
                Directory.CreateDirectory(BackupFolder);
        }

        private static BackupManifest LoadManifest()
        {
            EnsureBackupFolder();

            if (File.Exists(ManifestPath))
            {
                var json = File.ReadAllText(ManifestPath);
                return JsonUtility.FromJson<BackupManifest>(json) ?? new BackupManifest();
            }

            return new BackupManifest();
        }

        private static void SaveManifest(BackupManifest manifest)
        {
            var json = JsonUtility.ToJson(manifest, true);
            File.WriteAllText(ManifestPath, json);
        }

        private static void CleanupOldBackups(BackupManifest manifest)
        {
            // Limit the number of automatic backups
            var autoBackups = manifest.Backups.Where(b => b.IsAutoBackup).ToList();
            while (autoBackups.Count > manifest.MaxBackupCount)
            {
                var oldest = autoBackups.Last();
                if (File.Exists(oldest.FilePath))
                    File.Delete(oldest.FilePath);

                manifest.Backups.Remove(oldest);
                autoBackups.Remove(oldest);
            }
        }

        private static string SerializeTable(ITableAsset table)
        {
            var records = new List<object>();
            foreach (var record in table.Records)
            {
                if (record != null)
                    records.Add(record);
            }

            var wrapper = new TableBackupWrapper
            {
                TableType = table.GetType().FullName,
                RecordType = table.RecordType.FullName,
                RecordCount = records.Count,
                RecordsJson = records.Select(r => JsonUtility.ToJson(r)).ToList()
            };

            return JsonUtility.ToJson(wrapper, true);
        }

        private static void DeserializeToTable(string json, ITableAsset table)
        {
            var wrapper = JsonUtility.FromJson<TableBackupWrapper>(json);
            var recordType = table.RecordType;

            // Clear existing records
            while (table.Count > 0)
            {
                table.RemoveRecordAt(0);
            }

            // Restore records from backup
            foreach (var recordJson in wrapper.RecordsJson)
            {
                var record = JsonUtility.FromJson(recordJson, recordType);
                table.AddRecordObject(record);
            }
        }

        [Serializable]
        private class TableBackupWrapper
        {
            public string TableType;
            public string RecordType;
            public int RecordCount;
            public List<string> RecordsJson = new();
        }
    }
}
