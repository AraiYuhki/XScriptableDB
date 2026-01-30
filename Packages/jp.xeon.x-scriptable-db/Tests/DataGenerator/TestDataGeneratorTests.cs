using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xeon.XScriptableDB.Editor;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// TestDataGenerator のテスト。
    /// </summary>
    public class TestDataGeneratorTests
    {
        #region Test Data Classes

        [Serializable]
        private class SimpleRecord
        {
            [PrimaryKey]
            public int Id;
            public string Name;
            public int Value;
        }

        [Serializable]
        private class ComplexRecord
        {
            [PrimaryKey]
            public int Id;
            public string ItemName;
            public int Price;
            public float Rate;
            public bool Active;
            public string Description;
            public int Level;
            public int CategoryId;
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

        #region GeneratorRule Enum Tests

        [Test]
        public void GeneratorRule_HasAllExpectedValues()
        {
            var values = Enum.GetValues(typeof(GeneratorRule));

            Assert.That(values.Length, Is.EqualTo(6));
            Assert.That(Enum.IsDefined(typeof(GeneratorRule), GeneratorRule.Sequential), Is.True);
            Assert.That(Enum.IsDefined(typeof(GeneratorRule), GeneratorRule.Random), Is.True);
            Assert.That(Enum.IsDefined(typeof(GeneratorRule), GeneratorRule.RandomRange), Is.True);
            Assert.That(Enum.IsDefined(typeof(GeneratorRule), GeneratorRule.RandomChoice), Is.True);
            Assert.That(Enum.IsDefined(typeof(GeneratorRule), GeneratorRule.Pattern), Is.True);
            Assert.That(Enum.IsDefined(typeof(GeneratorRule), GeneratorRule.Fixed), Is.True);
        }

        #endregion

        #region FieldGeneratorConfig Tests

        [Test]
        public void FieldGeneratorConfig_Initialization_HasCorrectDefaults()
        {
            var config = new FieldGeneratorConfig();

            Assert.That(config.FieldName, Is.Null);
            Assert.That(config.Rule, Is.EqualTo(GeneratorRule.Random));
            Assert.That(config.MinValue, Is.EqualTo("0"));
            Assert.That(config.MaxValue, Is.EqualTo("100"));
            Assert.That(config.Choices, Is.Not.Null);
            Assert.That(config.Choices.Count, Is.EqualTo(0));
            Assert.That(config.Pattern, Is.EqualTo("{0}"));
            Assert.That(config.FixedValue, Is.EqualTo(""));
            Assert.That(config.StartValue, Is.EqualTo(1));
        }

        [Test]
        public void FieldGeneratorConfig_SetProperties_StoresCorrectly()
        {
            var config = new FieldGeneratorConfig
            {
                FieldName = "TestField",
                Rule = GeneratorRule.Sequential,
                MinValue = "10",
                MaxValue = "200",
                Pattern = "Item_{0}",
                FixedValue = "Fixed",
                StartValue = 100
            };
            config.Choices.Add("Choice1");
            config.Choices.Add("Choice2");

            Assert.That(config.FieldName, Is.EqualTo("TestField"));
            Assert.That(config.Rule, Is.EqualTo(GeneratorRule.Sequential));
            Assert.That(config.MinValue, Is.EqualTo("10"));
            Assert.That(config.MaxValue, Is.EqualTo("200"));
            Assert.That(config.Choices.Count, Is.EqualTo(2));
            Assert.That(config.Pattern, Is.EqualTo("Item_{0}"));
            Assert.That(config.FixedValue, Is.EqualTo("Fixed"));
            Assert.That(config.StartValue, Is.EqualTo(100));
        }

        #endregion

        #region TableGeneratorConfig Tests

        [Test]
        public void TableGeneratorConfig_Initialization_HasCorrectDefaults()
        {
            var config = new TableGeneratorConfig();

            Assert.That(config.TableName, Is.Null);
            Assert.That(config.RecordCount, Is.EqualTo(100));
            Assert.That(config.FieldConfigs, Is.Not.Null);
            Assert.That(config.FieldConfigs.Count, Is.EqualTo(0));
        }

        [Test]
        public void TableGeneratorConfig_AddFieldConfig_StoresCorrectly()
        {
            var config = new TableGeneratorConfig
            {
                TableName = "TestTable",
                RecordCount = 50
            };
            config.FieldConfigs.Add(new FieldGeneratorConfig { FieldName = "Field1" });
            config.FieldConfigs.Add(new FieldGeneratorConfig { FieldName = "Field2" });

            Assert.That(config.TableName, Is.EqualTo("TestTable"));
            Assert.That(config.RecordCount, Is.EqualTo(50));
            Assert.That(config.FieldConfigs.Count, Is.EqualTo(2));
            Assert.That(config.FieldConfigs[0].FieldName, Is.EqualTo("Field1"));
            Assert.That(config.FieldConfigs[1].FieldName, Is.EqualTo("Field2"));
        }

        #endregion

        #region Generate Tests

        [Test]
        public void Generate_SimpleRecords_CreatesCorrectCount()
        {
            var table = new MockTableAsset<SimpleRecord>();

            var generated = TestDataGenerator.Generate(table, 10);

            Assert.That(generated, Is.EqualTo(10));
            Assert.That(table.Count, Is.EqualTo(10));
        }

        [Test]
        public void Generate_WithPrimaryKey_AssignsSequentialIds()
        {
            var table = new MockTableAsset<SimpleRecord>();

            TestDataGenerator.Generate(table, 5);

            var records = table.GetRecords();
            Assert.That(records[0].Id, Is.EqualTo(1));
            Assert.That(records[1].Id, Is.EqualTo(2));
            Assert.That(records[2].Id, Is.EqualTo(3));
            Assert.That(records[3].Id, Is.EqualTo(4));
            Assert.That(records[4].Id, Is.EqualTo(5));
        }

        [Test]
        public void Generate_MultipleTime_ContinuesIdSequence()
        {
            var table = new MockTableAsset<SimpleRecord>();

            TestDataGenerator.Generate(table, 3);
            TestDataGenerator.Generate(table, 2);

            var records = table.GetRecords();
            Assert.That(records.Count, Is.EqualTo(5));
            // 2回目の生成は既存の最大ID+1から始まる
            Assert.That(records[3].Id, Is.EqualTo(4));
            Assert.That(records[4].Id, Is.EqualTo(5));
        }

        [Test]
        public void Generate_NameField_GeneratesNonEmptyStrings()
        {
            var table = new MockTableAsset<SimpleRecord>();

            TestDataGenerator.Generate(table, 5);

            var records = table.GetRecords();
            foreach (var record in records)
            {
                Assert.That(record.Name, Is.Not.Null.And.Not.Empty);
            }
        }

        [Test]
        public void Generate_ValueField_GeneratesNumbers()
        {
            var table = new MockTableAsset<SimpleRecord>();

            TestDataGenerator.Generate(table, 5);

            var records = table.GetRecords();
            foreach (var record in records)
            {
                Assert.That(record.Value, Is.GreaterThanOrEqualTo(0));
            }
        }

        [Test]
        public void Generate_ComplexRecord_PopulatesAllFields()
        {
            var table = new MockTableAsset<ComplexRecord>();

            TestDataGenerator.Generate(table, 3);

            var records = table.GetRecords();
            foreach (var record in records)
            {
                Assert.That(record.Id, Is.GreaterThan(0));
                Assert.That(record.ItemName, Is.Not.Null);
                Assert.That(record.Price, Is.GreaterThanOrEqualTo(0));
            }
        }

        [Test]
        public void Generate_ZeroCount_CreatesNoRecords()
        {
            var table = new MockTableAsset<SimpleRecord>();

            var generated = TestDataGenerator.Generate(table, 0);

            Assert.That(generated, Is.EqualTo(0));
            Assert.That(table.Count, Is.EqualTo(0));
        }

        #endregion

        #region ClearTable Tests

        [Test]
        public void ClearTable_RemovesAllRecords()
        {
            var table = new MockTableAsset<SimpleRecord>();
            TestDataGenerator.Generate(table, 10);

            TestDataGenerator.ClearTable(table);

            Assert.That(table.Count, Is.EqualTo(0));
        }

        [Test]
        public void ClearTable_EmptyTable_DoesNotThrow()
        {
            var table = new MockTableAsset<SimpleRecord>();

            Assert.DoesNotThrow(() => TestDataGenerator.ClearTable(table));
            Assert.That(table.Count, Is.EqualTo(0));
        }

        #endregion

        #region CreateDefaultConfig Tests

        [Test]
        public void CreateDefaultConfig_ReturnsValidConfig()
        {
            var table = new MockTableAsset<SimpleRecord>();

            var config = TestDataGenerator.CreateDefaultConfig(table);

            Assert.That(config, Is.Not.Null);
            Assert.That(config.RecordCount, Is.EqualTo(100));
        }

        [Test]
        public void CreateDefaultConfig_IncludesAllFields()
        {
            var table = new MockTableAsset<SimpleRecord>();

            var config = TestDataGenerator.CreateDefaultConfig(table);

            Assert.That(config.FieldConfigs.Count, Is.EqualTo(3)); // Id, Name, Value
            Assert.That(config.FieldConfigs.Any(f => f.FieldName == "Id"), Is.True);
            Assert.That(config.FieldConfigs.Any(f => f.FieldName == "Name"), Is.True);
            Assert.That(config.FieldConfigs.Any(f => f.FieldName == "Value"), Is.True);
        }

        [Test]
        public void CreateDefaultConfig_PrimaryKeyField_HasSequentialRule()
        {
            var table = new MockTableAsset<SimpleRecord>();

            var config = TestDataGenerator.CreateDefaultConfig(table);

            var idConfig = config.FieldConfigs.First(f => f.FieldName == "Id");
            Assert.That(idConfig.Rule, Is.EqualTo(GeneratorRule.Sequential));
        }

        [Test]
        public void CreateDefaultConfig_NonPrimaryKeyField_HasRandomRule()
        {
            var table = new MockTableAsset<SimpleRecord>();

            var config = TestDataGenerator.CreateDefaultConfig(table);

            var nameConfig = config.FieldConfigs.First(f => f.FieldName == "Name");
            Assert.That(nameConfig.Rule, Is.EqualTo(GeneratorRule.Random));

            var valueConfig = config.FieldConfigs.First(f => f.FieldName == "Value");
            Assert.That(valueConfig.Rule, Is.EqualTo(GeneratorRule.Random));
        }

        [Test]
        public void CreateDefaultConfig_WithExistingRecords_StartsFromNextId()
        {
            var table = new MockTableAsset<SimpleRecord>();
            table.AddRecord(new SimpleRecord { Id = 1, Name = "Existing", Value = 100 });
            table.AddRecord(new SimpleRecord { Id = 2, Name = "Existing2", Value = 200 });

            var config = TestDataGenerator.CreateDefaultConfig(table);

            var idConfig = config.FieldConfigs.First(f => f.FieldName == "Id");
            Assert.That(idConfig.StartValue, Is.EqualTo(3)); // table.Count + 1
        }

        #endregion

        #region Generate With Config Tests

        [Test]
        public void Generate_WithSequentialConfig_GeneratesSequence()
        {
            var table = new MockTableAsset<SimpleRecord>();
            var config = new TableGeneratorConfig
            {
                TableName = "TestTable",
                RecordCount = 5,
                FieldConfigs = new List<FieldGeneratorConfig>
                {
                    new FieldGeneratorConfig
                    {
                        FieldName = "Value",
                        Rule = GeneratorRule.Sequential,
                        StartValue = 100
                    }
                }
            };

            TestDataGenerator.Generate(table, 5, config);

            var records = table.GetRecords();
            Assert.That(records[0].Value, Is.EqualTo(100));
            Assert.That(records[1].Value, Is.EqualTo(101));
            Assert.That(records[2].Value, Is.EqualTo(102));
        }

        [Test]
        public void Generate_WithFixedConfig_GeneratesSameValue()
        {
            var table = new MockTableAsset<SimpleRecord>();
            var config = new TableGeneratorConfig
            {
                TableName = "TestTable",
                FieldConfigs = new List<FieldGeneratorConfig>
                {
                    new FieldGeneratorConfig
                    {
                        FieldName = "Value",
                        Rule = GeneratorRule.Fixed,
                        FixedValue = "42"
                    }
                }
            };

            TestDataGenerator.Generate(table, 3, config);

            var records = table.GetRecords();
            Assert.That(records.All(r => r.Value == 42), Is.True);
        }

        [Test]
        public void Generate_WithPatternConfig_GeneratesFormattedStrings()
        {
            var table = new MockTableAsset<SimpleRecord>();
            var config = new TableGeneratorConfig
            {
                TableName = "TestTable",
                FieldConfigs = new List<FieldGeneratorConfig>
                {
                    new FieldGeneratorConfig
                    {
                        FieldName = "Name",
                        Rule = GeneratorRule.Pattern,
                        Pattern = "Item_{0}"
                    }
                }
            };

            TestDataGenerator.Generate(table, 3, config);

            var records = table.GetRecords();
            Assert.That(records[0].Name, Is.EqualTo("Item_1"));
            Assert.That(records[1].Name, Is.EqualTo("Item_2"));
            Assert.That(records[2].Name, Is.EqualTo("Item_3"));
        }

        [Test]
        public void Generate_WithRandomRangeConfig_GeneratesInRange()
        {
            var table = new MockTableAsset<SimpleRecord>();
            var config = new TableGeneratorConfig
            {
                TableName = "TestTable",
                FieldConfigs = new List<FieldGeneratorConfig>
                {
                    new FieldGeneratorConfig
                    {
                        FieldName = "Value",
                        Rule = GeneratorRule.RandomRange,
                        MinValue = "50",
                        MaxValue = "100"
                    }
                }
            };

            TestDataGenerator.Generate(table, 10, config);

            var records = table.GetRecords();
            foreach (var record in records)
            {
                Assert.That(record.Value, Is.GreaterThanOrEqualTo(50));
                Assert.That(record.Value, Is.LessThanOrEqualTo(100));
            }
        }

        [Test]
        public void Generate_WithRandomChoiceConfig_SelectsFromChoices()
        {
            var table = new MockTableAsset<SimpleRecord>();
            var config = new TableGeneratorConfig
            {
                TableName = "TestTable",
                FieldConfigs = new List<FieldGeneratorConfig>
                {
                    new FieldGeneratorConfig
                    {
                        FieldName = "Name",
                        Rule = GeneratorRule.RandomChoice,
                        Choices = new List<string> { "Alpha", "Beta", "Gamma" }
                    }
                }
            };

            TestDataGenerator.Generate(table, 10, config);

            var records = table.GetRecords();
            var validChoices = new HashSet<string> { "Alpha", "Beta", "Gamma" };
            foreach (var record in records)
            {
                Assert.That(validChoices.Contains(record.Name), Is.True);
            }
        }

        #endregion

        #region Field Name Inference Tests

        [Test]
        public void Generate_PriceField_GeneratesReasonableValues()
        {
            var table = new MockTableAsset<ComplexRecord>();

            TestDataGenerator.Generate(table, 5);

            var records = table.GetRecords();
            foreach (var record in records)
            {
                Assert.That(record.Price, Is.GreaterThanOrEqualTo(10));
                Assert.That(record.Price, Is.LessThanOrEqualTo(10000));
            }
        }

        [Test]
        public void Generate_LevelField_GeneratesSmallNumbers()
        {
            var table = new MockTableAsset<ComplexRecord>();

            TestDataGenerator.Generate(table, 5);

            var records = table.GetRecords();
            foreach (var record in records)
            {
                Assert.That(record.Level, Is.GreaterThanOrEqualTo(1));
                Assert.That(record.Level, Is.LessThanOrEqualTo(10));
            }
        }

        [Test]
        public void Generate_ActiveField_GeneratesBooleans()
        {
            var table = new MockTableAsset<ComplexRecord>();

            TestDataGenerator.Generate(table, 10);

            var records = table.GetRecords();
            // すべてbool値であることを確認（trueまたはfalse）
            foreach (var record in records)
            {
                Assert.That(record.Active, Is.TypeOf<bool>());
            }
        }

        [Test]
        public void Generate_DescriptionField_GeneratesStrings()
        {
            var table = new MockTableAsset<ComplexRecord>();

            TestDataGenerator.Generate(table, 5);

            var records = table.GetRecords();
            foreach (var record in records)
            {
                Assert.That(record.Description, Is.Not.Null);
                Assert.That(record.Description, Does.Contain("Description"));
            }
        }

        #endregion
    }
}
