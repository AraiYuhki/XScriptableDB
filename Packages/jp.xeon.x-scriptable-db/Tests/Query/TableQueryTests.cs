using NUnit.Framework;
using System;
using System.Linq;
using UnityEngine;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// TableQuery 拡張メソッドのテスト。
    /// </summary>
    public class TableQueryTests
    {
        #region Test Data Classes

        private class TestRecord
        {
            [PrimaryKey]
            public int Id { get; set; }

            [SecondaryKey("Category")]
            public string Category { get; set; }

            [SecondaryKey("GroupId")]
            public int GroupId { get; set; }

            public string Name { get; set; }
            public int Value { get; set; }
        }

        /// <summary>
        /// テスト用のTableAsset実装。
        /// </summary>
        private class TestTableAsset : TableAsset<TestRecord, int>
        {
            public static TestTableAsset Create(TestRecord[] records)
            {
                var asset = ScriptableObject.CreateInstance<TestTableAsset>();
                asset.records = records;
                asset.EnsureSorted();
                asset.secondaryIndices = IndexBuilder.BuildIndices(records);
                return asset;
            }
        }

        #endregion

        private TestTableAsset CreateTestTable()
        {
            var records = new[]
            {
                new TestRecord { Id = 1, Category = "A", GroupId = 10, Name = "Record1", Value = 100 },
                new TestRecord { Id = 2, Category = "B", GroupId = 10, Name = "Record2", Value = 200 },
                new TestRecord { Id = 3, Category = "A", GroupId = 20, Name = "Record3", Value = 150 },
                new TestRecord { Id = 4, Category = "C", GroupId = 20, Name = "Record4", Value = 300 },
                new TestRecord { Id = 5, Category = "A", GroupId = 10, Name = "Record5", Value = 50 }
            };
            return TestTableAsset.Create(records);
        }

        #region QueryBySecondaryKey Tests

        [Test]
        public void QueryBySecondaryKey_String_ReturnsCorrectRecords()
        {
            var table = CreateTestTable();

            var result = table.QueryBySecondaryKey<TestRecord, int, string>("Category", "A");

            Assert.That(result.Count, Is.EqualTo(3));
            foreach (var record in result)
            {
                Assert.That(record.Category, Is.EqualTo("A"));
            }
        }

        [Test]
        public void QueryBySecondaryKey_Int_ReturnsCorrectRecords()
        {
            var table = CreateTestTable();

            var result = table.QueryBySecondaryKey<TestRecord, int, int>("GroupId", 10);

            Assert.That(result.Count, Is.EqualTo(3));
            foreach (var record in result)
            {
                Assert.That(record.GroupId, Is.EqualTo(10));
            }
        }

        [Test]
        public void QueryBySecondaryKey_NotFound_ReturnsEmpty()
        {
            var table = CreateTestTable();

            var result = table.QueryBySecondaryKey<TestRecord, int, string>("Category", "Z");

            Assert.That(result.IsEmpty, Is.True);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void QueryBySecondaryKey_InvalidIndex_ReturnsEmpty()
        {
            var table = CreateTestTable();

            var result = table.QueryBySecondaryKey<TestRecord, int, string>("NonExistent", "A");

            Assert.That(result.IsEmpty, Is.True);
        }

        #endregion

        #region Where Tests

        [Test]
        public void Where_ReturnsMatchingRecords()
        {
            var table = CreateTestTable();

            var results = table.Where(r => r.Value > 150).ToList();

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.All(r => r.Value > 150), Is.True);
        }

        [Test]
        public void Where_NoMatch_ReturnsEmpty()
        {
            var table = CreateTestTable();

            var results = table.Where(r => r.Value > 1000).ToList();

            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public void Where_AllMatch_ReturnsAll()
        {
            var table = CreateTestTable();

            var results = table.Where(r => r.Value > 0).ToList();

            Assert.That(results.Count, Is.EqualTo(5));
        }

        [Test]
        public void Where_ComplexPredicate_Works()
        {
            var table = CreateTestTable();

            var results = table.Where(r => r.Category == "A" && r.Value > 100).ToList();

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Id, Is.EqualTo(3));
        }

        #endregion

        #region FirstOrDefault Tests

        [Test]
        public void FirstOrDefault_Found_ReturnsRecord()
        {
            var table = CreateTestTable();

            var result = table.FirstOrDefault(r => r.Category == "B");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Category, Is.EqualTo("B"));
        }

        [Test]
        public void FirstOrDefault_NotFound_ReturnsNull()
        {
            var table = CreateTestTable();

            var result = table.FirstOrDefault(r => r.Category == "Z");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void FirstOrDefault_MultipleMatches_ReturnsFirst()
        {
            var table = CreateTestTable();

            var result = table.FirstOrDefault(r => r.Category == "A");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(1)); // First sorted record with Category "A"
        }

        #endregion

        #region Any Tests

        [Test]
        public void Any_Exists_ReturnsTrue()
        {
            var table = CreateTestTable();

            var result = table.Any(r => r.Value == 300);

            Assert.That(result, Is.True);
        }

        [Test]
        public void Any_NotExists_ReturnsFalse()
        {
            var table = CreateTestTable();

            var result = table.Any(r => r.Value == 999);

            Assert.That(result, Is.False);
        }

        #endregion

        #region All Tests

        [Test]
        public void All_AllMatch_ReturnsTrue()
        {
            var table = CreateTestTable();

            var result = table.All(r => r.Id > 0);

            Assert.That(result, Is.True);
        }

        [Test]
        public void All_SomeNotMatch_ReturnsFalse()
        {
            var table = CreateTestTable();

            var result = table.All(r => r.Category == "A");

            Assert.That(result, Is.False);
        }

        [Test]
        public void All_EmptyTable_ReturnsTrue()
        {
            var emptyTable = TestTableAsset.Create(Array.Empty<TestRecord>());

            var result = emptyTable.All(r => r.Value > 1000);

            Assert.That(result, Is.True); // Vacuous truth
        }

        #endregion

        #region Count Tests

        [Test]
        public void Count_ReturnsCorrectCount()
        {
            var table = CreateTestTable();

            var count = table.Count(r => r.GroupId == 10);

            Assert.That(count, Is.EqualTo(3));
        }

        [Test]
        public void Count_NoMatch_ReturnsZero()
        {
            var table = CreateTestTable();

            var count = table.Count(r => r.Value > 1000);

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void Count_AllMatch_ReturnsTotal()
        {
            var table = CreateTestTable();

            var count = table.Count(r => true);

            Assert.That(count, Is.EqualTo(5));
        }

        #endregion

        #region Select Tests

        [Test]
        public void Select_TransformsRecords()
        {
            var table = CreateTestTable();

            var names = table.Select(r => r.Name).ToList();

            Assert.That(names.Count, Is.EqualTo(5));
            Assert.That(names, Contains.Item("Record1"));
            Assert.That(names, Contains.Item("Record5"));
        }

        [Test]
        public void Select_ToInt_TransformsCorrectly()
        {
            var table = CreateTestTable();

            var ids = table.Select(r => r.Id).ToList();

            Assert.That(ids.Count, Is.EqualTo(5));
            Assert.That(ids, Contains.Item(1));
            Assert.That(ids, Contains.Item(5));
        }

        [Test]
        public void Select_ToAnonymousType_Works()
        {
            var table = CreateTestTable();

            var projected = table.Select(r => new { r.Id, r.Name }).ToList();

            Assert.That(projected.Count, Is.EqualTo(5));
            Assert.That(projected[0].Id, Is.EqualTo(1));
        }

        #endregion

        #region Skip Tests

        [Test]
        public void Skip_SkipsCorrectNumber()
        {
            var table = CreateTestTable();

            var results = table.Skip(2).ToList();

            Assert.That(results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Skip_Zero_ReturnsAll()
        {
            var table = CreateTestTable();

            var results = table.Skip(0).ToList();

            Assert.That(results.Count, Is.EqualTo(5));
        }

        [Test]
        public void Skip_MoreThanCount_ReturnsEmpty()
        {
            var table = CreateTestTable();

            var results = table.Skip(10).ToList();

            Assert.That(results.Count, Is.EqualTo(0));
        }

        #endregion

        #region Take Tests

        [Test]
        public void Take_TakesCorrectNumber()
        {
            var table = CreateTestTable();

            var results = table.Take(3).ToList();

            Assert.That(results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Take_Zero_ReturnsEmpty()
        {
            var table = CreateTestTable();

            var results = table.Take(0).ToList();

            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public void Take_MoreThanCount_ReturnsAll()
        {
            var table = CreateTestTable();

            var results = table.Take(100).ToList();

            Assert.That(results.Count, Is.EqualTo(5));
        }

        #endregion

        #region Chained Operations Tests

        [Test]
        public void ChainedOperations_WhereSelect_Works()
        {
            var table = CreateTestTable();

            var names = table.Where(r => r.Category == "A")
                            .Select(r => r.Name)
                            .ToList();

            Assert.That(names.Count, Is.EqualTo(3));
        }

        [Test]
        public void ChainedOperations_SkipTake_Works()
        {
            var table = CreateTestTable();

            var results = table.Skip(1).Take(2).ToList();

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Id, Is.EqualTo(2));
            Assert.That(results[1].Id, Is.EqualTo(3));
        }

        #endregion

        [TearDown]
        public void TearDown()
        {
            // Clean up ScriptableObject instances
            var assets = Resources.FindObjectsOfTypeAll<TestTableAsset>();
            foreach (var asset in assets)
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }
    }
}
