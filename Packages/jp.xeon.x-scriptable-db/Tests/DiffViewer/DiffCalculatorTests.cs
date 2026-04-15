using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Xeon.XScriptableDB.Editor;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// Tests for DiffCalculator.
    /// </summary>
    public class DiffCalculatorTests
    {
        #region Test Data Classes

        private class TestRecord
        {
            [PrimaryKey]
            public int Id { get; set; }

            public string Name { get; set; }
            public int Value { get; set; }
        }

        #endregion

        #region Basic Diff Tests

        [Test]
        public void Calculate_NoChanges_ReturnsAllUnchanged()
        {
            var oldRecords = new List<TestRecord>
            {
                new TestRecord { Id = 1, Name = "A", Value = 100 },
                new TestRecord { Id = 2, Name = "B", Value = 200 }
            };

            var newRecords = new List<TestRecord>
            {
                new TestRecord { Id = 1, Name = "A", Value = 100 },
                new TestRecord { Id = 2, Name = "B", Value = 200 }
            };

            var result = DiffCalculator.Calculate(oldRecords, newRecords, r => r.Id, "TestTable");

            Assert.That(result.HasDifferences, Is.False);
            Assert.That(result.UnchangedCount, Is.EqualTo(2));
            Assert.That(result.AddedCount, Is.EqualTo(0));
            Assert.That(result.RemovedCount, Is.EqualTo(0));
            Assert.That(result.ModifiedCount, Is.EqualTo(0));
        }

        [Test]
        public void Calculate_AddedRecords_DetectsAdditions()
        {
            var oldRecords = new List<TestRecord>
            {
                new TestRecord { Id = 1, Name = "A", Value = 100 }
            };

            var newRecords = new List<TestRecord>
            {
                new TestRecord { Id = 1, Name = "A", Value = 100 },
                new TestRecord { Id = 2, Name = "B", Value = 200 },
                new TestRecord { Id = 3, Name = "C", Value = 300 }
            };

            var result = DiffCalculator.Calculate(oldRecords, newRecords, r => r.Id, "TestTable");

            Assert.That(result.HasDifferences, Is.True);
            Assert.That(result.AddedCount, Is.EqualTo(2));
            Assert.That(result.UnchangedCount, Is.EqualTo(1));

            var addedDiffs = result.Diffs.Where(d => d.DiffType == DiffType.Added).ToList();
            Assert.That(addedDiffs.Count, Is.EqualTo(2));
            Assert.That(addedDiffs.Any(d => (int)d.PrimaryKey == 2), Is.True);
            Assert.That(addedDiffs.Any(d => (int)d.PrimaryKey == 3), Is.True);
        }

        [Test]
        public void Calculate_RemovedRecords_DetectsRemovals()
        {
            var oldRecords = new List<TestRecord>
            {
                new TestRecord { Id = 1, Name = "A", Value = 100 },
                new TestRecord { Id = 2, Name = "B", Value = 200 },
                new TestRecord { Id = 3, Name = "C", Value = 300 }
            };

            var newRecords = new List<TestRecord>
            {
                new TestRecord { Id = 1, Name = "A", Value = 100 }
            };

            var result = DiffCalculator.Calculate(oldRecords, newRecords, r => r.Id, "TestTable");

            Assert.That(result.HasDifferences, Is.True);
            Assert.That(result.RemovedCount, Is.EqualTo(2));
            Assert.That(result.UnchangedCount, Is.EqualTo(1));

            var removedDiffs = result.Diffs.Where(d => d.DiffType == DiffType.Removed).ToList();
            Assert.That(removedDiffs.Count, Is.EqualTo(2));
            Assert.That(removedDiffs.Any(d => (int)d.PrimaryKey == 2), Is.True);
            Assert.That(removedDiffs.Any(d => (int)d.PrimaryKey == 3), Is.True);
        }

        [Test]
        public void Calculate_ModifiedRecords_DetectsModifications()
        {
            var oldRecords = new List<TestRecord>
            {
                new TestRecord { Id = 1, Name = "A", Value = 100 },
                new TestRecord { Id = 2, Name = "B", Value = 200 }
            };

            var newRecords = new List<TestRecord>
            {
                new TestRecord { Id = 1, Name = "A", Value = 100 },
                new TestRecord { Id = 2, Name = "B Modified", Value = 250 }
            };

            var result = DiffCalculator.Calculate(oldRecords, newRecords, r => r.Id, "TestTable");

            Assert.That(result.HasDifferences, Is.True);
            Assert.That(result.ModifiedCount, Is.EqualTo(1));
            Assert.That(result.UnchangedCount, Is.EqualTo(1));

            var modifiedDiff = result.Diffs.First(d => d.DiffType == DiffType.Modified);
            Assert.That((int)modifiedDiff.PrimaryKey, Is.EqualTo(2));
            Assert.That(modifiedDiff.ChangedFieldCount, Is.GreaterThan(0));
        }

        [Test]
        public void Calculate_MixedChanges_DetectsAllTypes()
        {
            var oldRecords = new List<TestRecord>
            {
                new TestRecord { Id = 1, Name = "A", Value = 100 },
                new TestRecord { Id = 2, Name = "B", Value = 200 },
                new TestRecord { Id = 3, Name = "C", Value = 300 }
            };

            var newRecords = new List<TestRecord>
            {
                new TestRecord { Id = 1, Name = "A Modified", Value = 100 }, // Modified
                // Id = 2 removed
                new TestRecord { Id = 3, Name = "C", Value = 300 }, // Unchanged
                new TestRecord { Id = 4, Name = "D", Value = 400 }  // Added
            };

            var result = DiffCalculator.Calculate(oldRecords, newRecords, r => r.Id, "TestTable");

            Assert.That(result.HasDifferences, Is.True);
            Assert.That(result.AddedCount, Is.EqualTo(1));
            Assert.That(result.RemovedCount, Is.EqualTo(1));
            Assert.That(result.ModifiedCount, Is.EqualTo(1));
            Assert.That(result.UnchangedCount, Is.EqualTo(1));
        }

        #endregion

        #region Empty Records Tests

        [Test]
        public void Calculate_EmptyOldRecords_AllAdded()
        {
            var oldRecords = new List<TestRecord>();

            var newRecords = new List<TestRecord>
            {
                new TestRecord { Id = 1, Name = "A", Value = 100 },
                new TestRecord { Id = 2, Name = "B", Value = 200 }
            };

            var result = DiffCalculator.Calculate(oldRecords, newRecords, r => r.Id, "TestTable");

            Assert.That(result.AddedCount, Is.EqualTo(2));
            Assert.That(result.RemovedCount, Is.EqualTo(0));
        }

        [Test]
        public void Calculate_EmptyNewRecords_AllRemoved()
        {
            var oldRecords = new List<TestRecord>
            {
                new TestRecord { Id = 1, Name = "A", Value = 100 },
                new TestRecord { Id = 2, Name = "B", Value = 200 }
            };

            var newRecords = new List<TestRecord>();

            var result = DiffCalculator.Calculate(oldRecords, newRecords, r => r.Id, "TestTable");

            Assert.That(result.AddedCount, Is.EqualTo(0));
            Assert.That(result.RemovedCount, Is.EqualTo(2));
        }

        [Test]
        public void Calculate_BothEmpty_NoDifferences()
        {
            var oldRecords = new List<TestRecord>();
            var newRecords = new List<TestRecord>();

            var result = DiffCalculator.Calculate(oldRecords, newRecords, r => r.Id, "TestTable");

            Assert.That(result.HasDifferences, Is.False);
            Assert.That(result.Diffs.Count, Is.EqualTo(0));
        }

        #endregion

        #region Field Diff Tests

        [Test]
        public void CompareRecords_AllFieldsChanged_ReturnsAllModified()
        {
            var oldRecord = new TestRecord { Id = 1, Name = "Old", Value = 100 };
            var newRecord = new TestRecord { Id = 1, Name = "New", Value = 200 };

            var fieldDiffs = DiffCalculator.CompareRecords(oldRecord, newRecord);

            var changedFields = fieldDiffs.Where(f => f.HasChanged).ToList();
            Assert.That(changedFields.Count, Is.GreaterThanOrEqualTo(2)); // Name and Value
        }

        [Test]
        public void CompareRecords_NoFieldsChanged_ReturnsAllUnchanged()
        {
            var oldRecord = new TestRecord { Id = 1, Name = "Same", Value = 100 };
            var newRecord = new TestRecord { Id = 1, Name = "Same", Value = 100 };

            var fieldDiffs = DiffCalculator.CompareRecords(oldRecord, newRecord);

            var changedFields = fieldDiffs.Where(f => f.HasChanged).ToList();
            Assert.That(changedFields.Count, Is.EqualTo(0));
        }

        [Test]
        public void CompareRecords_NullOldRecord_ReturnsEmpty()
        {
            var newRecord = new TestRecord { Id = 1, Name = "New", Value = 100 };

            var fieldDiffs = DiffCalculator.CompareRecords(null, newRecord);

            Assert.That(fieldDiffs.Count, Is.EqualTo(0));
        }

        [Test]
        public void CompareRecords_NullNewRecord_ReturnsEmpty()
        {
            var oldRecord = new TestRecord { Id = 1, Name = "Old", Value = 100 };

            var fieldDiffs = DiffCalculator.CompareRecords(oldRecord, null);

            Assert.That(fieldDiffs.Count, Is.EqualTo(0));
        }

        #endregion

        #region Sorting Tests

        [Test]
        public void Calculate_ResultsSortedByPrimaryKey()
        {
            var oldRecords = new List<TestRecord>
            {
                new TestRecord { Id = 3, Name = "C", Value = 300 },
                new TestRecord { Id = 1, Name = "A", Value = 100 }
            };

            var newRecords = new List<TestRecord>
            {
                new TestRecord { Id = 2, Name = "B", Value = 200 },
                new TestRecord { Id = 3, Name = "C", Value = 300 },
                new TestRecord { Id = 1, Name = "A", Value = 100 }
            };

            var result = DiffCalculator.Calculate(oldRecords, newRecords, r => r.Id, "TestTable");

            var keys = result.Diffs.Select(d => (int)d.PrimaryKey).ToList();
            var sortedKeys = keys.OrderBy(k => k).ToList();

            Assert.That(keys, Is.EqualTo(sortedKeys));
        }

        #endregion

        #region TableDiffResult Tests

        [Test]
        public void TableDiffResult_RecalculateCounts_CorrectCounts()
        {
            var result = new TableDiffResult
            {
                TableName = "Test",
                Diffs = new List<RecordDiff>
                {
                    new RecordDiff { DiffType = DiffType.Added },
                    new RecordDiff { DiffType = DiffType.Added },
                    new RecordDiff { DiffType = DiffType.Removed },
                    new RecordDiff { DiffType = DiffType.Modified },
                    new RecordDiff { DiffType = DiffType.Unchanged },
                    new RecordDiff { DiffType = DiffType.Unchanged }
                }
            };

            result.RecalculateCounts();

            Assert.That(result.AddedCount, Is.EqualTo(2));
            Assert.That(result.RemovedCount, Is.EqualTo(1));
            Assert.That(result.ModifiedCount, Is.EqualTo(1));
            Assert.That(result.UnchangedCount, Is.EqualTo(2));
        }

        [Test]
        public void TableDiffResult_GetSummary_FormatsCorrectly()
        {
            var result = new TableDiffResult
            {
                TableName = "TestTable",
                Diffs = new List<RecordDiff>
                {
                    new RecordDiff { DiffType = DiffType.Added },
                    new RecordDiff { DiffType = DiffType.Removed },
                    new RecordDiff { DiffType = DiffType.Modified }
                }
            };

            result.RecalculateCounts();
            var summary = result.GetSummary();

            Assert.That(summary, Does.Contain("TestTable"));
            Assert.That(summary, Does.Contain("+1"));
            Assert.That(summary, Does.Contain("-1"));
            Assert.That(summary, Does.Contain("*1"));
        }

        #endregion

        #region RecordDiff Tests

        [Test]
        public void RecordDiff_Summary_FormatsCorrectly()
        {
            var addedDiff = new RecordDiff { PrimaryKey = 1, DiffType = DiffType.Added };
            Assert.That(addedDiff.Summary, Does.Contain("[+]"));
            Assert.That(addedDiff.Summary, Does.Contain("1"));

            var removedDiff = new RecordDiff { PrimaryKey = 2, DiffType = DiffType.Removed };
            Assert.That(removedDiff.Summary, Does.Contain("[-]"));

            var modifiedDiff = new RecordDiff
            {
                PrimaryKey = 3,
                DiffType = DiffType.Modified,
                FieldDiffs = new List<FieldDiff>
                {
                    new FieldDiff("Field1", "old", "new", DiffType.Modified),
                    new FieldDiff("Field2", 1, 2, DiffType.Modified)
                }
            };
            Assert.That(modifiedDiff.Summary, Does.Contain("[*]"));
            Assert.That(modifiedDiff.Summary, Does.Contain("2 fields"));
        }

        #endregion

        #region FieldDiff Tests

        [Test]
        public void FieldDiff_ToString_FormatsCorrectly()
        {
            var unchanged = new FieldDiff("Name", "Value", "Value", DiffType.Unchanged);
            Assert.That(unchanged.ToString(), Does.Contain("unchanged"));

            var added = new FieldDiff("Name", null, "NewValue", DiffType.Added);
            Assert.That(added.ToString(), Does.Contain("+"));

            var removed = new FieldDiff("Name", "OldValue", null, DiffType.Removed);
            Assert.That(removed.ToString(), Does.Contain("-"));

            var modified = new FieldDiff("Name", "Old", "New", DiffType.Modified);
            Assert.That(modified.ToString(), Does.Contain("->"));
        }

        [Test]
        public void FieldDiff_HasChanged_ReturnsCorrectValue()
        {
            var unchanged = new FieldDiff("Name", "Value", "Value", DiffType.Unchanged);
            Assert.That(unchanged.HasChanged, Is.False);

            var modified = new FieldDiff("Name", "Old", "New", DiffType.Modified);
            Assert.That(modified.HasChanged, Is.True);
        }

        #endregion
    }
}
