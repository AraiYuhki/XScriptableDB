using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// データベースマネージャー。
    /// テーブルの登録と型安全なアクセスを提供する。
    /// </summary>
    public class Database
    {
        private static Database instance;
        private static readonly object lockObject = new();

        /// <summary>
        /// シングルトンインスタンス。
        /// </summary>
        public static Database Instance
        {
            get
            {
                if (instance != null)
                    return instance;
                lock (lockObject)
                {
                    instance ??= new Database();
                }
                return instance;
            }
        }

        private readonly Dictionary<Type, ScriptableObject> tables = new();
        private readonly Dictionary<string, string> addressableKeys = new();

        private Database() { }

        ~Database()
        {
            tables.Clear();
        }

        /// <summary>
        /// テーブルを登録する。
        /// </summary>
        /// <typeparam name="TTable">テーブルの型</typeparam>
        /// <param name="table">登録するテーブル</param>
        public static void Register<TTable>(TTable table) where TTable : ScriptableObject
        {
            if (table == null)
            {
                Debug.LogWarning($"Cannot register null table for type {typeof(TTable).Name}");
                return;
            }
            Instance.tables[typeof(TTable)] = table;
        }

        /// <summary>
        /// テーブルを取得する。
        /// </summary>
        /// <typeparam name="TTable">テーブルの型</typeparam>
        /// <returns>テーブル、見つからない場合はnull</returns>
        public static TTable Get<TTable>() where TTable : ScriptableObject
        {
            if (!Instance.tables.TryGetValue(typeof(TTable), out var table))
            {
                Debug.LogWarning($"Table of type {typeof(TTable).Name} is not registered");
                return null;
            }
            return table as TTable;
        }

        /// <summary>
        /// テーブルを取得する。
        /// </summary>
        /// <typeparam name="TTable">テーブルの型</typeparam>
        /// <param name="table">取得したテーブル</param>
        /// <returns>見つかった場合はtrue</returns>
        public static bool TryGet<TTable>(out TTable table) where TTable : ScriptableObject
        {
            table = null;
            if (!Instance.tables.TryGetValue(typeof(TTable), out var so))
                return false;
            table = so as TTable;
            return table != null;
        }

        /// <summary>
        /// Addressableキーを登録する。
        /// </summary>
        /// <typeparam name="TTable">テーブルの型</typeparam>
        /// <param name="addressableKey">Addressableキー</param>
        public static void RegisterAddressable<TTable>(string addressableKey) where TTable : ScriptableObject
        {
            Instance.addressableKeys[typeof(TTable).FullName] = addressableKey;
        }

        /// <summary>
        /// Addressableからテーブルをロードして登録する。
        /// </summary>
        /// <typeparam name="TTable">テーブルの型</typeparam>
        /// <param name="addressableKey">Addressableキー</param>
        /// <returns>ロードしたテーブル</returns>
        public static TTable LoadAndRegister<TTable>(string addressableKey) where TTable : ScriptableObject
        {
            var table = Addressables.LoadAssetAsync<TTable>(addressableKey).WaitForCompletion();
            if (table == null)
            {
                Debug.LogError($"Failed to load table from Addressable key: {addressableKey}");
                return null;
            }
            Register(table);
            return table;
        }

        /// <summary>
        /// テーブルの登録を解除する。
        /// </summary>
        /// <typeparam name="TTable">テーブルの型</typeparam>
        public static void Unregister<TTable>() where TTable : ScriptableObject
        {
            var type = typeof(TTable);
            if (!Instance.tables.TryGetValue(type, out var table))
                return;
            SafeRelease(table);
            Instance.tables.Remove(type);
        }

        /// <summary>
        /// 全てのテーブルの登録を解除する。
        /// </summary>
        public static void Clear()
        {
            foreach (var table in Instance.tables.Values)
                SafeRelease(table);
            Instance.tables.Clear();
        }

        /// <summary>
        /// インスタンスをリセットする。
        /// </summary>
        public static void Reset()
        {
            lock (lockObject)
            {
                if (instance != null)
                {
                    foreach (var table in instance.tables.Values)
                        SafeRelease(table);
                    instance.tables.Clear();
                }
                instance = null;
            }
        }

        /// <summary>
        /// Addressableキーを使用して全テーブルを再ロードする。
        /// </summary>
        public static void Reload()
        {
            var keys = Instance.addressableKeys.ToList();
            Clear();
            foreach (var (typeName, addressableKey) in keys)
            {
                var table = Addressables.LoadAssetAsync<ScriptableObject>(addressableKey).WaitForCompletion();
                if (table == null)
                    continue;
                var type = table.GetType();
                Instance.tables[type] = table;
            }
        }

        private static void SafeRelease(object target)
        {
            if (target == null)
                return;
            try
            {
                Addressables.Release(target);
            }
            catch
            {
                // Ignore release errors for non-Addressable objects
            }
        }

        /// <summary>
        /// 登録されているテーブル数。
        /// </summary>
        public static int TableCount => Instance.tables.Count;

        /// <summary>
        /// 登録されているテーブルの型一覧。
        /// </summary>
        public static IEnumerable<Type> RegisteredTypes => Instance.tables.Keys;

        /// <summary>
        /// 登録されている全テーブル。
        /// </summary>
        public static IEnumerable<ScriptableObject> AllTables => Instance.tables.Values;

        /// <summary>
        /// 指定した型のテーブルが登録されているかどうか。
        /// </summary>
        /// <typeparam name="TTable">テーブルの型</typeparam>
        /// <returns>登録されている場合はtrue</returns>
        public static bool IsRegistered<TTable>() where TTable : ScriptableObject
        {
            return Instance.tables.ContainsKey(typeof(TTable));
        }

        /// <summary>
        /// 指定した型のテーブルからレコードを検索する。
        /// </summary>
        /// <typeparam name="TTable">テーブルの型</typeparam>
        /// <typeparam name="TRecord">レコードの型</typeparam>
        /// <typeparam name="TKey">PrimaryKeyの型</typeparam>
        /// <param name="key">検索するPrimaryKey</param>
        /// <returns>見つかったレコード、見つからない場合はdefault</returns>
        public static TRecord Find<TTable, TRecord, TKey>(TKey key)
            where TTable : TableAsset<TRecord, TKey>
            where TRecord : class, new()
            where TKey : IComparable<TKey>
        {
            var table = Get<TTable>();
            if (table == null)
                return default;
            return table.FindByKey(key);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 登録されているテーブルの一覧を取得する（Editor専用）。
        /// </summary>
        public static Dictionary<Type, ScriptableObject> GetTableDictionary()
        {
            return new Dictionary<Type, ScriptableObject>(Instance.tables);
        }

        /// <summary>
        /// 登録されているテーブル名の一覧を取得する（Editor専用）。
        /// </summary>
        public static string[] GetTableNames()
        {
            return Instance.tables.Keys.Select(t => t.Name).ToArray();
        }

        /// <summary>
        /// テーブル名でテーブルを取得する（Editor専用）。
        /// </summary>
        /// <param name="name">テーブル名</param>
        /// <returns>テーブル、見つからない場合はnull</returns>
        public static ScriptableObject GetTableByName(string name)
        {
            return Instance.tables.FirstOrDefault(p => p.Key.Name == name).Value;
        }

        /// <summary>
        /// ITableAssetとして全テーブルを取得する（Editor専用）。
        /// </summary>
        public static IEnumerable<ITableAsset> GetAllTableAssets()
        {
            foreach (var table in Instance.tables.Values)
            {
                if (table is ITableAsset tableAsset)
                    yield return tableAsset;
            }
        }

        [MenuItem("Tools/XScriptableDB/Clear Database")]
        private static void ClearDatabase()
        {
            Clear();
            Debug.Log("Database cleared");
        }

        [MenuItem("Tools/XScriptableDB/Reload Database")]
        private static void ReloadDatabase() => Reload();

        private static void ExportToFile(string extension)
        {
            var folderPath = EditorUtility.SaveFolderPanel(
                "Select Export Folder",
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "Masters");
            if (string.IsNullOrEmpty(folderPath))
                return;

            var successList = new List<string>();
            var failedList = new List<string>();

            foreach (var table in Instance.tables.Values)
            {
                if (table is not IExportable exporter)
                    continue;

                var filePath = Path.Combine(folderPath, table.name + extension);
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

        private static void ImportFromFile(string extension)
        {
            var folderPath = EditorUtility.OpenFolderPanel(
                "Select Import Folder",
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "Masters");
            if (string.IsNullOrEmpty(folderPath))
                return;

            var successList = new List<string>();
            var failedList = new List<string>();
            var skippedList = new List<string>();

            var directoryInfo = new DirectoryInfo(folderPath);
            var files = directoryInfo.GetFiles($"*{extension}", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                var tableName = Path.GetFileNameWithoutExtension(file.Name);
                var targetTable = Instance.tables.Values.FirstOrDefault(
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

        [MenuItem("Tools/XScriptableDB/Export/CSV")]
        public static void ExportToCsv() => ExportToFile(".csv");

        [MenuItem("Tools/XScriptableDB/Export/TSV")]
        public static void ExportToTsv() => ExportToFile(".tsv");

        [MenuItem("Tools/XScriptableDB/Import/CSV")]
        public static void ImportFromCsv() => ImportFromFile(".csv");

        [MenuItem("Tools/XScriptableDB/Import/TSV")]
        public static void ImportFromTsv() => ImportFromFile(".tsv");
#endif
    }
}
