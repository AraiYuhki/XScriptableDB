using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Import/export utility that does not require database registration.
    /// Scans and processes ITableAsset directly from AssetDatabase.
    /// </summary>
    public static class TableAssetFileUtility
    {
        /// <summary>
        /// Scans and returns all ITableAssets in the project.
        /// </summary>
        private static List<ScriptableObject> FindAllTableAssets()
        {
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            var results = new List<ScriptableObject>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset is ITableAsset)
                    results.Add(asset);
            }

            return results;
        }

        /// <summary>
        /// Exports all TableAssets in the project.
        /// </summary>
        public static void ExportAll(string extension)
        {
            var folderPath = EditorUtility.SaveFolderPanel(
                "Select export destination folder",
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "Masters");
            if (string.IsNullOrEmpty(folderPath))
                return;

            var tables = FindAllTableAssets();
            var successList = new List<string>();
            var failedList = new List<string>();

            foreach (var table in tables)
            {
                if (table is not IExportable exporter)
                    continue;
                var fileName = table.name.ToSnakeCase();
                var filePath = Path.Combine(folderPath, fileName + extension);
                try
                {
                    exporter.Export(filePath, Encoding.UTF8);
                    successList.Add(table.name);
                    Debug.Log($"Exported: {filePath}");
                }
                catch (Exception e)
                {
                    failedList.Add($"{table.name}: {e.Message}");
                    Debug.LogError($"Export failed: {table.name} - {e.Message}");
                }
            }

            ShowResultDialog("Export Complete", successList, failedList);
        }

        /// <summary>
        /// Batch imports files from a folder into TableAssets in the project.
        /// </summary>
        public static void ImportAll(string extension)
        {
            var folderPath = EditorUtility.OpenFolderPanel(
                "Select import source folder",
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "Masters");
            if (string.IsNullOrEmpty(folderPath))
                return;

            var tables = FindAllTableAssets();
            var successList = new List<string>();
            var failedList = new List<string>();
            var skippedList = new List<string>();

            var directoryInfo = new DirectoryInfo(folderPath);
            var files = directoryInfo.GetFiles($"*{extension}", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                var tableName = Path.GetFileNameWithoutExtension(file.Name);
                tableName = tableName.SnakeToPascalCase();
                var targetTable = tables.FirstOrDefault(
                    table => tableName == table.GetType().Name || tableName == table.name);

                if (targetTable == null)
                {
                    skippedList.Add($"{file.Name} (table not found)");
                    continue;
                }

                if (targetTable is not IImportable importable)
                {
                    skippedList.Add($"{file.Name} (IImportable not implemented)");
                    continue;
                }

                try
                {
                    Debug.Log($"Importing: {file.Name} -> {targetTable.name}");
                    importable.Import(file.FullName);
                    EditorUtility.SetDirty(targetTable);
                    successList.Add($"{file.Name} -> {targetTable.name}");
                }
                catch (Exception e)
                {
                    failedList.Add($"{file.Name}: {e.Message}");
                    Debug.LogError($"Import failed: {file.Name} - {e.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ShowImportResultDialog(successList, failedList, skippedList);
        }

        /// <summary>
        /// Exports a single table.
        /// </summary>
        public static bool ExportSingle(ScriptableObject table, string filePath, Encoding encoding = null)
        {
            if (table is not IExportable exporter)
            {
                Debug.LogWarning($"{table.name} does not implement IExportable");
                return false;
            }

            exporter.Export(filePath, encoding ?? Encoding.UTF8);
            return true;
        }

        /// <summary>
        /// Imports a single table.
        /// </summary>
        public static bool ImportSingle(ScriptableObject table, string filePath)
        {
            if (table is not IImportable importable)
            {
                Debug.LogWarning($"{table.name} does not implement IImportable");
                return false;
            }

            importable.Import(filePath);
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static void ShowResultDialog(string title, List<string> successList, List<string> failedList)
        {
            var message = new StringBuilder();

            if (successList.Count > 0)
            {
                message.AppendLine($"Success ({successList.Count}):");
                foreach (var item in successList)
                    message.AppendLine($"  - {item}");
            }

            if (failedList.Count > 0)
            {
                if (message.Length > 0)
                    message.AppendLine();
                message.AppendLine($"Failed ({failedList.Count}):");
                foreach (var item in failedList)
                    message.AppendLine($"  - {item}");
            }

            if (successList.Count == 0 && failedList.Count == 0)
                message.AppendLine("No target tables were found.");

            EditorUtility.DisplayDialog(title, message.ToString(), "OK");
        }

        private static void ShowImportResultDialog(List<string> successList, List<string> failedList, List<string> skippedList)
        {
            var message = new StringBuilder();

            if (successList.Count > 0)
            {
                message.AppendLine($"Success ({successList.Count}):");
                foreach (var item in successList)
                    message.AppendLine($"  - {item}");
            }

            if (failedList.Count > 0)
            {
                if (message.Length > 0)
                    message.AppendLine();
                message.AppendLine($"Failed ({failedList.Count}):");
                foreach (var item in failedList)
                    message.AppendLine($"  - {item}");
            }

            if (skippedList.Count > 0)
            {
                if (message.Length > 0)
                    message.AppendLine();
                message.AppendLine($"Skipped ({skippedList.Count}):");
                foreach (var item in skippedList)
                    message.AppendLine($"  - {item}");
            }

            if (successList.Count == 0 && failedList.Count == 0 && skippedList.Count == 0)
                message.AppendLine("No target files were found.");

            EditorUtility.DisplayDialog("Import Complete", message.ToString(), "OK");
        }

        [MenuItem("Tools/XScriptableDB/Direct Export/CSV")]
        private static void DirectExportCsv() => ExportAll(".csv");

        [MenuItem("Tools/XScriptableDB/Direct Export/TSV")]
        private static void DirectExportTsv() => ExportAll(".tsv");

        [MenuItem("Tools/XScriptableDB/Direct Import/CSV")]
        private static void DirectImportCsv() => ImportAll(".csv");

        [MenuItem("Tools/XScriptableDB/Direct Import/TSV")]
        private static void DirectImportTsv() => ImportAll(".tsv");
    }
}
