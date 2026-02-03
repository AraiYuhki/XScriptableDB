using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Xeon.XScriptableDB.Editor;

namespace Xeon.XScriptableDB.Tests.Editor
{
    /// <summary>
    /// TableEditor関連クラスのテスト。
    /// </summary>
    public class TableEditorTests
    {
        #region ColumnDefinition Tests

        [Test]
        public void ColumnDefinition_Properties_GetSet()
        {
            var column = new ColumnDefinition
            {
                Name = "test_column",
                Type = "int",
                IsNullable = true,
                IsPrimaryKey = false
            };

            Assert.AreEqual("test_column", column.Name);
            Assert.AreEqual("int", column.Type);
            Assert.IsTrue(column.IsNullable);
            Assert.IsFalse(column.IsPrimaryKey);
        }

        [Test]
        public void ColumnDefinition_ToString_ContainsNameAndType()
        {
            var column = new ColumnDefinition
            {
                Name = "id",
                Type = "int",
                IsPrimaryKey = true
            };

            var str = column.ToString();
            Assert.IsTrue(str.Contains("id"));
            Assert.IsTrue(str.Contains("int"));
        }

        #endregion

        #region IndexDefinition Tests

        [Test]
        public void IndexDefinition_SingleColumn_IsNotComposite()
        {
            var index = IndexDefinition.FromSingleColumn("category");

            Assert.AreEqual("category", index.Name);
            Assert.AreEqual(1, index.Columns.Count);
            Assert.AreEqual("category", index.Columns[0]);
            Assert.IsFalse(index.IsComposite);
        }

        [Test]
        public void IndexDefinition_MultipleColumns_IsComposite()
        {
            var index = IndexDefinition.FromComposite("category_type", "category", "type");

            Assert.AreEqual("category_type", index.Name);
            Assert.AreEqual(2, index.Columns.Count);
            Assert.AreEqual("category", index.Columns[0]);
            Assert.AreEqual("type", index.Columns[1]);
            Assert.IsTrue(index.IsComposite);
        }

        [Test]
        public void IndexDefinition_FromLegacy_CreatesSingleColumnIndex()
        {
            var index = IndexDefinition.FromLegacy("old_index");

            Assert.AreEqual("old_index", index.Name);
            Assert.AreEqual(1, index.Columns.Count);
            Assert.IsFalse(index.IsComposite);
        }

        [Test]
        public void IndexDefinition_AllowDuplicates_DefaultIsTrue()
        {
            var index = new IndexDefinition("test");
            Assert.IsTrue(index.AllowDuplicates);
        }

        [Test]
        public void IndexDefinition_AllowDuplicates_CanBeSetToFalse()
        {
            var index = new IndexDefinition("test") { AllowDuplicates = false };
            Assert.IsFalse(index.AllowDuplicates);
        }

        [Test]
        public void IndexDefinition_ToString_ContainsNameAndColumns()
        {
            var index = IndexDefinition.FromComposite("composite_key", "col1", "col2");
            index.AllowDuplicates = false;

            var str = index.ToString();
            Assert.IsTrue(str.Contains("composite_key"));
            Assert.IsTrue(str.Contains("col1"));
            Assert.IsTrue(str.Contains("col2"));
            Assert.IsTrue(str.Contains("unique"));
        }

        [Test]
        public void IndexDefinition_Columns_SetNull_BecomesEmptyList()
        {
            var index = new IndexDefinition("test");
            index.Columns = null;

            Assert.IsNotNull(index.Columns);
            Assert.AreEqual(0, index.Columns.Count);
        }

        #endregion

        #region TableDefinition Tests

        [Test]
        public void TableDefinition_Properties_GetSet()
        {
            var definition = new TableDefinition
            {
                TableName = "item_master",
                IsReadOnly = true
            };

            Assert.AreEqual("item_master", definition.TableName);
            Assert.IsTrue(definition.IsReadOnly);
        }

        [Test]
        public void TableDefinition_MigrateFromLegacyIndices_ConvertsToIndexDefinitions()
        {
            var definition = new TableDefinition();
            var legacyIndices = new List<string> { "category", "type", "rarity" };

            definition.MigrateFromLegacyIndices(legacyIndices);

            Assert.AreEqual(3, definition.Indices.Count);
            Assert.AreEqual("category", definition.Indices[0].Name);
            Assert.AreEqual("type", definition.Indices[1].Name);
            Assert.AreEqual("rarity", definition.Indices[2].Name);
        }

        [Test]
        public void TableDefinition_MigrateFromLegacyIndices_NullOrEmpty_DoesNothing()
        {
            var definition = new TableDefinition();
            definition.Indices = new List<IndexDefinition> { new IndexDefinition("existing") };

            definition.MigrateFromLegacyIndices(null);
            Assert.AreEqual(1, definition.Indices.Count);

            definition.MigrateFromLegacyIndices(new List<string>());
            Assert.AreEqual(1, definition.Indices.Count);
        }

        [Test]
        public void TableDefinition_ToString_ContainsTableName()
        {
            var definition = new TableDefinition { TableName = "test_table" };
            var str = definition.ToString();
            Assert.IsTrue(str.Contains("test_table"));
        }

        #endregion

        #region ClassGenerator Tests

        [Test]
        public void ClassGenerator_GenerateTable_ContainsClassName()
        {
            var definition = CreateTestTableDefinition();
            var code = ClassGenerator.GenerateTable(definition);

            Assert.IsTrue(code.Contains("ItemMasterTable"));
            Assert.IsTrue(code.Contains("ItemMasterRecord"));
            Assert.IsTrue(code.Contains("TableAsset<"));
        }

        [Test]
        public void ClassGenerator_GenerateTable_ContainsPrimaryKeyType()
        {
            var definition = CreateTestTableDefinition();
            var code = ClassGenerator.GenerateTable(definition);

            Assert.IsTrue(code.Contains("int"));
        }

        [Test]
        public void ClassGenerator_GenerateRecord_ContainsFieldsAndProperties()
        {
            var definition = CreateTestTableDefinition();
            var code = ClassGenerator.GenerateRecord(definition);

            Assert.IsTrue(code.Contains("ItemMasterRecord"));
            Assert.IsTrue(code.Contains("SerializeField"));
            Assert.IsTrue(code.Contains("PrimaryKey"));
            Assert.IsTrue(code.Contains("CsvColumn("));
        }

        [Test]
        public void ClassGenerator_GenerateRecord_ReadOnly_HasEditorOnlySetter()
        {
            var definition = CreateTestTableDefinition();
            definition.IsReadOnly = true;
            var code = ClassGenerator.GenerateRecord(definition);

            Assert.IsTrue(code.Contains("#if UNITY_EDITOR"));
        }

        [Test]
        public void ClassGenerator_GenerateRecord_NotReadOnly_HasPublicSetter()
        {
            var definition = CreateTestTableDefinition();
            definition.IsReadOnly = false;
            var code = ClassGenerator.GenerateRecord(definition);

            Assert.IsFalse(code.Contains("#if UNITY_EDITOR"));
            Assert.IsTrue(code.Contains("set =>"));
        }

        [Test]
        public void ClassGenerator_GenerateRecord_SingleSecondaryKey_GeneratesAttribute()
        {
            var definition = CreateTestTableDefinition();
            definition.Indices = new List<IndexDefinition>
            {
                IndexDefinition.FromSingleColumn("category")
            };

            var code = ClassGenerator.GenerateRecord(definition);
            // SecondaryKey is part of combined attribute list like [SerializeField, CsvColumn("category"), SecondaryKey]
            Assert.IsTrue(code.Contains("SecondaryKey"));
        }

        [Test]
        public void ClassGenerator_GenerateRecord_CompositeSecondaryKey_GeneratesAttributeWithOrder()
        {
            var definition = CreateTestTableDefinition();
            definition.Indices = new List<IndexDefinition>
            {
                IndexDefinition.FromComposite("category_type", "category", "type")
            };

            var code = ClassGenerator.GenerateRecord(definition);
            Assert.IsTrue(code.Contains("SecondaryKey(\"category_type\""));
        }

        [Test]
        public void ClassGenerator_GenerateRecord_NullableType_HasQuestionMark()
        {
            var definition = new TableDefinition
            {
                TableName = "test",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", Type = "int", IsPrimaryKey = true },
                    new ColumnDefinition { Name = "value", Type = "int", IsNullable = true }
                }
            };

            var code = ClassGenerator.GenerateRecord(definition);
            Assert.IsTrue(code.Contains("int?"));
        }

        [Test]
        public void ClassGenerator_TypeConversion_SqlTypes()
        {
            var testCases = new Dictionary<string, string>
            {
                { "tinyint", "byte" },
                { "smallint", "short" },
                { "integer", "int" },
                { "bigint", "long" },
                { "varchar", "string" },
                { "text", "string" },
                { "datetime", "SerializableDateTime" },
                { "boolean", "bool" }
            };

            foreach (var testCase in testCases)
            {
                var definition = new TableDefinition
                {
                    TableName = "test",
                    Columns = new List<ColumnDefinition>
                    {
                        new ColumnDefinition { Name = "id", Type = "int", IsPrimaryKey = true },
                        new ColumnDefinition { Name = "test_col", Type = testCase.Key }
                    }
                };

                var code = ClassGenerator.GenerateRecord(definition);
                Assert.IsTrue(code.Contains(testCase.Value),
                    $"Expected '{testCase.Value}' for SQL type '{testCase.Key}'");
            }
        }

        #endregion

        #region DefinitionLoader Tests

        [Test]
        public void DefinitionLoader_LoadDefinition_ValidYaml_ReturnsDefinition()
        {
            var tempPath = Path.GetTempFileName();
            try
            {
                var yamlContent = @"tableName: test_table
isReadOnly: true
columns:
  - name: id
    type: int
    isPrimaryKey: true
  - name: name
    type: varchar
indices:
  - name: name
    columns:
      - name
    allowDuplicates: true
";
                File.WriteAllText(tempPath, yamlContent);

                var definition = DefinitionLoader.LoadDefinition(tempPath);

                Assert.AreEqual("test_table", definition.TableName);
                Assert.IsTrue(definition.IsReadOnly);
                Assert.AreEqual(2, definition.Columns.Count);
                Assert.AreEqual("id", definition.Columns[0].Name);
                Assert.IsTrue(definition.Columns[0].IsPrimaryKey);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [Test]
        public void DefinitionLoader_LoadDefinition_LegacyFormat_MigratesIndices()
        {
            var tempPath = Path.GetTempFileName();
            try
            {
                var yamlContent = @"tableName: legacy_table
isReadOnly: true
columns:
  - name: id
    type: int
    isPrimaryKey: true
  - name: category
    type: varchar
indices:
  - category
";
                File.WriteAllText(tempPath, yamlContent);

                var definition = DefinitionLoader.LoadDefinition(tempPath);

                Assert.AreEqual("legacy_table", definition.TableName);
                Assert.AreEqual(1, definition.Indices.Count);
                Assert.AreEqual("category", definition.Indices[0].Name);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [Test]
        public void DefinitionLoader_ExportYAML_CreatesValidFile()
        {
            var tempPath = Path.GetTempFileName();
            try
            {
                var definition = new TableDefinition
                {
                    TableName = "exported_table",
                    IsReadOnly = false,
                    Columns = new List<ColumnDefinition>
                    {
                        new ColumnDefinition { Name = "id", Type = "int", IsPrimaryKey = true },
                        new ColumnDefinition { Name = "value", Type = "string" }
                    },
                    Indices = new List<IndexDefinition>
                    {
                        IndexDefinition.FromSingleColumn("value")
                    }
                };

                DefinitionLoader.ExportYAML(definition, tempPath);

                var content = File.ReadAllText(tempPath);
                Assert.IsTrue(content.Contains("exported_table"));
                Assert.IsTrue(content.Contains("isReadOnly: false"));
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [Test]
        public void DefinitionLoader_RoundTrip_PreservesData()
        {
            var tempPath = Path.GetTempFileName();
            try
            {
                var original = new TableDefinition
                {
                    TableName = "roundtrip_test",
                    IsReadOnly = true,
                    Columns = new List<ColumnDefinition>
                    {
                        new ColumnDefinition { Name = "id", Type = "int", IsPrimaryKey = true, IsNullable = false },
                        new ColumnDefinition { Name = "category", Type = "varchar", IsNullable = true }
                    },
                    Indices = new List<IndexDefinition>
                    {
                        IndexDefinition.FromSingleColumn("category")
                    }
                };

                DefinitionLoader.ExportYAML(original, tempPath);
                var loaded = DefinitionLoader.LoadDefinition(tempPath);

                Assert.AreEqual(original.TableName, loaded.TableName);
                Assert.AreEqual(original.IsReadOnly, loaded.IsReadOnly);
                Assert.AreEqual(original.Columns.Count, loaded.Columns.Count);
                Assert.AreEqual(original.Columns[0].Name, loaded.Columns[0].Name);
                Assert.AreEqual(original.Columns[0].IsPrimaryKey, loaded.Columns[0].IsPrimaryKey);
                Assert.AreEqual(original.Indices.Count, loaded.Indices.Count);
                Assert.AreEqual(original.Indices[0].Name, loaded.Indices[0].Name);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        #endregion

        #region Helper Methods

        private TableDefinition CreateTestTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "item_master",
                IsReadOnly = true,
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", Type = "int", IsPrimaryKey = true },
                    new ColumnDefinition { Name = "name", Type = "varchar" },
                    new ColumnDefinition { Name = "category", Type = "varchar" },
                    new ColumnDefinition { Name = "type", Type = "varchar" },
                    new ColumnDefinition { Name = "price", Type = "int" }
                },
                Indices = new List<IndexDefinition>()
            };
        }

        #endregion
    }
}
