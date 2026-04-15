using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Xeon.XScriptableDB.Editor
{
    public class DefinitionLoader
    {
        /// <summary>
        /// Loads a table definition from a YAML file.
        /// Also supports automatic migration from the legacy format (indices: string[]).
        /// </summary>
        public static TableDefinition LoadDefinition(string path)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var yamlText = File.ReadAllText(path);

            try
            {
                return deserializer.Deserialize<TableDefinition>(yamlText);
            }
            catch (YamlException)
            {
                var legacy = deserializer.Deserialize<LegacyTableDefinition>(yamlText);
                return legacy.ToTableDefinition();
            }
        }

        public static void ExportYAML(TableDefinition definition, string savePath)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var outputText = serializer.Serialize(definition);
            File.WriteAllText(savePath, outputText);
        }
    }
}
