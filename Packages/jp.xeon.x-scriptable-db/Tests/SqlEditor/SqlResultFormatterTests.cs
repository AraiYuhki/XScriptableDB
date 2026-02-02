using NUnit.Framework;
using System;
using System.Collections.Generic;
using Xeon.XScriptableDB.Editor;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// SqlResultFormatter のテスト。
    /// </summary>
    public class SqlResultFormatterTests
    {
        #region Test Data Classes

        [Serializable]
        private class TestRecord
        {
            public int Id;
            public string Name;
            public int Value;
            public bool Active;
        }

        [Serializable]
        private class CategoryRecord
        {
            public int Id;
            public string CategoryName;
        }

        #endregion

        #region GetColumnNamesFromRecord Tests

        [Test]
        public void GetColumnNamesFromRecord_NullRecord_ReturnsEmptyList()
        {
            var result = SqlResultFormatter.GetColumnNamesFromRecord(null);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetColumnNamesFromRecord_ResultRow_ReturnsValuesKeys()
        {
            var row = new ResultRow();
            row.Values["Id"] = 1;
            row.Values["Name"] = "Test";
            row.Values["COUNT(*)"] = 5;

            var result = SqlResultFormatter.GetColumnNamesFromRecord(row);

            Assert.That(result, Does.Contain("Id"));
            Assert.That(result, Does.Contain("Name"));
            Assert.That(result, Does.Contain("COUNT(*)"));
            Assert.That(result.Count, Is.EqualTo(3));
        }

        [Test]
        public void GetColumnNamesFromRecord_ResultRowEmpty_ReturnsEmptyList()
        {
            var row = new ResultRow();

            var result = SqlResultFormatter.GetColumnNamesFromRecord(row);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetColumnNamesFromRecord_RegularRecord_ReturnsFieldNames()
        {
            var record = new TestRecord { Id = 1, Name = "Test", Value = 100, Active = true };

            var result = SqlResultFormatter.GetColumnNamesFromRecord(record);

            Assert.That(result, Does.Contain("Id"));
            Assert.That(result, Does.Contain("Name"));
            Assert.That(result, Does.Contain("Value"));
            Assert.That(result, Does.Contain("Active"));
        }

        [Test]
        public void GetColumnNamesFromRecord_JoinedRecord_ReturnsAliasedFieldNames()
        {
            var testRecord = new TestRecord { Id = 1, Name = "Test" };
            var categoryRecord = new CategoryRecord { Id = 1, CategoryName = "Category1" };

            var joinedRecord = new JoinedRecord
            {
                TableRecords = { ["t"] = testRecord, ["c"] = categoryRecord },
                TableTypes = { ["t"] = typeof(TestRecord), ["c"] = typeof(CategoryRecord) }
            };

            var result = SqlResultFormatter.GetColumnNamesFromRecord(joinedRecord);

            Assert.That(result, Does.Contain("t.Id"));
            Assert.That(result, Does.Contain("t.Name"));
            Assert.That(result, Does.Contain("c.Id"));
            Assert.That(result, Does.Contain("c.CategoryName"));
        }

        [Test]
        public void GetColumnNamesFromRecord_JoinedRecordWithNullTable_SkipsNullTable()
        {
            var testRecord = new TestRecord { Id = 1, Name = "Test" };

            var joinedRecord = new JoinedRecord
            {
                TableRecords = { ["t"] = testRecord, ["c"] = null },
                TableTypes = { ["t"] = typeof(TestRecord), ["c"] = typeof(CategoryRecord) }
            };

            var result = SqlResultFormatter.GetColumnNamesFromRecord(joinedRecord);

            Assert.That(result, Does.Contain("t.Id"));
            Assert.That(result, Does.Contain("t.Name"));
            Assert.That(result, Does.Not.Contain("c.Id"));
            Assert.That(result, Does.Not.Contain("c.CategoryName"));
        }

        #endregion

        #region GetFieldValue Tests

        [Test]
        public void GetFieldValue_NullRecord_ReturnsNull()
        {
            var result = SqlResultFormatter.GetFieldValue(null, typeof(TestRecord), "Id");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetFieldValue_ResultRow_ReturnsValueFromDictionary()
        {
            var row = new ResultRow();
            row.Values["Id"] = 42;
            row.Values["Name"] = "TestName";

            var idResult = SqlResultFormatter.GetFieldValue(row, null, "Id");
            var nameResult = SqlResultFormatter.GetFieldValue(row, null, "Name");

            Assert.That(idResult, Is.EqualTo(42));
            Assert.That(nameResult, Is.EqualTo("TestName"));
        }

        [Test]
        public void GetFieldValue_ResultRow_NonExistentKey_ReturnsNull()
        {
            var row = new ResultRow();
            row.Values["Id"] = 42;

            var result = SqlResultFormatter.GetFieldValue(row, null, "NonExistent");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetFieldValue_ResultRow_AggregateColumnName_ReturnsValue()
        {
            var row = new ResultRow();
            row.Values["COUNT(*)"] = 10;
            row.Values["SUM(Value)"] = 500.0;

            var countResult = SqlResultFormatter.GetFieldValue(row, null, "COUNT(*)");
            var sumResult = SqlResultFormatter.GetFieldValue(row, null, "SUM(Value)");

            Assert.That(countResult, Is.EqualTo(10));
            Assert.That(sumResult, Is.EqualTo(500.0));
        }

        [Test]
        public void GetFieldValue_RegularRecord_ReturnsFieldValue()
        {
            var record = new TestRecord { Id = 1, Name = "Test", Value = 100, Active = true };

            var idResult = SqlResultFormatter.GetFieldValue(record, typeof(TestRecord), "Id");
            var nameResult = SqlResultFormatter.GetFieldValue(record, typeof(TestRecord), "Name");
            var activeResult = SqlResultFormatter.GetFieldValue(record, typeof(TestRecord), "Active");

            Assert.That(idResult, Is.EqualTo(1));
            Assert.That(nameResult, Is.EqualTo("Test"));
            Assert.That(activeResult, Is.EqualTo(true));
        }

        [Test]
        public void GetFieldValue_RegularRecord_CaseInsensitive_ReturnsFieldValue()
        {
            var record = new TestRecord { Id = 1, Name = "Test" };

            var result = SqlResultFormatter.GetFieldValue(record, typeof(TestRecord), "id");

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void GetFieldValue_JoinedRecord_WithAlias_ReturnsValue()
        {
            var testRecord = new TestRecord { Id = 1, Name = "Test", Value = 100 };
            var categoryRecord = new CategoryRecord { Id = 2, CategoryName = "Category1" };

            var joinedRecord = new JoinedRecord
            {
                TableRecords = { ["t"] = testRecord, ["c"] = categoryRecord },
                TableTypes = { ["t"] = typeof(TestRecord), ["c"] = typeof(CategoryRecord) }
            };

            var tIdResult = SqlResultFormatter.GetFieldValue(joinedRecord, null, "t.Id");
            var tNameResult = SqlResultFormatter.GetFieldValue(joinedRecord, null, "t.Name");
            var cIdResult = SqlResultFormatter.GetFieldValue(joinedRecord, null, "c.Id");
            var cNameResult = SqlResultFormatter.GetFieldValue(joinedRecord, null, "c.CategoryName");

            Assert.That(tIdResult, Is.EqualTo(1));
            Assert.That(tNameResult, Is.EqualTo("Test"));
            Assert.That(cIdResult, Is.EqualTo(2));
            Assert.That(cNameResult, Is.EqualTo("Category1"));
        }

        [Test]
        public void GetFieldValue_JoinedRecord_WithoutAlias_SearchesAllTables()
        {
            var testRecord = new TestRecord { Id = 1, Name = "Test" };
            var categoryRecord = new CategoryRecord { Id = 2, CategoryName = "Category1" };

            var joinedRecord = new JoinedRecord
            {
                TableRecords = { ["t"] = testRecord, ["c"] = categoryRecord },
                TableTypes = { ["t"] = typeof(TestRecord), ["c"] = typeof(CategoryRecord) }
            };

            // CategoryNameはcategoryRecordにのみ存在
            var categoryNameResult = SqlResultFormatter.GetFieldValue(joinedRecord, null, "CategoryName");

            Assert.That(categoryNameResult, Is.EqualTo("Category1"));
        }

        [Test]
        public void GetFieldValue_JoinedRecord_NullTableRecord_ReturnsNull()
        {
            var testRecord = new TestRecord { Id = 1, Name = "Test" };

            var joinedRecord = new JoinedRecord
            {
                TableRecords = { ["t"] = testRecord, ["c"] = null },
                TableTypes = { ["t"] = typeof(TestRecord), ["c"] = typeof(CategoryRecord) }
            };

            var result = SqlResultFormatter.GetFieldValue(joinedRecord, null, "c.CategoryName");

            Assert.That(result, Is.Null);
        }

        #endregion

        #region FormatValue Tests

        [Test]
        public void FormatValue_Null_ReturnsNullString()
        {
            var result = SqlResultFormatter.FormatValue(null);

            Assert.That(result, Is.EqualTo("(null)"));
        }

        [Test]
        public void FormatValue_String_ReturnsAsIs()
        {
            var result = SqlResultFormatter.FormatValue("Hello World");

            Assert.That(result, Is.EqualTo("Hello World"));
        }

        [Test]
        public void FormatValue_EmptyString_ReturnsEmpty()
        {
            var result = SqlResultFormatter.FormatValue("");

            Assert.That(result, Is.EqualTo(""));
        }

        [Test]
        public void FormatValue_BoolTrue_ReturnsTrue()
        {
            var result = SqlResultFormatter.FormatValue(true);

            Assert.That(result, Is.EqualTo("true"));
        }

        [Test]
        public void FormatValue_BoolFalse_ReturnsFalse()
        {
            var result = SqlResultFormatter.FormatValue(false);

            Assert.That(result, Is.EqualTo("false"));
        }

        [Test]
        public void FormatValue_Integer_ReturnsStringRepresentation()
        {
            var result = SqlResultFormatter.FormatValue(42);

            Assert.That(result, Is.EqualTo("42"));
        }

        [Test]
        public void FormatValue_Float_ReturnsFormattedWithTwoDecimals()
        {
            var result = SqlResultFormatter.FormatValue(3.14159f);

            Assert.That(result, Is.EqualTo("3.14"));
        }

        [Test]
        public void FormatValue_Double_ReturnsFormattedWithTwoDecimals()
        {
            var result = SqlResultFormatter.FormatValue(2.71828);

            Assert.That(result, Is.EqualTo("2.72"));
        }

        [Test]
        public void FormatValue_DateTime_ReturnsFormattedDateTime()
        {
            var dt = new DateTime(2024, 1, 15, 10, 30, 45);

            var result = SqlResultFormatter.FormatValue(dt);

            Assert.That(result, Is.EqualTo("2024-01-15 10:30:45"));
        }

        [Test]
        public void FormatValue_CustomObject_ReturnsToString()
        {
            var obj = new TestRecord { Id = 1, Name = "Test" };

            var result = SqlResultFormatter.FormatValue(obj);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("TestRecord"));
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Integration_ResultRowFromGroupBy_CorrectlyFormatsAllValues()
        {
            // GROUP BY の結果を模擬
            var row = new ResultRow();
            row.Values["CategoryId"] = 1;
            row.Values["COUNT(*)"] = 5;
            row.Values["SUM(Value)"] = 500.0;
            row.Values["AVG(Value)"] = 100.0;

            // カラム名の取得
            var columnNames = SqlResultFormatter.GetColumnNamesFromRecord(row);
            Assert.That(columnNames.Count, Is.EqualTo(4));

            // 値の取得とフォーマット
            foreach (var colName in columnNames)
            {
                var value = SqlResultFormatter.GetFieldValue(row, null, colName);
                var formatted = SqlResultFormatter.FormatValue(value);
                Assert.That(formatted, Is.Not.EqualTo("(null)"));
            }
        }

        [Test]
        public void Integration_JoinedRecordFromJoin_CorrectlyFormatsAllValues()
        {
            // JOIN の結果を模擬
            var testRecord = new TestRecord { Id = 1, Name = "Item1", Value = 100, Active = true };
            var categoryRecord = new CategoryRecord { Id = 1, CategoryName = "Electronics" };

            var joinedRecord = new JoinedRecord
            {
                TableRecords = { ["t"] = testRecord, ["c"] = categoryRecord },
                TableTypes = { ["t"] = typeof(TestRecord), ["c"] = typeof(CategoryRecord) }
            };

            // カラム名の取得
            var columnNames = SqlResultFormatter.GetColumnNamesFromRecord(joinedRecord);
            Assert.That(columnNames.Count, Is.GreaterThan(0));

            // 全てのカラムの値が取得できることを確認
            foreach (var colName in columnNames)
            {
                var value = SqlResultFormatter.GetFieldValue(joinedRecord, null, colName);
                // nullでないことを確認（JOINでnullテーブルがない場合）
                Assert.That(value, Is.Not.Null, $"Column {colName} should not be null");
            }
        }

        [Test]
        public void Integration_LeftJoinWithNullTable_CorrectlyHandlesNullValues()
        {
            // LEFT JOIN でマッチしなかった場合を模擬
            var testRecord = new TestRecord { Id = 1, Name = "Item1", Value = 100, Active = true };

            var joinedRecord = new JoinedRecord
            {
                TableRecords = { ["t"] = testRecord, ["c"] = null },
                TableTypes = { ["t"] = typeof(TestRecord), ["c"] = typeof(CategoryRecord) }
            };

            // テーブルtのカラムは取得できる
            var tIdValue = SqlResultFormatter.GetFieldValue(joinedRecord, null, "t.Id");
            Assert.That(tIdValue, Is.EqualTo(1));

            // テーブルcのカラムはnull
            var cIdValue = SqlResultFormatter.GetFieldValue(joinedRecord, null, "c.Id");
            Assert.That(cIdValue, Is.Null);

            // nullのフォーマット
            var formatted = SqlResultFormatter.FormatValue(cIdValue);
            Assert.That(formatted, Is.EqualTo("(null)"));
        }

        #endregion
    }
}
