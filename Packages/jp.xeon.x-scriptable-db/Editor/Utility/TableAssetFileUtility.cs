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
    /// Database登録不要のインポート/エクスポートユーティリティ。
    /// AssetDatabaseから直接ITableAssetをスキャンして処理する。
    /// </summary>
    public static class TableAssetFileUtility
    {
        /// <summary>
        /// プロジェクト内の全ITableAssetをスキャンして返す。
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
        /// プロジェクト内全TableAssetをエクスポートする。
        /// </summary>
        public static void ExportAll(string extension)
        {
            var folderPath = EditorUtility.SaveFolderPanel(
                "エクスポート先フォルダを選択",
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

            ShowResultDialog("エクスポート完了", successList, failedList);
        }

        /// <summary>
        /// フォルダ内ファイルをプロジェクト内TableAssetに一括インポートする。
        /// </summary>
        public static void ImportAll(string extension)
        {
            var folderPath = EditorUtility.OpenFolderPanel(
                "インポート元フォルダを選択",
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
                    skippedList.Add($"{file.Name} (テーブルが見つかりません)");
                    continue;
                }

                if (targetTable is not IImportable importable)
                {
                    skippedList.Add($"{file.Name} (IImportable未実装)");
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
        /// 単一テーブルをエクスポートする。
        /// </summary>
        public static bool ExportSingle(ScriptableObject table, string filePath, Encoding encoding = null)
        {
            if (table is not IExportable exporter)
            {
                Debug.LogWarning($"{table.name} はIExportableを実装していません");
                return false;
            }

            exporter.Export(filePath, encoding ?? Encoding.UTF8);
            return true;
        }

        /// <summary>
        /// 単一テーブルをインポートする。
        /// </summary>
        public static bool ImportSingle(ScriptableObject table, string filePath)
        {
            if (table is not IImportable importable)
            {
                Debug.LogWarning($"{table.name} はIImportableを実装していません");
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
                message.AppendLine($"成功 ({successList.Count}件):");
                foreach (var item in successList)
                    message.AppendLine($"  - {item}");
            }

            if (failedList.Count > 0)
            {
                if (message.Length > 0)
                    message.AppendLine();
                message.AppendLine($"失敗 ({failedList.Count}件):");
                foreach (var item in failedList)
                    message.AppendLine($"  - {item}");
            }

            if (successList.Count == 0 && failedList.Count == 0)
                message.AppendLine("対象のテーブルがありませんでした。");

            EditorUtility.DisplayDialog(title, message.ToString(), "OK");
        }

        private static void ShowImportResultDialog(List<string> successList, List<string> failedList, List<string> skippedList)
        {
            var message = new StringBuilder();

            if (successList.Count > 0)
            {
                message.AppendLine($"成功 ({successList.Count}件):");
                foreach (var item in successList)
                    message.AppendLine($"  - {item}");
            }

            if (failedList.Count > 0)
            {
                if (message.Length > 0)
                    message.AppendLine();
                message.AppendLine($"失敗 ({failedList.Count}件):");
                foreach (var item in failedList)
                    message.AppendLine($"  - {item}");
            }

            if (skippedList.Count > 0)
            {
                if (message.Length > 0)
                    message.AppendLine();
                message.AppendLine($"スキップ ({skippedList.Count}件):");
                foreach (var item in skippedList)
                    message.AppendLine($"  - {item}");
            }

            if (successList.Count == 0 && failedList.Count == 0 && skippedList.Count == 0)
                message.AppendLine("対象のファイルがありませんでした。");

            EditorUtility.DisplayDialog("インポート完了", message.ToString(), "OK");
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
