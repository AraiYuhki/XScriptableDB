using System.IO;
using UnityEditor;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Xeon.XScriptableDB.Editor
{
    public class DefinitionLoader
    {
        [MenuItem("Tools/Load table definition from YAML")]
        public static void LoadDefinition()
        {
            string path = EditorUtility.OpenFilePanel("Select YAML definition file", Path.Join(Application.dataPath, "../"), "yaml,yml");
            if (string.IsNullOrEmpty(path))
                return;
            var definition = LoadDefinition(path);
            Debug.Log(ClassGenerator.GenerateTable(definition));
            Debug.Log(ClassGenerator.GenerateRecord(definition));
        }

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
