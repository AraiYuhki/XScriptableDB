using NUnit.Framework;
using System;
using System.Linq;
using Xeon.XScriptableDB.Editor;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// Tests for SchemaComparer.
    /// </summary>
    public class SchemaComparerTests
    {
        #region Test Data Classes

        [Serializable]
        private class OriginalRecord
        {
            [PrimaryKey]
            public int Id;
            public string Name;
            public int Value;
            public bool Active;
        }

        [Serializable]
        private class FieldAddedRecord
        {
            [PrimaryKey]
            public int Id;
            public string Name;
            public int Value;
            public bool Active;
            public string NewField; // Added field
        }

        [Serializable]
        private class FieldRemovedRecord
        {
            [PrimaryKey]
            public int Id;
            public string Name;
            // Value field removed
            public bool Active;
        }

        [Serializable]
        private class FieldTypeChangedRecord
        {
            [PrimaryKey]
            public int Id;
            public string Name;
            public float Value; // Changed from int -> float
            public bool Active;
        }

        [Serializable]
        private class PrimaryKeyChangedRecord
        {
            public int Id; // PrimaryKey attribute removed
            [PrimaryKey]
            public string Name; // Name is the new PrimaryKey
            public int Value;
            public bool Active;
        }

        [Serializable]
        private class SecondaryKeyAddedRecord
        {
            [PrimaryKey]
            public int Id;
            [SecondaryKey]
            public string Name; // SecondaryKey added
            public int Value;
            public bool Active;
        }

        [Serializable]
        private class MultipleChangesRecord
        {
            [PrimaryKey]
            public int Id;
            public string Name;
            public double Value; // int -> double
            // Active removed
            public string Category; // New field
            public int Level; // New field
        }

        #endregion

        #region Compare Tests

        [Test]
        public void Compare_IdenticalTypes_ReturnsNoDifferences()
        {
            var result = SchemaComparer.Compare(typeof(OriginalRecord), typeof(OriginalRecord));

            Assert.That(result.HasDifferences, Is.False);
            Assert.That(result.Differences.Count, Is.EqualTo(0));
            Assert.That(result.IsCompatible, Is.True);
        }

        [Test]
        public void Compare_FieldAdded_DetectsAddition()
        {
            var result = SchemaComparer.Compare(typeof(OriginalRecord), typeof(FieldAddedRecord));

            Assert.That(result.HasDifferences, Is.True);
            Assert.That(result.AddedFieldCount, Is.EqualTo(1));

            var addedDiff = result.Differences.First(d => d.Type == SchemaDifferenceType.FieldAdded);
            Assert.That(addedDiff.FieldName, Is.EqualTo("NewField"));
            Assert.That(result.IsCompatible, Is.True); // Adding a field is compatible
        }

        [Test]
        public void Compare_FieldRemoved_DetectsRemoval()
        {
            var result = SchemaComparer.Compare(typeof(OriginalRecord), typeof(FieldRemovedRecord));

            Assert.That(result.HasDifferences, Is.True);
            Assert.That(result.RemovedFieldCount, Is.EqualTo(1));

            var removedDiff = result.Differences.First(d => d.Type == SchemaDifferenceType.FieldRemoved);
            Assert.That(removedDiff.FieldName, Is.EqualTo("Value"));
            Assert.That(result.IsCompatible, Is.False); // Removing a field is incompatible
        }

        [Test]
        public void Compare_FieldTypeChanged_DetectsTypeChange()
        {
            var result = SchemaComparer.Compare(typeof(OriginalRecord), typeof(FieldTypeChangedRecord));

            Assert.That(result.HasDifferences, Is.True);
            Assert.That(result.ChangedFieldCount, Is.EqualTo(1));

            var changedDiff = result.Differences.First(d => d.Type == SchemaDifferenceType.FieldTypeChanged);
            Assert.That(changedDiff.FieldName, Is.EqualTo("Value"));
            Assert.That(changedDiff.OldValue, Is.EqualTo("Int32"));
            Assert.That(changedDiff.NewValue, Is.EqualTo("Single"));
        }

        [Test]
        public void Compare_PrimaryKeyChanged_DetectsKeyChange()
        {
            var result = SchemaComparer.Compare(typeof(OriginalRecord), typeof(PrimaryKeyChangedRecord));

            Assert.That(result.HasDifferences, Is.True);

            var keyChanges = result.Differences.Where(d => d.Type == SchemaDifferenceType.PrimaryKeyChanged).ToList();
            Assert.That(keyChanges.Count, Is.EqualTo(2)); // Id removed, Name added
        }

        [Test]
        public void Compare_SecondaryKeyAdded_DetectsKeyAddition()
        {
            var result = SchemaComparer.Compare(typeof(OriginalRecord), typeof(SecondaryKeyAddedRecord));

            Assert.That(result.HasDifferences, Is.True);

            var keyAdded = result.Differences.FirstOrDefault(d => d.Type == SchemaDifferenceType.SecondaryKeyAdded);
            Assert.That(keyAdded, Is.Not.Null);
            Assert.That(keyAdded.FieldName, Is.EqualTo("Name"));
        }

        [Test]
        public void Compare_MultipleChanges_DetectsAllChanges()
        {
            var result = SchemaComparer.Compare(typeof(OriginalRecord), typeof(MultipleChangesRecord));

            Assert.That(result.HasDifferences, Is.True);
            Assert.That(result.AddedFieldCount, Is.EqualTo(2)); // Category, Level
            Assert.That(result.RemovedFieldCount, Is.EqualTo(1)); // Active
            Assert.That(result.ChangedFieldCount, Is.EqualTo(1)); // Value: int -> double
        }

        #endregion

        #region GetFieldSchemas Tests

        [Test]
        public void GetFieldSchemas_ReturnsAllPublicFields()
        {
            var schemas = SchemaComparer.GetFieldSchemas(typeof(OriginalRecord));

            Assert.That(schemas.Count, Is.EqualTo(4));
            Assert.That(schemas.ContainsKey("Id"), Is.True);
            Assert.That(schemas.ContainsKey("Name"), Is.True);
            Assert.That(schemas.ContainsKey("Value"), Is.True);
            Assert.That(schemas.ContainsKey("Active"), Is.True);
        }

        [Test]
        public void GetFieldSchemas_DetectsPrimaryKey()
        {
            var schemas = SchemaComparer.GetFieldSchemas(typeof(OriginalRecord));

            Assert.That(schemas["Id"].IsPrimaryKey, Is.True);
            Assert.That(schemas["Name"].IsPrimaryKey, Is.False);
        }

        [Test]
        public void GetFieldSchemas_DetectsSecondaryKey()
        {
            var schemas = SchemaComparer.GetFieldSchemas(typeof(SecondaryKeyAddedRecord));

            Assert.That(schemas["Name"].IsSecondaryKey, Is.True);
            Assert.That(schemas["Id"].IsSecondaryKey, Is.False);
        }

        [Test]
        public void GetFieldSchemas_ReturnsCorrectTypes()
        {
            var schemas = SchemaComparer.GetFieldSchemas(typeof(OriginalRecord));

            Assert.That(schemas["Id"].FieldType, Is.EqualTo(typeof(int)));
            Assert.That(schemas["Name"].FieldType, Is.EqualTo(typeof(string)));
            Assert.That(schemas["Value"].FieldType, Is.EqualTo(typeof(int)));
            Assert.That(schemas["Active"].FieldType, Is.EqualTo(typeof(bool)));
        }

        #endregion

        #region GenerateSchemaSummary Tests

        [Test]
        public void GenerateSchemaSummary_ContainsTableName()
        {
            var summary = SchemaComparer.GenerateSchemaSummary(typeof(OriginalRecord));

            Assert.That(summary, Does.Contain("OriginalRecord"));
        }

        [Test]
        public void GenerateSchemaSummary_ContainsFieldCount()
        {
            var summary = SchemaComparer.GenerateSchemaSummary(typeof(OriginalRecord));

            Assert.That(summary, Does.Contain("フィールド数: 4"));
        }

        [Test]
        public void GenerateSchemaSummary_ContainsPrimaryKeyInfo()
        {
            var summary = SchemaComparer.GenerateSchemaSummary(typeof(OriginalRecord));

            Assert.That(summary, Does.Contain("PrimaryKey: Id"));
        }

        [Test]
        public void GenerateSchemaSummary_ContainsFieldList()
        {
            var summary = SchemaComparer.GenerateSchemaSummary(typeof(OriginalRecord));

            Assert.That(summary, Does.Contain("Id: Int32"));
            Assert.That(summary, Does.Contain("Name: String"));
            Assert.That(summary, Does.Contain("Value: Int32"));
            Assert.That(summary, Does.Contain("Active: Boolean"));
        }

        #endregion

        #region SchemaDifference ToString Tests

        [Test]
        public void SchemaDifference_FieldAdded_ToString()
        {
            var diff = new SchemaDifference(SchemaDifferenceType.FieldAdded, "NewField", null, "String");
            var str = diff.ToString();

            Assert.That(str, Does.Contain("フィールド追加"));
            Assert.That(str, Does.Contain("NewField"));
        }

        [Test]
        public void SchemaDifference_FieldRemoved_ToString()
        {
            var diff = new SchemaDifference(SchemaDifferenceType.FieldRemoved, "OldField", "Int32", null);

            var str = diff.ToString();

            Assert.That(str, Does.Contain("フィールド削除"));
            Assert.That(str, Does.Contain("OldField"));
        }

        [Test]
        public void SchemaDifference_FieldTypeChanged_ToString()
        {
            var diff = new SchemaDifference(SchemaDifferenceType.FieldTypeChanged, "Value", "Int32", "Single");

            var str = diff.ToString();

            Assert.That(str, Does.Contain("型変更"));
            Assert.That(str, Does.Contain("Int32"));
            Assert.That(str, Does.Contain("Single"));
        }

        #endregion
    }
}
