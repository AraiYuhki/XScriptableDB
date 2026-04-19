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
    /// Unity -batchmode で実行可能です。
    /// </summary>
    public static class XScriptableDBCLI
    {
        /// <summary>
        /// エクスポートコマンド。
        /// 使用法: Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.Export -table=ItemTable -output=./export.csv
        /// </summary>
        public static void Export()
        {
            var args = ParseCommandLineArgs();

            if (!args.TryGetValue("table", out var tableName))
            {
                LogError("引数が不足しています: -table");
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
                    LogError($"テーブルが見つかりません: {tableName}");
                    EditorApplication.Exit(1);
                    return;
                }

                ExportTable(table, outputPath, format);
                Log($"エクスポート完了: {outputPath}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                LogError($"エクスポート失敗: {ex.Message}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// インポートコマンド。
        /// 使用法: Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.Import -table=ItemTable -input=./data.csv
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
                LogError("引数が不足しています: -input");
                EditorApplication.Exit(1);
                return;
            }

            if (!File.Exists(inputPath))
            {
                LogError($"入力ファイルが見つかりません: {inputPath}");
                EditorApplication.Exit(1);
                return;
            }

            try
            {
                var table = FindTable(tableName);
                if (table == null)
                {
                    LogError($"テーブルが見つかりません: {tableName}");
                    EditorApplication.Exit(1);
                    return;
                }

                ImportTable(table, inputPath);
                Log($"インポート完了: {inputPath}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                LogError($"インポート失敗: {ex.Message}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// バックアップコマンド。
        /// 使用法: Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.Backup -table=ItemTable
        /// </summary>
        public static void Backup()
        {
            var args = ParseCommandLineArgs();

            if (!args.TryGetValue("table", out var tableName))
            {
                // すべてのテーブルをバックアップします
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
                Log($"バックアップ作成完了: {backup.Name}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                LogError($"バックアップ失敗: {ex.Message}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// バリデーションコマンド。
        /// 使用法: Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.Validate -table=ItemTable
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
                    LogError($"テーブルが見つかりません: {tableName}");
                    EditorApplication.Exit(1);
                    return;
                }

                hasErrors = !ValidateTable(table);
            }
            else
            {
                // すべてのテーブルをバリデーションします
                foreach (var table in FindAllTables())
                {
                    if (!ValidateTable(table))
                        hasErrors = true;
                }
            }

            EditorApplication.Exit(hasErrors ? 1 : 0);
        }

        /// <summary>
        /// SQLクエリコマンド。
        /// 使用法: Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.Query -sql="SELECT * FROM Items WHERE Price > 100"
        /// </summary>
        public static void Query()
        {
            var args = ParseCommandLineArgs();

            if (!args.TryGetValue("sql", out var sql))
            {
                LogError("引数が不足しています: -sql");
                EditorApplication.Exit(1);
                return;
            }

            try
            {
                var executor = new SqlExecutor();

                // すべてのテーブルを登録します
                foreach (var table in FindAllTables())
                {
                    executor.RegisterTable(table.GetType().Name, table);
                }

                var result = executor.Execute(sql);

                if (!result.IsSuccess)
                {
                    LogError($"クエリ失敗: {result.ErrorMessage}");
                    EditorApplication.Exit(1);
                    return;
                }

                Log($"クエリが正常に実行されました。レコード数: {result.Records.Count}");

                // Output results in JSON format
                if (args.TryGetValue("output", out var outputPath))
                {
                    var json = JsonUtility.ToJson(new QueryResultWrapper { Records = result.Records.Select(r => r.ToString()).ToList() }, true);
                    File.WriteAllText(outputPath, json);
                    Log($"結果を保存しました: {outputPath}");
                }
                else
                {
                    foreach (var record in result.Records.Take(10))
                    {
                        Log($"  {record}");
                    }

                    if (result.Records.Count > 10)
                        Log($"  ... 他 {result.Records.Count - 10} 件のレコード");
                }

                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                LogError($"クエリ失敗: {ex.Message}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// テーブル一覧表示コマンド。
        /// 使用法: Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.ListTables
        /// </summary>
        public static void ListTables()
        {
            var tables = FindAllTables();

            Log($"{tables.Count} 個のテーブルが見つかりました:");
            foreach (var table in tables)
            {
                Log($"  - {table.GetType().Name}: {table.Count} レコード ({table.RecordType.Name})");
            }

            EditorApplication.Exit(0);
        }

        /// <summary>
        /// スキーマ情報表示コマンド。
        /// 使用法: Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.Schema -table=ItemTable
        /// </summary>
        public static void Schema()
        {
            var args = ParseCommandLineArgs();

            if (!args.TryGetValue("table", out var tableName))
            {
                LogError("引数が不足しています: -table");
                EditorApplication.Exit(1);
                return;
            }

            var table = FindTable(tableName);
            if (table == null)
            {
                LogError($"テーブルが見つかりません: {tableName}");
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
            Log("XScriptableDB CLI - コマンドラインインターフェース");
            Log("");
            Log("利用可能なコマンド:");
            Log("  Export   - テーブルデータをCSV/JSONにエクスポートします");
            Log("           -table=<name>  テーブル名（必須）");
            Log("           -output=<path> 出力ファイルパス");
            Log("           -format=<csv|json> 出力形式（デフォルト: csv）");
            Log("");
            Log("  Import   - CSVファイルからデータをインポートします");
            Log("           -table=<name>  テーブル名（必須）");
            Log("           -input=<path>  入力ファイルパス（必須）");
            Log("");
            Log("  Backup   - バックアップを作成します");
            Log("           -table=<name>  テーブル名（任意、省略した場合は全テーブル）");
            Log("           -name=<name>   バックアップ名");
            Log("");
            Log("  Validate - テーブルデータをバリデーションします");
            Log("           -table=<name>  テーブル名（任意、省略した場合は全テーブル）");
            Log("");
            Log("  Query    - SQLクエリを実行します");
            Log("           -sql=<query>   SQLクエリ（必須）");
            Log("           -output=<path> 結果の出力ファイルパス");
            Log("");
            Log("  ListTables - すべてのテーブルを一覧表示します");
            Log("");
            Log("  Schema   - テーブルのスキーマを表示します");
            Log("           -table=<name>  テーブル名（必須）");

            EditorApplication.Exit(0);
        }

        private static Dictionary<string, string> ParseCommandLineArgs()
        {
            var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var commandLineArgs = Environment.GetCommandLineArgs();

            foreach (var arg in commandLineArgs)
            {
                if (!arg.StartsWith("-") || !arg.Contains("="))
                    continue;
                var parts = arg[1..].Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    args[parts[0]] = parts[1].Trim('"', '\'');
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
                return;
            }
            
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

        private static void ImportTable(ITableAsset table, string inputPath)
        {
            if (table is not IImportable importable)
                throw new NotSupportedException($"テーブル {table.GetType().Name} はインポートをサポートしていません");
            
            importable.Import(inputPath);

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
                    LogError($"{table.GetType().Name} のバックアップに失敗しました: {ex.Message}");
                }
            }

            Log($"バックアップ完了: {successCount}/{tables.Count} 個のテーブル");
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
                LogError($"[{tableName}] {duplicates.Count} 個の重複キーが見つかりました");
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
                LogError($"[{tableName}] {nullCount} 個の null レコードが見つかりました");
                hasErrors = true;
            }

            if (!hasErrors)
            {
                Log($"[{tableName}] バリデーションに合格しました ({table.Count} レコード)");
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
