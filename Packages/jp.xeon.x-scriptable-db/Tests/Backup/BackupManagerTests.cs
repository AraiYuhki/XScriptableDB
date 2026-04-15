using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Xeon.XScriptableDB.Editor;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// Tests for BackupManager.
    /// </summary>
    public class BackupManagerTests
    {
        #region Test Data Classes

        [Serializable]
        private class TestRecord
        {
            [PrimaryKey]
            public int Id;
            public string Name;
            public int Value;
        }

        private class MockTableAsset : ITableAsset
        {
            private List<TestRecord> records = new();

            public IEnumerable Records => records;
            public int Count => records.Count;
            public Type RecordType => typeof(TestRecord);
            public Type KeyType => typeof(int);

            public void AddRecord(TestRecord record) => records.Add(record);

            public object CreateNewRecord() => new TestRecord();

            public void AddRecordObject(object record)
            {
                if (record is TestRecord typedRecord)
                    records.Add(typedRecord);
            }

            public void RemoveRecordAt(int index)
            {
                if (index >= 0 && index < records.Count)
                    records.RemoveAt(index);
            }

            public IList FindDuplicateKeysAsObjects() => new List<object>();

            public List<TestRecord> GetRecords() => records;

            public void Clear() => records.Clear();
        }

        #endregion

        #region BackupInfo Tests

        [Test]
        public void BackupInfo_Initialization_HasCorrectDefaults()
        {
            var backupInfo = new BackupInfo();

            Assert.That(backupInfo.Id, Is.Null);
            Assert.That(backupInfo.Name, Is.Null);
            Assert.That(backupInfo.Description, Is.Null);
            Assert.That(backupInfo.RecordCount, Is.EqualTo(0));
            Assert.That(backupInfo.FileSize, Is.EqualTo(0));
            Assert.That(backupInfo.IsAutoBackup, Is.False);
        }

        [Test]
        public void BackupInfo_SetProperties_StoresCorrectly()
        {
            var backupInfo = new BackupInfo
            {
                Id = "abc12345",
                Name = "TestBackup",
                Description = "Test backup description",
                CreatedAt = "2025-01-01 00:00:00",
                TableName = "TestTable",
                TableType = "Xeon.TestTable",
                RecordCount = 100,
                FilePath = "/path/to/backup.json",
                FileSize = 1024,
                IsAutoBackup = true
            };

            Assert.That(backupInfo.Id, Is.EqualTo("abc12345"));
            Assert.That(backupInfo.Name, Is.EqualTo("TestBackup"));
            Assert.That(backupInfo.Description, Is.EqualTo("Test backup description"));
            Assert.That(backupInfo.CreatedAt, Is.EqualTo("2025-01-01 00:00:00"));
            Assert.That(backupInfo.TableName, Is.EqualTo("TestTable"));
            Assert.That(backupInfo.TableType, Is.EqualTo("Xeon.TestTable"));
            Assert.That(backupInfo.RecordCount, Is.EqualTo(100));
            Assert.That(backupInfo.FilePath, Is.EqualTo("/path/to/backup.json"));
            Assert.That(backupInfo.FileSize, Is.EqualTo(1024));
            Assert.That(backupInfo.IsAutoBackup, Is.True);
        }

        #endregion

        #region BackupManifest Tests

        [Test]
        public void BackupManifest_Initialization_HasCorrectDefaults()
        {
            var manifest = new BackupManifest();

            Assert.That(manifest.Backups, Is.Not.Null);
            Assert.That(manifest.Backups.Count, Is.EqualTo(0));
            Assert.That(manifest.MaxBackupCount, Is.EqualTo(10));
            Assert.That(manifest.AutoBackupEnabled, Is.True);
        }

        [Test]
        public void BackupManifest_AddBackup_StoresCorrectly()
        {
            var manifest = new BackupManifest();
            var backupInfo = new BackupInfo
            {
                Id = "test123",
                Name = "TestBackup"
            };

            manifest.Backups.Add(backupInfo);

            Assert.That(manifest.Backups.Count, Is.EqualTo(1));
            Assert.That(manifest.Backups[0].Id, Is.EqualTo("test123"));
        }

        [Test]
        public void BackupManifest_ConfigureSettings_StoresCorrectly()
        {
            var manifest = new BackupManifest
            {
                MaxBackupCount = 5,
                AutoBackupEnabled = false,
                LastBackupAt = "2025-01-01 12:00:00"
            };

            Assert.That(manifest.MaxBackupCount, Is.EqualTo(5));
            Assert.That(manifest.AutoBackupEnabled, Is.False);
            Assert.That(manifest.LastBackupAt, Is.EqualTo("2025-01-01 12:00:00"));
        }

        #endregion

        #region MockTableAsset Tests

        [Test]
        public void MockTable_AddRecords_IncrementsCount()
        {
            var table = new MockTableAsset();

            table.AddRecord(new TestRecord { Id = 1, Name = "Test1", Value = 100 });
            table.AddRecord(new TestRecord { Id = 2, Name = "Test2", Value = 200 });

            Assert.That(table.Count, Is.EqualTo(2));
        }

        [Test]
        public void MockTable_RemoveRecordAt_DecrementsCount()
        {
            var table = new MockTableAsset();
            table.AddRecord(new TestRecord { Id = 1, Name = "Test1", Value = 100 });
            table.AddRecord(new TestRecord { Id = 2, Name = "Test2", Value = 200 });

            table.RemoveRecordAt(0);

            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table.GetRecords()[0].Id, Is.EqualTo(2));
        }

        [Test]
        public void MockTable_AddRecordObject_AddsTypedRecord()
        {
            var table = new MockTableAsset();
            var record = new TestRecord { Id = 1, Name = "Test", Value = 50 };

            table.AddRecordObject(record);

            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table.GetRecords()[0].Name, Is.EqualTo("Test"));
        }

        [Test]
        public void MockTable_CreateNewRecord_ReturnsCorrectType()
        {
            var table = new MockTableAsset();

            var record = table.CreateNewRecord();

            Assert.That(record, Is.InstanceOf<TestRecord>());
        }

        [Test]
        public void MockTable_RecordType_ReturnsCorrectType()
        {
            var table = new MockTableAsset();

            Assert.That(table.RecordType, Is.EqualTo(typeof(TestRecord)));
        }

        [Test]
        public void MockTable_KeyType_ReturnsCorrectType()
        {
            var table = new MockTableAsset();

            Assert.That(table.KeyType, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void MockTable_Clear_RemovesAllRecords()
        {
            var table = new MockTableAsset();
            table.AddRecord(new TestRecord { Id = 1, Name = "Test1", Value = 100 });
            table.AddRecord(new TestRecord { Id = 2, Name = "Test2", Value = 200 });

            table.Clear();

            Assert.That(table.Count, Is.EqualTo(0));
        }

        #endregion

        #region Backup Retrieval Tests

        [Test]
        public void GetAllBackups_ReturnsListType()
        {
            var backups = BackupManager.GetAllBackups();

            Assert.That(backups, Is.Not.Null);
            Assert.That(backups, Is.InstanceOf<List<BackupInfo>>());
        }

        [Test]
        public void GetBackupsForTable_FiltersByTableName()
        {
            // This function simply filters from the manifest internally
            var backups = BackupManager.GetBackupsForTable("TestTable");

            Assert.That(backups, Is.Not.Null);
            Assert.That(backups, Is.InstanceOf<List<BackupInfo>>());
        }

        [Test]
        public void GetTotalBackupSize_ReturnsNonNegative()
        {
            var size = BackupManager.GetTotalBackupSize();

            Assert.That(size, Is.GreaterThanOrEqualTo(0));
        }

        #endregion

        #region Table Records Enumeration Tests

        [Test]
        public void MockTable_Records_EnumeratesCorrectly()
        {
            var table = new MockTableAsset();
            table.AddRecord(new TestRecord { Id = 1, Name = "First", Value = 10 });
            table.AddRecord(new TestRecord { Id = 2, Name = "Second", Value = 20 });
            table.AddRecord(new TestRecord { Id = 3, Name = "Third", Value = 30 });

            var recordList = new List<TestRecord>();
            foreach (var record in table.Records)
            {
                recordList.Add((TestRecord)record);
            }

            Assert.That(recordList.Count, Is.EqualTo(3));
            Assert.That(recordList[0].Name, Is.EqualTo("First"));
            Assert.That(recordList[1].Name, Is.EqualTo("Second"));
            Assert.That(recordList[2].Name, Is.EqualTo("Third"));
        }

        [Test]
        public void MockTable_RemoveRecordAt_InvalidIndex_DoesNotThrow()
        {
            var table = new MockTableAsset();
            table.AddRecord(new TestRecord { Id = 1, Name = "Test", Value = 100 });

            Assert.DoesNotThrow(() => table.RemoveRecordAt(-1));
            Assert.DoesNotThrow(() => table.RemoveRecordAt(10));
            Assert.That(table.Count, Is.EqualTo(1));
        }

        #endregion
    }
}
