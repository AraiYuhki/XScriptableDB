using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xeon.XScriptableDB.Editor;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// SqlExecutorのテスト。
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
            public int CategoryId;
        }

        [Serializable]
        private class CategoryRecord
        {
            [PrimaryKey]
            public int Id;
            public string CategoryName;
        }

        /// <summary>
        /// テスト用のMock TableAsset。
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

        private MockTableAsset<CategoryRecord> categoryTable;

        [SetUp]
        public void SetUp()
        {
            executor = new SqlExecutor();
            testTable = new MockTableAsset<TestRecord>();
            categoryTable = new MockTableAsset<CategoryRecord>();

            // テストデータを追加
            testTable.AddRecord(new TestRecord { Id = 1, Name = "Alice", Value = 100, Active = true, CategoryId = 1 });
            testTable.AddRecord(new TestRecord { Id = 2, Name = "Bob", Value = 200, Active = true, CategoryId = 1 });
            testTable.AddRecord(new TestRecord { Id = 3, Name = "Charlie", Value = 150, Active = false, CategoryId = 2 });
            testTable.AddRecord(new TestRecord { Id = 4, Name = "Diana", Value = 300, Active = true, CategoryId = 2 });
            testTable.AddRecord(new TestRecord { Id = 5, Name = "Eve", Value = 50, Active = false, CategoryId = 3 });

            // カテゴリデータを追加
            categoryTable.AddRecord(new CategoryRecord { Id = 1, CategoryName = "Electronics" });
            categoryTable.AddRecord(new CategoryRecord { Id = 2, CategoryName = "Clothing" });
            categoryTable.AddRecord(new CategoryRecord { Id = 3, CategoryName = "Books" });

            executor.RegisterTable("TestTable", testTable);
            executor.RegisterTable("Categories", categoryTable);
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

        #region JOIN Tests

        [Test]
        public void Execute_InnerJoin_ReturnsMatchingRecords()
        {
            var result = executor.Execute("SELECT * FROM TestTable t INNER JOIN Categories c ON t.CategoryId = c.Id");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(5)); // すべてのレコードがカテゴリに一致します
        }

        [Test]
        public void Execute_LeftJoin_ReturnsAllLeftRecords()
        {
            // 存在しないカテゴリ4に属するレコードを追加します
            testTable.AddRecord(new TestRecord { Id = 6, Name = "Frank", Value = 400, Active = true, CategoryId = 99 });

            var result = executor.Execute("SELECT * FROM TestTable t LEFT JOIN Categories c ON t.CategoryId = c.Id");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(6)); // Frankも含まれます
        }

        [Test]
        public void Execute_CrossJoin_ReturnsCartesianProduct()
        {
            var result = executor.Execute("SELECT * FROM TestTable CROSS JOIN Categories");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(15)); // 5 * 3 = 15
        }

        #endregion

        #region Aggregate Function Tests

        [Test]
        public void Execute_CountStar_ReturnsRecordCount()
        {
            var result = executor.Execute("SELECT COUNT(*) FROM TestTable");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(1));

            var row = result.Records[0] as ResultRow;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.Values.Values.First(), Is.EqualTo(5));
        }

        [Test]
        public void Execute_SumFunction_ReturnsSumOfValues()
        {
            var result = executor.Execute("SELECT SUM(Value) FROM TestTable");

            Assert.That(result.IsSuccess, Is.True);
            var row = result.Records[0] as ResultRow;
            Assert.That(Convert.ToDouble(row.Values.Values.First()), Is.EqualTo(800.0)); // 100+200+150+300+50
        }

        [Test]
        public void Execute_AvgFunction_ReturnsAverageOfValues()
        {
            var result = executor.Execute("SELECT AVG(Value) FROM TestTable");

            Assert.That(result.IsSuccess, Is.True);
            var row = result.Records[0] as ResultRow;
            Assert.That(Convert.ToDouble(row.Values.Values.First()), Is.EqualTo(160.0)); // 800/5
        }

        [Test]
        public void Execute_MinFunction_ReturnsMinValue()
        {
            var result = executor.Execute("SELECT MIN(Value) FROM TestTable");

            Assert.That(result.IsSuccess, Is.True);
            var row = result.Records[0] as ResultRow;
            Assert.That(Convert.ToInt32(row.Values.Values.First()), Is.EqualTo(50));
        }

        [Test]
        public void Execute_MaxFunction_ReturnsMaxValue()
        {
            var result = executor.Execute("SELECT MAX(Value) FROM TestTable");

            Assert.That(result.IsSuccess, Is.True);
            var row = result.Records[0] as ResultRow;
            Assert.That(Convert.ToInt32(row.Values.Values.First()), Is.EqualTo(300));
        }

        [Test]
        public void Execute_MultipleAggregates_ReturnsAllResults()
        {
            var result = executor.Execute("SELECT COUNT(*), SUM(Value), AVG(Value), MIN(Value), MAX(Value) FROM TestTable");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(1));
            var row = result.Records[0] as ResultRow;
            Assert.That(row.Values.Count, Is.EqualTo(5));
        }

        #endregion

        #region GROUP BY Tests

        [Test]
        public void Execute_GroupByWithCount_ReturnsGroupedCounts()
        {
            var result = executor.Execute("SELECT CategoryId, COUNT(*) FROM TestTable GROUP BY CategoryId");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(3)); // 3カテゴリ
        }

        [Test]
        public void Execute_GroupByWithSum_ReturnsGroupedSums()
        {
            var result = executor.Execute("SELECT CategoryId, SUM(Value) FROM TestTable GROUP BY CategoryId");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(3));
        }

        [Test]
        public void Execute_GroupByWithHaving_FiltersGroups()
        {
            var result = executor.Execute("SELECT CategoryId, COUNT(*) FROM TestTable GROUP BY CategoryId HAVING COUNT(*) > 1");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(2)); // CategoryId 1と2のみ（それぞれ2レコード）
        }

        #endregion

        #region DISTINCT Tests

        [Test]
        public void Execute_SelectDistinct_RemovesDuplicates()
        {
            var result = executor.Execute("SELECT DISTINCT CategoryId FROM TestTable");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(3)); // 3つのユニークなカテゴリ
        }

        [Test]
        public void Execute_SelectDistinct_Active_ReturnsUniqueValues()
        {
            var result = executor.Execute("SELECT DISTINCT Active FROM TestTable");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(2)); // true and false
        }

        #endregion

        #region Arithmetic Expression Tests

        [Test]
        public void Execute_ArithmeticInWhere_EvaluatesCorrectly()
        {
            var result = executor.Execute("SELECT * FROM TestTable WHERE Value * 2 > 300");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(2)); // Bob (400), Diana (600)
        }

        #endregion

        #region String Function Tests

        [Test]
        public void Execute_WhereWithUpperFunction_MatchesCaseInsensitive()
        {
            var result = executor.Execute("SELECT * FROM TestTable WHERE UPPER(Name) = 'ALICE'");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(1));
        }

        [Test]
        public void Execute_WhereWithLowerFunction_MatchesCaseInsensitive()
        {
            var result = executor.Execute("SELECT * FROM TestTable WHERE LOWER(Name) = 'bob'");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(1));
        }

        [Test]
        public void Execute_SelectWithUpperFunction_ReturnsUppercaseValues()
        {
            var result = executor.Execute("SELECT UPPER(Name) AS UpperName FROM TestTable WHERE Id = 1");

            Assert.That(result.IsSuccess, Is.True);
            var row = result.Records[0] as ResultRow;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.Values["UpperName"], Is.EqualTo("ALICE"));
        }

        [Test]
        public void Execute_SelectWithLowerFunction_ReturnsLowercaseValues()
        {
            var result = executor.Execute("SELECT LOWER(Name) AS LowerName FROM TestTable WHERE Id = 1");

            Assert.That(result.IsSuccess, Is.True);
            var row = result.Records[0] as ResultRow;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.Values["LowerName"], Is.EqualTo("alice"));
        }

        [Test]
        public void Execute_SelectWithLengthFunction_ReturnsStringLength()
        {
            var result = executor.Execute("SELECT LENGTH(Name) AS NameLength FROM TestTable WHERE Id = 1");

            Assert.That(result.IsSuccess, Is.True);
            var row = result.Records[0] as ResultRow;
            Assert.That(row, Is.Not.Null);
            Assert.That(Convert.ToInt32(row.Values["NameLength"]), Is.EqualTo(5)); // "Alice" = 5 characters
        }

        [Test]
        public void Execute_SelectWithTrimFunction_TrimsWhitespace()
        {
            var result = executor.Execute("SELECT TRIM(Name) AS TrimmedName FROM TestTable WHERE Id = 1");

            Assert.That(result.IsSuccess, Is.True);
            var row = result.Records[0] as ResultRow;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.Values["TrimmedName"], Is.EqualTo("Alice"));
        }

        #endregion

        #region CASE Expression Tests

        [Test]
        public void Execute_CaseExpression_ReturnsCorrectValue()
        {
            var result = executor.Execute("SELECT Id, CASE WHEN Value > 200 THEN 'High' ELSE 'Low' END AS Level FROM TestTable");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(5));

            // Diana (Value=300) は High
            var dianaRow = result.Records.Cast<ResultRow>().FirstOrDefault(r => Convert.ToInt32(r.Values["Id"]) == 4);
            Assert.That(dianaRow, Is.Not.Null);
            Assert.That(dianaRow.Values["Level"], Is.EqualTo("High"));

            // Alice (Value=100) は Low
            var aliceRow = result.Records.Cast<ResultRow>().FirstOrDefault(r => Convert.ToInt32(r.Values["Id"]) == 1);
            Assert.That(aliceRow, Is.Not.Null);
            Assert.That(aliceRow.Values["Level"], Is.EqualTo("Low"));
        }

        [Test]
        public void Execute_CaseExpressionMultipleWhen_ReturnsCorrectValue()
        {
            var result = executor.Execute("SELECT Id, CASE WHEN Value < 100 THEN 'Low' WHEN Value < 200 THEN 'Medium' ELSE 'High' END AS Level FROM TestTable");

            Assert.That(result.IsSuccess, Is.True);

            // Eve (Value=50) は Low
            var eveRow = result.Records.Cast<ResultRow>().FirstOrDefault(r => Convert.ToInt32(r.Values["Id"]) == 5);
            Assert.That(eveRow.Values["Level"], Is.EqualTo("Low"));

            // Alice (Value=100), Charlie (Value=150) は Medium
            var aliceRow = result.Records.Cast<ResultRow>().FirstOrDefault(r => Convert.ToInt32(r.Values["Id"]) == 1);
            Assert.That(aliceRow.Values["Level"], Is.EqualTo("Medium"));

            // Bob (Value=200), Diana (Value=300) は High
            var bobRow = result.Records.Cast<ResultRow>().FirstOrDefault(r => Convert.ToInt32(r.Values["Id"]) == 2);
            Assert.That(bobRow.Values["Level"], Is.EqualTo("High"));
        }

        [Test]
        public void Execute_CaseExpressionWithBoolean_ReturnsCorrectValue()
        {
            var result = executor.Execute("SELECT Id, CASE WHEN Active = 1 THEN 'Active' ELSE 'Inactive' END AS Status FROM TestTable");

            Assert.That(result.IsSuccess, Is.True);

            // Alice (Active=true) は Active
            var aliceRow = result.Records.Cast<ResultRow>().FirstOrDefault(r => Convert.ToInt32(r.Values["Id"]) == 1);
            Assert.That(aliceRow.Values["Status"], Is.EqualTo("Active"));

            // Charlie (Active=false) は Inactive
            var charlieRow = result.Records.Cast<ResultRow>().FirstOrDefault(r => Convert.ToInt32(r.Values["Id"]) == 3);
            Assert.That(charlieRow.Values["Status"], Is.EqualTo("Inactive"));
        }

        #endregion

        #region Additional JOIN Tests

        [Test]
        public void Execute_RightJoin_ReturnsAllRightRecords()
        {
            // （どのテストレコードにも属さない）カテゴリ4を追加します
            categoryTable.AddRecord(new CategoryRecord { Id = 4, CategoryName = "Sports" });

            var result = executor.Execute("SELECT * FROM TestTable t RIGHT JOIN Categories c ON t.CategoryId = c.Id");

            Assert.That(result.IsSuccess, Is.True);
            // カテゴリ1、2、3には一致するレコードがあります。カテゴリ4はNULLです
            Assert.That(result.Records.Count, Is.EqualTo(6)); // 5 + 1 (nullのSports)
        }

        [Test]
        public void Execute_JoinWithWhereClause_FiltersResults()
        {
            var result = executor.Execute("SELECT * FROM TestTable t INNER JOIN Categories c ON t.CategoryId = c.Id WHERE t.Value > 150");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(2)); // Bob (200), Diana (300)
        }

        [Test]
        public void Execute_JoinWithOrderBy_OrdersResults()
        {
            var result = executor.Execute("SELECT * FROM TestTable t INNER JOIN Categories c ON t.CategoryId = c.Id ORDER BY t.Value DESC");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(5));
        }

        #endregion

        #region SELECT Arithmetic Expression Tests

        [Test]
        public void Execute_SelectWithAddition_ReturnsCalculatedValue()
        {
            var result = executor.Execute("SELECT Id, Value + 100 AS IncreasedValue FROM TestTable WHERE Id = 1");

            Assert.That(result.IsSuccess, Is.True);
            var row = result.Records[0] as ResultRow;
            Assert.That(row, Is.Not.Null);
            Assert.That(Convert.ToInt32(row.Values["IncreasedValue"]), Is.EqualTo(200)); // 100 + 100
        }

        [Test]
        public void Execute_SelectWithSubtraction_ReturnsCalculatedValue()
        {
            var result = executor.Execute("SELECT Id, Value - 50 AS DecreasedValue FROM TestTable WHERE Id = 1");

            Assert.That(result.IsSuccess, Is.True);
            var row = result.Records[0] as ResultRow;
            Assert.That(Convert.ToInt32(row.Values["DecreasedValue"]), Is.EqualTo(50)); // 100 - 50
        }

        [Test]
        public void Execute_SelectWithMultiplication_ReturnsCalculatedValue()
        {
            var result = executor.Execute("SELECT Id, Value * 2 AS DoubledValue FROM TestTable WHERE Id = 1");

            Assert.That(result.IsSuccess, Is.True);
            var row = result.Records[0] as ResultRow;
            Assert.That(Convert.ToInt32(row.Values["DoubledValue"]), Is.EqualTo(200)); // 100 * 2
        }

        [Test]
        public void Execute_SelectWithDivision_ReturnsCalculatedValue()
        {
            var result = executor.Execute("SELECT Id, Value / 2 AS HalvedValue FROM TestTable WHERE Id = 2");

            Assert.That(result.IsSuccess, Is.True);
            var row = result.Records[0] as ResultRow;
            Assert.That(Convert.ToDouble(row.Values["HalvedValue"]), Is.EqualTo(100.0)); // 200 / 2
        }

        [Test]
        public void Execute_SelectWithComplexArithmetic_ReturnsCalculatedValue()
        {
            var result = executor.Execute("SELECT Id, Value * 2 + 50 AS CalculatedValue FROM TestTable WHERE Id = 1");

            Assert.That(result.IsSuccess, Is.True);
            var row = result.Records[0] as ResultRow;
            Assert.That(Convert.ToInt32(row.Values["CalculatedValue"]), Is.EqualTo(250)); // 100 * 2 + 50
        }

        [Test]
        public void Execute_SelectWithColumnArithmetic_ReturnsCalculatedValue()
        {
            var result = executor.Execute("SELECT Id, Id + Value AS Combined FROM TestTable WHERE Id = 1");

            Assert.That(result.IsSuccess, Is.True);
            var row = result.Records[0] as ResultRow;
            Assert.That(Convert.ToInt32(row.Values["Combined"]), Is.EqualTo(101)); // 1 + 100
        }

        #endregion

        #region Subquery Tests

        [Test]
        public void Execute_SubqueryInWhere_FiltersCorrectly()
        {
            // 平均より大きいValueを持つレコードを取得するサブクエリ
            var result = executor.Execute("SELECT * FROM TestTable WHERE Value > (SELECT AVG(Value) FROM TestTable)");

            Assert.That(result.IsSuccess, Is.True);
            // 平均は160です。Bob(200)とDiana(300)の方が大きいです
            Assert.That(result.Records.Count, Is.EqualTo(2));
        }

        [Test]
        public void Execute_SubqueryWithIn_FiltersCorrectly()
        {
            var result = executor.Execute("SELECT * FROM TestTable WHERE CategoryId IN (SELECT Id FROM Categories WHERE Id < 3)");

            Assert.That(result.IsSuccess, Is.True);
            // CategoryId 1、2のレコード: Alice, Bob, Charlie, Diana
            Assert.That(result.Records.Count, Is.EqualTo(4));
        }

        #endregion

        #region Aggregate with WHERE Tests

        [Test]
        public void Execute_CountWithWhere_CountsFilteredRecords()
        {
            var result = executor.Execute("SELECT COUNT(*) FROM TestTable WHERE Active = 1");

            Assert.That(result.IsSuccess, Is.True);
            var row = result.Records[0] as ResultRow;
            Assert.That(row.Values.Values.First(), Is.EqualTo(3)); // Alice, Bob, Dianaはアクティブです
        }

        [Test]
        public void Execute_SumWithWhere_SumsFilteredValues()
        {
            var result = executor.Execute("SELECT SUM(Value) FROM TestTable WHERE CategoryId = 1");

            Assert.That(result.IsSuccess, Is.True);
            var row = result.Records[0] as ResultRow;
            Assert.That(Convert.ToDouble(row.Values.Values.First()), Is.EqualTo(300.0)); // Alice(100) + Bob(200) = 300
        }

        [Test]
        public void Execute_AvgWithWhere_AveragesFilteredValues()
        {
            var result = executor.Execute("SELECT AVG(Value) FROM TestTable WHERE CategoryId = 2");

            Assert.That(result.IsSuccess, Is.True);
            var row = result.Records[0] as ResultRow;
            Assert.That(Convert.ToDouble(row.Values.Values.First()), Is.EqualTo(225.0)); // (150 + 300) / 2 = 225
        }

        #endregion

        #region Combined Feature Tests

        [Test]
        public void Execute_JoinWithGroupBy_GroupsJoinedData()
        {
            var result = executor.Execute("SELECT c.CategoryName, COUNT(*) AS ItemCount FROM TestTable t INNER JOIN Categories c ON t.CategoryId = c.Id GROUP BY c.CategoryName");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(3)); // 3カテゴリ
        }

        [Test]
        public void Execute_JoinWithGroupByAndHaving_FiltersGroupedData()
        {
            var result = executor.Execute("SELECT c.CategoryName, COUNT(*) AS ItemCount FROM TestTable t INNER JOIN Categories c ON t.CategoryId = c.Id GROUP BY c.CategoryName HAVING COUNT(*) > 1");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(2)); // Electronics(2)とClothing(2)
        }

        [Test]
        public void Execute_DistinctWithOrderBy_ReturnsOrderedUniqueValues()
        {
            var result = executor.Execute("SELECT DISTINCT CategoryId FROM TestTable ORDER BY CategoryId ASC");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Count, Is.EqualTo(3));

            var categoryIds = result.Records.Cast<ResultRow>().Select(r => Convert.ToInt32(r.Values["CategoryId"])).ToList();
            Assert.That(categoryIds, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        #endregion
    }
}
