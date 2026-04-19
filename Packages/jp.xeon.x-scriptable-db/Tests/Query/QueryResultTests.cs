using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// QueryResultおよびQueryResultSpanのテスト。
    /// </summary>
    public class QueryResultTests
    {
        private class TestRecord
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private TestRecord[] CreateTestRecords()
        {
            return new[]
            {
                new TestRecord { Id = 1, Name = "Record1" },
                new TestRecord { Id = 2, Name = "Record2" },
                new TestRecord { Id = 3, Name = "Record3" },
                new TestRecord { Id = 4, Name = "Record4" },
                new TestRecord { Id = 5, Name = "Record5" }
            };
        }

        #region QueryResult Constructor Tests

        [Test]
        public void QueryResult_WithSourceAndIndices_SetsCorrectly()
        {
            var records = CreateTestRecords();
            var indices = new[] { 0, 2, 4 };

            var result = new QueryResult<TestRecord>(records, indices);

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.IsEmpty, Is.False);
        }

        [Test]
        public void QueryResult_WithSourceOnly_UsesAllRecords()
        {
            var records = CreateTestRecords();

            var result = new QueryResult<TestRecord>(records);

            Assert.That(result.Count, Is.EqualTo(5));
        }

        [Test]
        public void QueryResult_Empty_ReturnsEmptyResult()
        {
            var result = QueryResult<TestRecord>.Empty;

            Assert.That(result.Count, Is.EqualTo(0));
            Assert.That(result.IsEmpty, Is.True);
            Assert.That(result.First, Is.Null);
        }

        [Test]
        public void QueryResult_NullIndices_SetsCountToZero()
        {
            var records = CreateTestRecords();

            var result = new QueryResult<TestRecord>(records, null);

            Assert.That(result.Count, Is.EqualTo(0));
        }

        #endregion

        #region QueryResult Indexer Tests

        [Test]
        public void QueryResult_Indexer_WithIndices_ReturnsCorrectRecord()
        {
            var records = CreateTestRecords();
            var indices = new[] { 1, 3 };

            var result = new QueryResult<TestRecord>(records, indices);

            Assert.That(result[0].Id, Is.EqualTo(2)); // records[1]
            Assert.That(result[1].Id, Is.EqualTo(4)); // records[3]
        }

        [Test]
        public void QueryResult_Indexer_WithoutIndices_ReturnsDirectIndex()
        {
            var records = CreateTestRecords();

            var result = new QueryResult<TestRecord>(records);

            Assert.That(result[0].Id, Is.EqualTo(1));
            Assert.That(result[2].Id, Is.EqualTo(3));
        }

        [Test]
        public void QueryResult_Indexer_OutOfRange_ThrowsException()
        {
            var records = CreateTestRecords();
            var indices = new[] { 0, 1 };

            var result = new QueryResult<TestRecord>(records, indices);

            // ラムダ式内でref structを使用できないため、try-catchでテストする
            var threw = false;
            try
            {
                var _ = result[2];
            }
            catch (IndexOutOfRangeException)
            {
                threw = true;
            }
            Assert.That(threw, Is.True, "IndexOutOfRangeExceptionがスローされることを想定");
        }

        [Test]
        public void QueryResult_Indexer_NegativeIndex_ThrowsException()
        {
            var records = CreateTestRecords();
            var result = new QueryResult<TestRecord>(records);

            // ラムダ式内でref structを使用できないため、try-catchでテストする
            var threw = false;
            try
            {
                var _ = result[-1];
            }
            catch (IndexOutOfRangeException)
            {
                threw = true;
            }
            Assert.That(threw, Is.True, "IndexOutOfRangeExceptionがスローされることを想定");
        }

        #endregion

        #region QueryResult First Property Tests

        [Test]
        public void QueryResult_First_ReturnsFirstRecord()
        {
            var records = CreateTestRecords();
            var indices = new[] { 2, 4 };

            var result = new QueryResult<TestRecord>(records, indices);

            Assert.That(result.First.Id, Is.EqualTo(3)); // records[2]
        }

        [Test]
        public void QueryResult_First_Empty_ReturnsNull()
        {
            var result = QueryResult<TestRecord>.Empty;
            Assert.That(result.First, Is.Null);
        }

        #endregion

        #region QueryResult Enumerator Tests

        [Test]
        public void QueryResult_GetEnumerator_IteratesCorrectly()
        {
            var records = CreateTestRecords();
            var indices = new[] { 0, 2, 4 };

            var result = new QueryResult<TestRecord>(records, indices);
            var iteratedIds = new List<int>();

            foreach (var record in result)
            {
                iteratedIds.Add(record.Id);
            }

            Assert.That(iteratedIds.Count, Is.EqualTo(3));
            Assert.That(iteratedIds[0], Is.EqualTo(1));
            Assert.That(iteratedIds[1], Is.EqualTo(3));
            Assert.That(iteratedIds[2], Is.EqualTo(5));
        }

        [Test]
        public void QueryResult_GetEnumerator_EmptyResult_NoIterations()
        {
            var result = QueryResult<TestRecord>.Empty;
            var count = 0;

            foreach (var _ in result)
            {
                count++;
            }

            Assert.That(count, Is.EqualTo(0));
        }

        #endregion

        #region QueryResult ToArray Tests

        [Test]
        public void QueryResult_ToArray_ReturnsCorrectArray()
        {
            var records = CreateTestRecords();
            var indices = new[] { 1, 3 };

            var result = new QueryResult<TestRecord>(records, indices);
            var array = result.ToArray();

            Assert.That(array.Length, Is.EqualTo(2));
            Assert.That(array[0].Id, Is.EqualTo(2));
            Assert.That(array[1].Id, Is.EqualTo(4));
        }

        [Test]
        public void QueryResult_ToArray_Empty_ReturnsEmptyArray()
        {
            var result = QueryResult<TestRecord>.Empty;
            var array = result.ToArray();

            Assert.That(array, Is.Not.Null);
            Assert.That(array.Length, Is.EqualTo(0));
        }

        #endregion

        #region QueryResult ToList Tests

        [Test]
        public void QueryResult_ToList_ReturnsCorrectList()
        {
            var records = CreateTestRecords();
            var indices = new[] { 0, 4 };

            var result = new QueryResult<TestRecord>(records, indices);
            var list = result.ToList();

            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0].Id, Is.EqualTo(1));
            Assert.That(list[1].Id, Is.EqualTo(5));
        }

        [Test]
        public void QueryResult_ToList_Empty_ReturnsEmptyList()
        {
            var result = QueryResult<TestRecord>.Empty;
            var list = result.ToList();

            Assert.That(list, Is.Not.Null);
            Assert.That(list.Count, Is.EqualTo(0));
        }

        #endregion

        #region QueryResultSpan Tests

        [Test]
        public void QueryResultSpan_FromArray_SetsCorrectly()
        {
            var records = CreateTestRecords();

            var result = new QueryResultSpan<TestRecord>(records);

            Assert.That(result.Length, Is.EqualTo(5));
            Assert.That(result.IsEmpty, Is.False);
        }

        [Test]
        public void QueryResultSpan_Empty_ReturnsEmptyResult()
        {
            var result = QueryResultSpan<TestRecord>.Empty;

            Assert.That(result.Length, Is.EqualTo(0));
            Assert.That(result.IsEmpty, Is.True);
        }

        [Test]
        public void QueryResultSpan_Indexer_ReturnsCorrectRecord()
        {
            var records = CreateTestRecords();

            var result = new QueryResultSpan<TestRecord>(records);

            Assert.That(result[0].Id, Is.EqualTo(1));
            Assert.That(result[2].Id, Is.EqualTo(3));
        }

        [Test]
        public void QueryResultSpan_ToArray_ReturnsCorrectArray()
        {
            var records = CreateTestRecords();

            var result = new QueryResultSpan<TestRecord>(records);
            var array = result.ToArray();

            Assert.That(array.Length, Is.EqualTo(5));
            Assert.That(array[0].Id, Is.EqualTo(1));
        }

        [Test]
        public void QueryResultSpan_GetEnumerator_IteratesCorrectly()
        {
            var records = CreateTestRecords();
            var result = new QueryResultSpan<TestRecord>(records);
            var count = 0;

            foreach (var record in result)
            {
                count++;
                Assert.That(record, Is.Not.Null);
            }

            Assert.That(count, Is.EqualTo(5));
        }

        #endregion
    }
}
