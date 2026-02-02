using NUnit.Framework;
using System.Collections.Generic;
using Xeon.XScriptableDB.Editor;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// 複合インデックスのEditor機能テスト。
    /// </summary>
    public class CompositeIndexEditorTests
    {
        #region IndexDefinition Tests

        [Test]
        public void IndexDefinition_DefaultConstructor_CreatesEmpty()
        {
            var indexDef = new IndexDefinition();
            Assert.That(indexDef.Name, Is.Null);
            Assert.That(indexDef.Columns, Is.Empty);
            Assert.That(indexDef.AllowDuplicates, Is.True);
            Assert.That(indexDef.IsComposite, Is.False);
        }

        [Test]
        public void IndexDefinition_WithName_CreatesCorrectly()
        {
            var indexDef = new IndexDefinition("TestIndex");
            Assert.That(indexDef.Name, Is.EqualTo("TestIndex"));
            Assert.That(indexDef.Columns.Count, Is.EqualTo(1));
            Assert.That(indexDef.Columns[0], Is.EqualTo("TestIndex"));
        }

        [Test]
        public void IndexDefinition_WithMultipleColumns_IsComposite()
        {
            var indexDef = new IndexDefinition("TestIndex", "col1", "col2");
            Assert.That(indexDef.IsComposite, Is.True);
            Assert.That(indexDef.Columns.Count, Is.EqualTo(2));
        }

        [Test]
        public void IndexDefinition_FromSingleColumn_CreatesCorrectly()
        {
            var indexDef = IndexDefinition.FromSingleColumn("Category");
            Assert.That(indexDef.Name, Is.EqualTo("Category"));
            Assert.That(indexDef.Columns.Count, Is.EqualTo(1));
            Assert.That(indexDef.IsComposite, Is.False);
        }

        [Test]
        public void IndexDefinition_FromComposite_CreatesCorrectly()
        {
            var indexDef = IndexDefinition.FromComposite("CategoryPrice", "category", "price");
            Assert.That(indexDef.Name, Is.EqualTo("CategoryPrice"));
            Assert.That(indexDef.Columns.Count, Is.EqualTo(2));
            Assert.That(indexDef.IsComposite, Is.True);
        }

        [Test]
        public void IndexDefinition_FromLegacy_CreatesCorrectly()
        {
            var indexDef = IndexDefinition.FromLegacy("OldIndex");
            Assert.That(indexDef.Name, Is.EqualTo("OldIndex"));
            Assert.That(indexDef.Columns.Count, Is.EqualTo(1));
        }

        [Test]
        public void IndexDefinition_ToString_FormatsCorrectly()
        {
            var indexDef = new IndexDefinition("TestIndex", "col1", "col2");
            var str = indexDef.ToString();
            Assert.That(str, Does.Contain("TestIndex"));
            Assert.That(str, Does.Contain("col1"));
            Assert.That(str, Does.Contain("col2"));
        }

        [Test]
        public void IndexDefinition_ToString_UniqueShowsMarker()
        {
            var indexDef = new IndexDefinition("UniqueIndex", "col1") { AllowDuplicates = false };
            var str = indexDef.ToString();
            Assert.That(str, Does.Contain("unique"));
        }

        #endregion

        #region TableDefinition Tests

        [Test]
        public void TableDefinition_MigrateFromLegacyIndices_ConvertsCorrectly()
        {
            var tableDef = new TableDefinition();
            var legacyIndices = new List<string> { "col1", "col2", "col3" };

            tableDef.MigrateFromLegacyIndices(legacyIndices);

            Assert.That(tableDef.Indices.Count, Is.EqualTo(3));
            Assert.That(tableDef.Indices[0].Name, Is.EqualTo("col1"));
            Assert.That(tableDef.Indices[1].Name, Is.EqualTo("col2"));
            Assert.That(tableDef.Indices[2].Name, Is.EqualTo("col3"));
        }

        [Test]
        public void TableDefinition_MigrateFromLegacyIndices_NullInput_DoesNotThrow()
        {
            var tableDef = new TableDefinition();
            Assert.DoesNotThrow(() => tableDef.MigrateFromLegacyIndices(null));
        }

        [Test]
        public void TableDefinition_MigrateFromLegacyIndices_EmptyInput_DoesNotThrow()
        {
            var tableDef = new TableDefinition();
            Assert.DoesNotThrow(() => tableDef.MigrateFromLegacyIndices(new List<string>()));
        }

        #endregion

        #region LegacyTableDefinition Tests

        [Test]
        public void LegacyTableDefinition_ToTableDefinition_ConvertsCorrectly()
        {
            var legacy = new LegacyTableDefinition
            {
                tableName = "TestTable",
                isReadOnly = false,
                columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", Type = "int", IsPrimaryKey = true },
                    new ColumnDefinition { Name = "name", Type = "string" }
                },
                indices = new List<string> { "name" }
            };

            var converted = legacy.ToTableDefinition();

            Assert.That(converted.TableName, Is.EqualTo("TestTable"));
            Assert.That(converted.IsReadOnly, Is.False);
            Assert.That(converted.Columns.Count, Is.EqualTo(2));
            Assert.That(converted.Indices.Count, Is.EqualTo(1));
            Assert.That(converted.Indices[0].Name, Is.EqualTo("name"));
        }

        #endregion

        #region ClassGenerator Tests

        [Test]
        public void ClassGenerator_GenerateRecord_IncludesSecondaryKeyAttribute()
        {
            var tableDef = new TableDefinition
            {
                TableName = "test_table",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", Type = "int", IsPrimaryKey = true },
                    new ColumnDefinition { Name = "category", Type = "string" }
                },
                Indices = new List<IndexDefinition>
                {
                    IndexDefinition.FromSingleColumn("category")
                }
            };

            var code = ClassGenerator.GenerateRecord(tableDef);

            Assert.That(code, Does.Contain("SecondaryKey"));
        }

        [Test]
        public void ClassGenerator_GenerateRecord_CompositeIndex_IncludesOrderAndName()
        {
            var tableDef = new TableDefinition
            {
                TableName = "test_table",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", Type = "int", IsPrimaryKey = true },
                    new ColumnDefinition { Name = "category", Type = "string" },
                    new ColumnDefinition { Name = "price", Type = "int" }
                },
                Indices = new List<IndexDefinition>
                {
                    IndexDefinition.FromComposite("CategoryPrice", "category", "price")
                }
            };

            var code = ClassGenerator.GenerateRecord(tableDef);

            Assert.That(code, Does.Contain("SecondaryKey(\"CategoryPrice\", 0)"));
            Assert.That(code, Does.Contain("SecondaryKey(\"CategoryPrice\", 1)"));
        }

        [Test]
        public void ClassGenerator_GenerateRecord_UniqueIndex_IncludesAllowDuplicatesFalse()
        {
            var tableDef = new TableDefinition
            {
                TableName = "test_table",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", Type = "int", IsPrimaryKey = true },
                    new ColumnDefinition { Name = "code", Type = "string" }
                },
                Indices = new List<IndexDefinition>
                {
                    new IndexDefinition("code") { AllowDuplicates = false }
                }
            };

            var code = ClassGenerator.GenerateRecord(tableDef);

            Assert.That(code, Does.Contain("AllowDuplicates = false"));
        }

        #endregion
    }
}
