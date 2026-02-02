using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// XScriptableDB コマンドラインインターフェース。
    /// Unity -batchmode で実行可能。
    /// </summary>
    public static class XScriptableDBCLI
    {
        /// <summary>
        /// エクスポートコマンド。
        /// 使用例: Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.Export -table=ItemTable -output=./export.csv
        /// </summary>
        public static void Export()
        {
            var args = ParseCommandLineArgs();

            if (!args.TryGetValue("table", out var tableName))
            {
                LogError("Missing required argument: -table");
                EditorApplication.Exit(1);
                return;
            }

            if (!args.TryGetValue("output", out var outputPath))
            {
                outputPath = $"./{tableName}.csv";
            }

            var format = args.GetValueOrDefault("format", "csv").ToLower();

            try
            {
                var table = FindTable(tableName);
                if (table == null)
                {
                    LogError($"Table not found: {tableName}");
                    EditorApplication.Exit(1);
                    return;
                }

                ExportTable(table, outputPath, format);
                Log($"Export completed: {outputPath}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                LogError($"Export failed: {ex.Message}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// インポートコマンド。
        /// 使用例: Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.Import -table=ItemTable -input=./data.csv
        /// </summary>
        public static void Import()
        {
            var args = ParseCommandLineArgs();

            if (!args.TryGetValue("table", out var tableName))
            {
                LogError("Missing required argument: -table");
                EditorApplication.Exit(1);
                return;
            }

            if (!args.TryGetValue("input", out var inputPath))
            {
                LogError("Missing required argument: -input");
                EditorApplication.Exit(1);
                return;
            }

            if (!File.Exists(inputPath))
            {
                LogError($"Input file not found: {inputPath}");
                EditorApplication.Exit(1);
                return;
            }

            try
            {
                var table = FindTable(tableName);
                if (table == null)
                {
                    LogError($"Table not found: {tableName}");
                    EditorApplication.Exit(1);
                    return;
                }

                ImportTable(table, inputPath);
                Log($"Import completed: {inputPath}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                LogError($"Import failed: {ex.Message}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// バックアップコマンド。
        /// 使用例: Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.Backup -table=ItemTable
        /// </summary>
        public static void Backup()
        {
            var args = ParseCommandLineArgs();

            if (!args.TryGetValue("table", out var tableName))
            {
                // 全テーブルをバックアップ
                BackupAllTables();
                return;
            }

            try
            {
                var table = FindTable(tableName);
                if (table == null)
                {
                    LogError($"Table not found: {tableName}");
                    EditorApplication.Exit(1);
                    return;
                }

                var name = args.GetValueOrDefault("name", null);
                var backup = BackupManager.CreateBackup(table, name);
                Log($"Backup created: {backup.Name}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                LogError($"Backup failed: {ex.Message}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// バリデーションコマンド。
        /// 使用例: Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.Validate -table=ItemTable
        /// </summary>
        public static void Validate()
        {
            var args = ParseCommandLineArgs();
            var hasErrors = false;

            if (args.TryGetValue("table", out var tableName))
            {
                var table = FindTable(tableName);
                if (table == null)
                {
                    LogError($"Table not found: {tableName}");
                    EditorApplication.Exit(1);
                    return;
                }

                hasErrors = !ValidateTable(table);
            }
            else
            {
                // 全テーブルをバリデーション
                foreach (var table in FindAllTables())
                {
                    if (!ValidateTable(table))
                        hasErrors = true;
                }
            }

            EditorApplication.Exit(hasErrors ? 1 : 0);
        }

        /// <summary>
        /// SQL実行コマンド。
        /// 使用例: Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.Query -sql="SELECT * FROM Items WHERE Price > 100"
        /// </summary>
        public static void Query()
        {
            var args = ParseCommandLineArgs();

            if (!args.TryGetValue("sql", out var sql))
            {
                LogError("Missing required argument: -sql");
                EditorApplication.Exit(1);
                return;
            }

            try
            {
                var executor = new SqlExecutor();

                // 全テーブルを登録
                foreach (var table in FindAllTables())
                {
                    executor.RegisterTable(table.GetType().Name, table);
                }

                var result = executor.Execute(sql);

                if (!result.IsSuccess)
                {
                    LogError($"Query failed: {result.ErrorMessage}");
                    EditorApplication.Exit(1);
                    return;
                }

                Log($"Query executed successfully. Records: {result.Records.Count}");

                // 結果をJSON形式で出力
                if (args.TryGetValue("output", out var outputPath))
                {
                    var json = JsonUtility.ToJson(new QueryResultWrapper { Records = result.Records.Select(r => r.ToString()).ToList() }, true);
                    File.WriteAllText(outputPath, json);
                    Log($"Results saved to: {outputPath}");
                }
                else
                {
                    foreach (var record in result.Records.Take(10))
                    {
                        Log($"  {record}");
                    }

                    if (result.Records.Count > 10)
                        Log($"  ... and {result.Records.Count - 10} more records");
                }

                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                LogError($"Query failed: {ex.Message}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// テーブル一覧コマンド。
        /// 使用例: Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.ListTables
        /// </summary>
        public static void ListTables()
        {
            var tables = FindAllTables();

            Log($"Found {tables.Count} tables:");
            foreach (var table in tables)
            {
                Log($"  - {table.GetType().Name}: {table.Count} records ({table.RecordType.Name})");
            }

            EditorApplication.Exit(0);
        }

        /// <summary>
        /// スキーマ情報コマンド。
        /// 使用例: Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.Schema -table=ItemTable
        /// </summary>
        public static void Schema()
        {
            var args = ParseCommandLineArgs();

            if (!args.TryGetValue("table", out var tableName))
            {
                LogError("Missing required argument: -table");
                EditorApplication.Exit(1);
                return;
            }

            var table = FindTable(tableName);
            if (table == null)
            {
                LogError($"Table not found: {tableName}");
                EditorApplication.Exit(1);
                return;
            }

            var summary = SchemaComparer.GenerateSchemaSummary(table.RecordType);
            Log(summary);

            EditorApplication.Exit(0);
        }

        /// <summary>
        /// ヘルプコマンド。
        /// </summary>
        public static void Help()
        {
            Log("XScriptableDB CLI - Command Line Interface");
            Log("");
            Log("Available commands:");
            Log("  Export   - Export table data to CSV/JSON");
            Log("           -table=<name>  Table name (required)");
            Log("           -output=<path> Output file path");
            Log("           -format=<csv|json> Output format (default: csv)");
            Log("");
            Log("  Import   - Import data from CSV file");
            Log("           -table=<name>  Table name (required)");
            Log("           -input=<path>  Input file path (required)");
            Log("");
            Log("  Backup   - Create backup");
            Log("           -table=<name>  Table name (optional, all tables if omitted)");
            Log("           -name=<name>   Backup name");
            Log("");
            Log("  Validate - Validate table data");
            Log("           -table=<name>  Table name (optional, all tables if omitted)");
            Log("");
            Log("  Query    - Execute SQL query");
            Log("           -sql=<query>   SQL query (required)");
            Log("           -output=<path> Output file path for results");
            Log("");
            Log("  ListTables - List all tables");
            Log("");
            Log("  Schema   - Show table schema");
            Log("           -table=<name>  Table name (required)");

            EditorApplication.Exit(0);
        }

        private static Dictionary<string, string> ParseCommandLineArgs()
        {
            var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var commandLineArgs = Environment.GetCommandLineArgs();

            foreach (var arg in commandLineArgs)
            {
                if (arg.StartsWith("-") && arg.Contains("="))
                {
                    var parts = arg[1..].Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        args[parts[0]] = parts[1].Trim('"', '\'');
                    }
                }
            }

            return args;
        }

        private static ITableAsset FindTable(string tableName)
        {
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset is ITableAsset tableAsset &&
                    tableAsset.GetType().Name.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                {
                    return tableAsset;
                }
            }
            return null;
        }

        private static List<ITableAsset> FindAllTables()
        {
            var tables = new List<ITableAsset>();
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset is ITableAsset tableAsset)
                    tables.Add(tableAsset);
            }

            return tables;
        }

        private static void ExportTable(ITableAsset table, string outputPath, string format)
        {
            if (table is IExportable exportable)
            {
                exportable.Export(outputPath);
            }
            else
            {
                // 汎用エクスポート
                var records = new List<object>();
                foreach (var record in table.Records)
                {
                    if (record != null)
                        records.Add(record);
                }

                if (format == "json")
                {
                    var json = JsonUtility.ToJson(new { Records = records }, true);
                    File.WriteAllText(outputPath, json);
                }
                else
                {
                    var csv = IO.CsvParser.ToCSV(records, table.RecordType);
                    File.WriteAllText(outputPath, csv);
                }
            }
        }

        private static void ImportTable(ITableAsset table, string inputPath)
        {
            if (table is IImportable importable)
            {
                importable.Import(inputPath);
            }
            else
            {
                throw new NotSupportedException($"Table {table.GetType().Name} does not support import");
            }

            EditorUtility.SetDirty(table as UnityEngine.Object);
            AssetDatabase.SaveAssets();
        }

        private static void BackupAllTables()
        {
            var tables = FindAllTables();
            var successCount = 0;

            foreach (var table in tables)
            {
                try
                {
                    BackupManager.CreateBackup(table);
                    successCount++;
                }
                catch (Exception ex)
                {
                    LogError($"Backup failed for {table.GetType().Name}: {ex.Message}");
                }
            }

            Log($"Backup completed: {successCount}/{tables.Count} tables");
            EditorApplication.Exit(successCount == tables.Count ? 0 : 1);
        }

        private static bool ValidateTable(ITableAsset table)
        {
            var tableName = table.GetType().Name;
            var hasErrors = false;

            // 重複キーのチェック
            var duplicates = table.FindDuplicateKeysAsObjects();
            if (duplicates.Count > 0)
            {
                LogError($"[{tableName}] Found {duplicates.Count} duplicate keys");
                hasErrors = true;
            }

            // nullレコードのチェック
            var nullCount = 0;
            foreach (var record in table.Records)
            {
                if (record == null)
                    nullCount++;
            }

            if (nullCount > 0)
            {
                LogError($"[{tableName}] Found {nullCount} null records");
                hasErrors = true;
            }

            if (!hasErrors)
            {
                Log($"[{tableName}] Validation passed ({table.Count} records)");
            }

            return !hasErrors;
        }

        private static void Log(string message)
        {
            Debug.Log($"[XScriptableDB CLI] {message}");
            Console.WriteLine(message);
        }

        private static void LogError(string message)
        {
            Debug.LogError($"[XScriptableDB CLI] {message}");
            Console.Error.WriteLine($"ERROR: {message}");
        }

        [Serializable]
        private class QueryResultWrapper
        {
            public List<string> Records;
        }
    }
}
