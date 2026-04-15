using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Utility for batch generating C# code from YAML files.
    /// </summary>
    public static class YamlBatchConverter
    {
        [MenuItem("Tools/XScriptableDB/Generate C# from YAML folder")]
        public static void GenerateFromFolder()
        {
            var folderPath = EditorUtility.OpenFolderPanel("Select folder containing YAML files", Application.dataPath, "");
            if (string.IsNullOrEmpty(folderPath))
                return;

            var yamlFiles = Directory.GetFiles(folderPath, "*.yaml")
                .Concat(Directory.GetFiles(folderPath, "*.yml"))
                .ToArray();

            if (yamlFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "No YAML files were found.", "OK");
                return;
            }

            var successCount = 0;
            var errorCount = 0;

            foreach (var yamlFile in yamlFiles)
            {
                try
                {
                    GenerateFromYaml(yamlFile);
                    successCount++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to generate from {yamlFile}: {e.Message}");
                    errorCount++;
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Generation Complete",
                $"Success: {successCount}\nFailed: {errorCount}",
                "OK");
        }

        [MenuItem("Tools/XScriptableDB/Generate C# from YAML file")]
        public static void GenerateFromFile()
        {
            var filePath = EditorUtility.OpenFilePanel("Select YAML file", Application.dataPath, "yaml,yml");
            if (string.IsNullOrEmpty(filePath))
                return;

            try
            {
                GenerateFromYaml(filePath);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Generation Complete", "C# files were generated.", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to generate from {filePath}: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Generation failed.\n{e.Message}", "OK");
            }
        }

        private static void GenerateFromYaml(string yamlFilePath)
        {
            var definition = DefinitionLoader.LoadDefinition(yamlFilePath);
            var savePath = TableGenerateSetting.Instance.SavePath;
            var className = definition.TableName.SnakeToPascalCase();

            var directoryPath = Path.Combine(Application.dataPath, savePath);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            var tableFilePath = Path.Combine(directoryPath, $"{className}Table.cs");
            var recordFilePath = Path.Combine(directoryPath, $"{className}Record.cs");

            File.WriteAllText(tableFilePath, ClassGenerator.GenerateTable(definition, TableGenerateSetting.Instance.NamespaceName));
            File.WriteAllText(recordFilePath, ClassGenerator.GenerateRecord(definition, TableGenerateSetting.Instance.NamespaceName));

            Debug.Log($"Generated: {tableFilePath}, {recordFilePath}");
        }
    }
}
