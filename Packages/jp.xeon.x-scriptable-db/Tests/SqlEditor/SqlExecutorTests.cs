using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xeon.XScriptableDB.Editor;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// SqlExecutor のテスト。
    /// </summary>
    public class SqlExecutorTests
    {
        private SqlExecutor executor;
        private MockTableAsset<TestRecord> testTable;

        #region Test Data Classes

        [Serializable]
        private class TestRecord
        {
            [PrimaryKey]
            public int Id;
            public string Name;
            public int Value;
            public bool Active;
        }

        /// <summary>
        /// テスト用のモックTableAsset。
        /// </summary>
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
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            executor = new SqlExecutor();
            testTable = new MockTableAsset<TestRecord>();

            // テストデータを追加
            testTable.AddRecord(new TestRecord { Id = 1, Name = "Alice", Value = 100, Active = true });
            testTable.AddRecord(new TestRecord { Id = 2, Name = "Bob", Value = 200, Active = true });
            testTable.AddRecord(new TestRecord { Id = 3, Name = "Charlie", Value = 150, Active = false });
            testTable.AddRecord(new TestRecord { Id = 4, Name = "Diana", Value = 300, Active = true });
            testTable.AddRecord(new TestRecord { Id = 5, Name = "Eve", Value = 50, Active = false });

            executor.RegisterTable("TestTable", testTable);
        }

        #region SELECT Tests

        [Test]
        public void Execute_SelectAll_ReturnsAllRecords()
        {
            var result = executor.Execute("SELECT * FROM TestTable");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(5));
        }

        [Test]
        public void Execute_SelectWithWhereEqual_ReturnsMatchingRecords()
        {
            var result = executor.Execute("SELECT * FROM TestTable WHERE Id = 1");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(1));
            Assert.That(((TestRecord)result.Records[0]).Name, Is.EqualTo("Alice"));
        }

        [Test]
        public void Execute_SelectWithWhereGreaterThan_ReturnsMatchingRecords()
        {
            var result = executor.Execute("SELECT * FROM TestTable WHERE Value > 150");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(2)); // Bob (200), Diana (300)
        }

        [Test]
        public void Execute_SelectWithWhereLessThan_ReturnsMatchingRecords()
        {
            var result = executor.Execute("SELECT * FROM TestTable WHERE Value < 150");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(2)); // Alice (100), Eve (50)
        }

        [Test]
        public void Execute_SelectWithWhereAnd_ReturnsMatchingRecords()
        {
            var result = executor.Execute("SELECT * FROM TestTable WHERE Active = 1 AND Value > 100");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(2)); // Bob (200), Diana (300)
        }

        [Test]
        public void Execute_SelectWithWhereOr_ReturnsMatchingRecords()
        {
            var result = executor.Execute("SELECT * FROM TestTable WHERE Id = 1 OR Id = 5");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(2)); // Alice, Eve
        }

        [Test]
        public void Execute_SelectWithWhereLike_ReturnsMatchingRecords()
        {
            var result = executor.Execute("SELECT * FROM TestTable WHERE Name LIKE '%li%'");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(2)); // Alice, Charlie
        }

        [Test]
        public void Execute_SelectWithWhereIn_ReturnsMatchingRecords()
        {
            var result = executor.Execute("SELECT * FROM TestTable WHERE Id IN (1, 3, 5)");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(3));
        }

        [Test]
        public void Execute_SelectWithOrderByAsc_ReturnsOrderedRecords()
        {
            var result = executor.Execute("SELECT * FROM TestTable ORDER BY Value ASC");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(5));

            var values = result.Records.Cast<TestRecord>().Select(r => r.Value).ToList();
            Assert.That(values, Is.EqualTo(new[] { 50, 100, 150, 200, 300 }));
        }

        [Test]
        public void Execute_SelectWithOrderByDesc_ReturnsOrderedRecords()
        {
            var result = executor.Execute("SELECT * FROM TestTable ORDER BY Value DESC");

            Assert.That(result.IsSuccess, Is.True);

            var values = result.Records.Cast<TestRecord>().Select(r => r.Value).ToList();
            Assert.That(values, Is.EqualTo(new[] { 300, 200, 150, 100, 50 }));
        }

        [Test]
        public void Execute_SelectWithLimit_ReturnsLimitedRecords()
        {
            var result = executor.Execute("SELECT * FROM TestTable LIMIT 3");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(3));
        }

        [Test]
        public void Execute_SelectWithOffset_ReturnsOffsetRecords()
        {
            var result = executor.Execute("SELECT * FROM TestTable ORDER BY Id LIMIT 2 OFFSET 2");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(2));

            var ids = result.Records.Cast<TestRecord>().Select(r => r.Id).ToList();
            Assert.That(ids, Is.EqualTo(new[] { 3, 4 }));
        }

        [Test]
        public void Execute_SelectWithStringComparison_ReturnsMatchingRecords()
        {
            var result = executor.Execute("SELECT * FROM TestTable WHERE Name = 'Alice'");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(1));
        }

        #endregion

        #region UPDATE Tests

        [Test]
        public void Execute_UpdateSingleRecord_ModifiesRecord()
        {
            var result = executor.Execute("UPDATE TestTable SET Value = 999 WHERE Id = 1");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.AffectedCount, Is.EqualTo(1));

            // 確認
            var checkResult = executor.Execute("SELECT * FROM TestTable WHERE Id = 1");
            Assert.That(((TestRecord)checkResult.Records[0]).Value, Is.EqualTo(999));
        }

        [Test]
        public void Execute_UpdateMultipleRecords_ModifiesAllMatching()
        {
            var result = executor.Execute("UPDATE TestTable SET Active = 0 WHERE Value > 100");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.AffectedCount, Is.EqualTo(3)); // Bob, Charlie, Diana
        }

        [Test]
        public void Execute_UpdateWithoutWhere_ModifiesAllRecords()
        {
            var result = executor.Execute("UPDATE TestTable SET Value = 0");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.AffectedCount, Is.EqualTo(5));
        }

        [Test]
        public void Execute_UpdateMultipleColumns_ModifiesAllColumns()
        {
            var result = executor.Execute("UPDATE TestTable SET Name = 'Updated', Value = 0 WHERE Id = 1");

            Assert.That(result.IsSuccess, Is.True);

            var checkResult = executor.Execute("SELECT * FROM TestTable WHERE Id = 1");
            var record = (TestRecord)checkResult.Records[0];
            Assert.That(record.Name, Is.EqualTo("Updated"));
            Assert.That(record.Value, Is.EqualTo(0));
        }

        #endregion

        #region DELETE Tests

        [Test]
        public void Execute_DeleteSingleRecord_RemovesRecord()
        {
            var result = executor.Execute("DELETE FROM TestTable WHERE Id = 1");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.AffectedCount, Is.EqualTo(1));
            Assert.That(testTable.Count, Is.EqualTo(4));
        }

        [Test]
        public void Execute_DeleteMultipleRecords_RemovesAllMatching()
        {
            var result = executor.Execute("DELETE FROM TestTable WHERE Active = 0");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.AffectedCount, Is.EqualTo(2)); // Charlie, Eve
            Assert.That(testTable.Count, Is.EqualTo(3));
        }

        [Test]
        public void Execute_DeleteWithoutWhere_RemovesAllRecords()
        {
            var result = executor.Execute("DELETE FROM TestTable");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.AffectedCount, Is.EqualTo(5));
            Assert.That(testTable.Count, Is.EqualTo(0));
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Execute_TableNotFound_ReturnsError()
        {
            var result = executor.Execute("SELECT * FROM NonExistentTable");

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("Table not found"));
        }

        [Test]
        public void Execute_InvalidSql_ReturnsError()
        {
            var result = executor.Execute("NOT A VALID SQL");

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("Parse error"));
        }

        [Test]
        public void Execute_EmptySql_ReturnsError()
        {
            var result = executor.Execute("");

            Assert.That(result.IsSuccess, Is.False);
        }

        #endregion

        #region Table Registration Tests

        [Test]
        public void RegisterTable_AddsTableToExecutor()
        {
            var newTable = new MockTableAsset<TestRecord>();
            executor.RegisterTable("NewTable", newTable);

            Assert.That(executor.TableNames, Does.Contain("NewTable"));
        }

        [Test]
        public void GetTable_ReturnsRegisteredTable()
        {
            var table = executor.GetTable("TestTable");

            Assert.That(table, Is.EqualTo(testTable));
        }

        [Test]
        public void GetTable_TableNotFound_ReturnsNull()
        {
            var table = executor.GetTable("NonExistent");

            Assert.That(table, Is.Null);
        }

        [Test]
        public void TableNames_IsCaseInsensitive()
        {
            var result1 = executor.Execute("SELECT * FROM TESTTABLE");
            var result2 = executor.Execute("SELECT * FROM testtable");
            var result3 = executor.Execute("SELECT * FROM TestTable");

            Assert.That(result1.IsSuccess, Is.True);
            Assert.That(result2.IsSuccess, Is.True);
            Assert.That(result3.IsSuccess, Is.True);
        }

        #endregion

        #region ExecutionTime Tests

        [Test]
        public void Execute_RecordsExecutionTime()
        {
            var result = executor.Execute("SELECT * FROM TestTable");

            Assert.That(result.ExecutionTimeMs, Is.GreaterThanOrEqualTo(0));
        }

        #endregion
    }
}
