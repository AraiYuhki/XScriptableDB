using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xeon.XScriptableDB.Editor;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// MigrationExecutor のテスト。
    /// </summary>
    public class MigrationExecutorTests
    {
        #region Test Data Classes

        [Serializable]
        private class SourceRecord
        {
            [PrimaryKey]
            public int Id;
            public string Name;
            public int Value;
        }

        [Serializable]
        private class TargetRecord
        {
            [PrimaryKey]
            public int Id;
            public string Name;
            public int Value;
            public string Category;
        }

        private class MockTableAsset<T> : ITableAsset where T : new()
        {
            private List<T> records = new();

            public IEnumerable Records => records;
            public int Count => records.Count;
            public Type RecordType => typeof(T);
            public Type KeyType => typeof(int);

            public void AddRecord(T record) => records.Add(record);

            public object CreateNewRecord() => new T();

            public void AddRecordObject(object record)
            {
                if (record is T typedRecord)
                    records.Add(typedRecord);
            }

            public void RemoveRecordAt(int index)
            {
                if (index >= 0 && index < records.Count)
                    records.RemoveAt(index);
            }

            public IList FindDuplicateKeysAsObjects() => new List<object>();

            public List<T> GetRecords() => records;
        }

        #endregion

        #region GenerateMigration Tests

        [Test]
        public void GenerateMigration_FromSchemaComparison_CreatesMigration()
        {
            var comparison = SchemaComparer.Compare(typeof(SourceRecord), typeof(TargetRecord));

            var migration = MigrationExecutor.GenerateMigration(comparison, "TestMigration");

            Assert.That(migration, Is.Not.Null);
            Assert.That(migration.MigrationName, Is.EqualTo("TestMigration"));
            Assert.That(migration.Operations.Count, Is.GreaterThan(0));
        }

        [Test]
        public void GenerateMigration_DetectsAddedField()
        {
            var comparison = SchemaComparer.Compare(typeof(SourceRecord), typeof(TargetRecord));

            var migration = MigrationExecutor.GenerateMigration(comparison);

            var addOp = migration.Operations.FirstOrDefault(o =>
                o.OperationType == MigrationOperationType.AddField &&
                o.FieldName == "Category");

            Assert.That(addOp, Is.Not.Null);
        }

        [Test]
        public void GenerateMigration_SetsDefaultDescription()
        {
            var comparison = SchemaComparer.Compare(typeof(SourceRecord), typeof(TargetRecord));

            var migration = MigrationExecutor.GenerateMigration(comparison);

            Assert.That(migration.Description, Does.Contain("Auto-generated"));
        }

        [Test]
        public void GenerateMigration_SetsCreatedAt()
        {
            var comparison = SchemaComparer.Compare(typeof(SourceRecord), typeof(TargetRecord));

            var migration = MigrationExecutor.GenerateMigration(comparison);

            Assert.That(string.IsNullOrEmpty(migration.CreatedAt), Is.False);
        }

        #endregion

        #region MigrationOperation Tests

        [Test]
        public void MigrationOperation_AddField_ToString()
        {
            var op = new MigrationOperation
            {
                OperationType = MigrationOperationType.AddField,
                FieldName = "NewField",
                NewTypeName = "String",
                DefaultValue = ""
            };

            var str = op.ToString();

            Assert.That(str, Does.Contain("Add"));
            Assert.That(str, Does.Contain("NewField"));
        }

        [Test]
        public void MigrationOperation_RemoveField_ToString()
        {
            var op = new MigrationOperation
            {
                OperationType = MigrationOperationType.RemoveField,
                FieldName = "OldField"
            };

            var str = op.ToString();

            Assert.That(str, Does.Contain("Remove"));
            Assert.That(str, Does.Contain("OldField"));
        }

        [Test]
        public void MigrationOperation_RenameField_ToString()
        {
            var op = new MigrationOperation
            {
                OperationType = MigrationOperationType.RenameField,
                FieldName = "OldName",
                NewFieldName = "NewName"
            };

            var str = op.ToString();

            Assert.That(str, Does.Contain("Rename"));
            Assert.That(str, Does.Contain("OldName"));
            Assert.That(str, Does.Contain("NewName"));
        }

        [Test]
        public void MigrationOperation_ChangeFieldType_ToString()
        {
            var op = new MigrationOperation
            {
                OperationType = MigrationOperationType.ChangeFieldType,
                FieldName = "Value",
                NewTypeName = "Single"
            };

            var str = op.ToString();

            Assert.That(str, Does.Contain("ChangeType"));
            Assert.That(str, Does.Contain("Value"));
            Assert.That(str, Does.Contain("Single"));
        }

        [Test]
        public void MigrationOperation_SetDefaultValue_ToString()
        {
            var op = new MigrationOperation
            {
                OperationType = MigrationOperationType.SetDefaultValue,
                FieldName = "Status",
                DefaultValue = "Active"
            };

            var str = op.ToString();

            Assert.That(str, Does.Contain("SetDefault"));
            Assert.That(str, Does.Contain("Status"));
            Assert.That(str, Does.Contain("Active"));
        }

        [Test]
        public void MigrationOperation_TransformValue_ToString()
        {
            var op = new MigrationOperation
            {
                OperationType = MigrationOperationType.TransformValue,
                FieldName = "Name",
                TransformExpression = "UPPER"
            };

            var str = op.ToString();

            Assert.That(str, Does.Contain("Transform"));
            Assert.That(str, Does.Contain("Name"));
            Assert.That(str, Does.Contain("UPPER"));
        }

        [Test]
        public void MigrationOperation_CopyField_ToString()
        {
            var op = new MigrationOperation
            {
                OperationType = MigrationOperationType.CopyField,
                FieldName = "Source",
                NewFieldName = "Target"
            };

            var str = op.ToString();

            Assert.That(str, Does.Contain("Copy"));
            Assert.That(str, Does.Contain("Source"));
            Assert.That(str, Does.Contain("Target"));
        }

        #endregion

        #region DryRun Tests

        [Test]
        public void DryRun_WithValidOperations_ReturnsSuccess()
        {
            var table = new MockTableAsset<SourceRecord>();
            table.AddRecord(new SourceRecord { Id = 1, Name = "Test", Value = 100 });

            var migration = UnityEngine.ScriptableObject.CreateInstance<MigrationDefinition>();
            migration.Operations = new List<MigrationOperation>
            {
                new MigrationOperation
                {
                    OperationType = MigrationOperationType.SetDefaultValue,
                    FieldName = "Name",
                    DefaultValue = "Default"
                }
            };

            var result = MigrationExecutor.DryRun(migration, table);

            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void DryRun_WithInvalidField_ReturnsError()
        {
            var table = new MockTableAsset<SourceRecord>();
            table.AddRecord(new SourceRecord { Id = 1, Name = "Test", Value = 100 });

            var migration = UnityEngine.ScriptableObject.CreateInstance<MigrationDefinition>();
            migration.Operations = new List<MigrationOperation>
            {
                new MigrationOperation
                {
                    OperationType = MigrationOperationType.SetDefaultValue,
                    FieldName = "NonExistentField",
                    DefaultValue = "Default"
                }
            };

            var result = MigrationExecutor.DryRun(migration, table);

            Assert.That(result.Errors.Count, Is.GreaterThan(0));
        }

        [Test]
        public void DryRun_ReportsProcessedCount()
        {
            var table = new MockTableAsset<SourceRecord>();
            table.AddRecord(new SourceRecord { Id = 1, Name = "Test1", Value = 100 });
            table.AddRecord(new SourceRecord { Id = 2, Name = "Test2", Value = 200 });
            table.AddRecord(new SourceRecord { Id = 3, Name = "Test3", Value = 300 });

            var migration = UnityEngine.ScriptableObject.CreateInstance<MigrationDefinition>();
            migration.Operations = new List<MigrationOperation>();

            var result = MigrationExecutor.DryRun(migration, table);

            Assert.That(result.ProcessedCount, Is.EqualTo(3));
        }

        #endregion

        #region Execute Tests

        [Test]
        public void Execute_SetDefaultValue_UpdatesField()
        {
            var table = new MockTableAsset<SourceRecord>();
            table.AddRecord(new SourceRecord { Id = 1, Name = "Test", Value = 100 });

            var migration = UnityEngine.ScriptableObject.CreateInstance<MigrationDefinition>();
            migration.Operations = new List<MigrationOperation>
            {
                new MigrationOperation
                {
                    OperationType = MigrationOperationType.SetDefaultValue,
                    FieldName = "Value",
                    DefaultValue = "999"
                }
            };

            var result = MigrationExecutor.Execute(migration, table);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(table.GetRecords()[0].Value, Is.EqualTo(999));
        }

        [Test]
        public void Execute_CopyField_CopiesValue()
        {
            var table = new MockTableAsset<SourceRecord>();
            table.AddRecord(new SourceRecord { Id = 1, Name = "TestName", Value = 0 });

            var migration = UnityEngine.ScriptableObject.CreateInstance<MigrationDefinition>();
            migration.Operations = new List<MigrationOperation>
            {
                new MigrationOperation
                {
                    OperationType = MigrationOperationType.CopyField,
                    FieldName = "Id",
                    NewFieldName = "Value"
                }
            };

            var result = MigrationExecutor.Execute(migration, table);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(table.GetRecords()[0].Value, Is.EqualTo(1)); // IdがValueにコピーされる
        }

        [Test]
        public void Execute_TransformUppercase_TransformsValue()
        {
            var table = new MockTableAsset<SourceRecord>();
            table.AddRecord(new SourceRecord { Id = 1, Name = "lowercase", Value = 100 });

            var migration = UnityEngine.ScriptableObject.CreateInstance<MigrationDefinition>();
            migration.Operations = new List<MigrationOperation>
            {
                new MigrationOperation
                {
                    OperationType = MigrationOperationType.TransformValue,
                    FieldName = "Name",
                    TransformExpression = "UPPER"
                }
            };

            var result = MigrationExecutor.Execute(migration, table);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(table.GetRecords()[0].Name, Is.EqualTo("LOWERCASE"));
        }

        [Test]
        public void Execute_TransformMultiply_TransformsNumericValue()
        {
            var table = new MockTableAsset<SourceRecord>();
            table.AddRecord(new SourceRecord { Id = 1, Name = "Test", Value = 100 });

            var migration = UnityEngine.ScriptableObject.CreateInstance<MigrationDefinition>();
            migration.Operations = new List<MigrationOperation>
            {
                new MigrationOperation
                {
                    OperationType = MigrationOperationType.TransformValue,
                    FieldName = "Value",
                    TransformExpression = "*2"
                }
            };

            var result = MigrationExecutor.Execute(migration, table);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(table.GetRecords()[0].Value, Is.EqualTo(200));
        }

        [Test]
        public void Execute_ReportsExecutionTime()
        {
            var table = new MockTableAsset<SourceRecord>();
            table.AddRecord(new SourceRecord { Id = 1, Name = "Test", Value = 100 });

            var migration = UnityEngine.ScriptableObject.CreateInstance<MigrationDefinition>();
            migration.Operations = new List<MigrationOperation>();

            var result = MigrationExecutor.Execute(migration, table);

            Assert.That(result.ExecutionTime, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
        }

        #endregion

        #region MigrationResult Tests

        [Test]
        public void MigrationResult_InitialState_HasCorrectDefaults()
        {
            var result = new MigrationResult();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ProcessedCount, Is.EqualTo(0));
            Assert.That(result.FailedCount, Is.EqualTo(0));
            Assert.That(result.Errors, Is.Not.Null);
            Assert.That(result.Warnings, Is.Not.Null);
        }

        #endregion
    }
}
