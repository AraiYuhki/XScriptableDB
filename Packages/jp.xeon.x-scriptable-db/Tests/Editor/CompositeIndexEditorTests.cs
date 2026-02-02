using System.IO;
using NUnit.Framework;
using Xeon.XScriptableDB.Editor;

namespace Xeon.XScriptableDB.Tests
{
    public class CompositeIndexEditorTests
    {
        [Test]
        public void IndexDefinition_MarksCompositeCorrectly()
        {
            var index = new IndexDefinition("RarityElement", "rarity", "element");

            Assert.That(index.IsComposite, Is.True);
            Assert.That(index.Columns.Count, Is.EqualTo(2));
        }

        [Test]
        public void ClassGenerator_GeneratesCompositeSecondaryKeyAttributes()
        {
            var definition = new TableDefinition
            {
                TableName = "equipment",
                Columns =
                {
                    new ColumnDefinition { Name = "id", Type = "int", IsPrimaryKey = true },
                    new ColumnDefinition { Name = "rarity", Type = "int" },
                    new ColumnDefinition { Name = "element", Type = "int" }
                },
                Indices =
                {
                    new IndexDefinition("RarityElement", "rarity", "element") { AllowDuplicates = false }
                }
            };

            var recordSource = ClassGenerator.GenerateRecord(definition);

            Assert.That(recordSource.Contains("SecondaryKey(\"RarityElement\", 0, AllowDuplicates = false)"), Is.True);
            Assert.That(recordSource.Contains("SecondaryKey(\"RarityElement\", 1, AllowDuplicates = false)"), Is.True);
        }

        [Test]
        public void DefinitionLoader_MigratesLegacyIndices()
        {
            var yaml = @"table_name: equipment
is_read_only: true
columns:
  - name: id
    type: int
    is_primary_key: true
  - name: category
    type: string
indices:
  - category
";
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, yaml);
                var definition = DefinitionLoader.LoadDefinition(path);

                Assert.That(definition.Indices.Count, Is.EqualTo(1));
                Assert.That(definition.Indices[0].Name, Is.EqualTo("category"));
                Assert.That(definition.Indices[0].Columns.Count, Is.EqualTo(1));
                Assert.That(definition.Indices[0].Columns[0], Is.EqualTo("category"));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}
