using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// YAMLファイルからC#コードを一括生成するユーティリティ。
    /// </summary>
    public static class YamlBatchConverter
    {
        [MenuItem("Tools/XScriptableDB/Generate C# from YAML folder")]
        public static void GenerateFromFolder()
        {
            var folderPath = EditorUtility.OpenFolderPanel("YAMLファイルのあるフォルダを選択", Application.dataPath, "");
            if (string.IsNullOrEmpty(folderPath))
                return;

            var yamlFiles = Directory.GetFiles(folderPath, "*.yaml")
                .Concat(Directory.GetFiles(folderPath, "*.yml"))
                .ToArray();

            if (yamlFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("エラー", "YAMLファイルが見つかりませんでした。", "OK");
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
                "生成完了",
                $"成功: {successCount}件\n失敗: {errorCount}件",
                "OK");
        }

        [MenuItem("Tools/XScriptableDB/Generate C# from YAML file")]
        public static void GenerateFromFile()
        {
            var filePath = EditorUtility.OpenFilePanel("YAMLファイルを選択", Application.dataPath, "yaml,yml");
            if (string.IsNullOrEmpty(filePath))
                return;

            try
            {
                GenerateFromYaml(filePath);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("生成完了", "C#ファイルを生成しました。", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to generate from {filePath}: {e.Message}");
                EditorUtility.DisplayDialog("エラー", $"生成に失敗しました。\n{e.Message}", "OK");
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

            File.WriteAllText(tableFilePath, ClassGenerator.GenerateTable(definition));
            File.WriteAllText(recordFilePath, ClassGenerator.GenerateRecord(definition));

            Debug.Log($"Generated: {tableFilePath}, {recordFilePath}");
        }
    }
}
