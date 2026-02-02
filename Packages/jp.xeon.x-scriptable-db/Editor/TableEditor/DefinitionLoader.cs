using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Xeon.XScriptableDB.Editor
{
    public class DefinitionLoader
    {
        public static TableDefinition LoadDefinition(string path)
        {
            var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

            var yamlText = File.ReadAllText(path);
            var definition = deserializer.Deserialize<TableDefinition>(yamlText);

            return definition;
        }

        public static void ExportYAML(TableDefinition definition, string savePath)
        {
            var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            var outputText = serializer.Serialize(definition);
            File.WriteAllText(savePath, outputText);
        }

    }
}
