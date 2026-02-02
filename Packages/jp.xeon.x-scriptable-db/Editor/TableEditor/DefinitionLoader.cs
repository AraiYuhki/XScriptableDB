using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Xeon.XScriptableDB.Editor
{
    public class DefinitionLoader
    {
        public static TableDefinition LoadDefinition(string path)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var yamlText = File.ReadAllText(path);
            try
            {
                var definition = deserializer.Deserialize<TableDefinition>(yamlText);
                return definition;
            }
            catch (YamlException)
            {
                var legacy = deserializer.Deserialize<LegacyTableDefinition>(yamlText);
                var definition = new TableDefinition
                {
                    TableName = legacy.TableName,
                    IsReadOnly = legacy.IsReadOnly,
                    Columns = legacy.Columns ?? new()
                };
                definition.MigrateFromLegacyIndices(legacy.Indices);
                return definition;
            }
        }

        public static void ExportYAML(TableDefinition definition, string savePath)
        {
            var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            var outputText = serializer.Serialize(definition);
            File.WriteAllText(savePath, outputText);
        }

        [System.Serializable]
        private class LegacyTableDefinition
        {
            public string TableName { get; set; }
            public bool IsReadOnly { get; set; } = true;
            public System.Collections.Generic.List<ColumnDefinition> Columns { get; set; }
            public System.Collections.Generic.List<string> Indices { get; set; }
        }
    }
}
