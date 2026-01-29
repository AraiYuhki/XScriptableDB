using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Xeon.XScriptableDB
{
    public class DB
    {
        private static DB instance;
        private static readonly object lockObject = new();

        public static DB Instance
        {
            get
            {
                if (instance != null)
                    return instance;
                lock (lockObject)
                {
                    instance ??= new DB();
                }
                return instance;
            }
        }

        private readonly Dictionary<Type, ScriptableObject> tables = new();
        private readonly Dictionary<string, string> addressableKeys = new();

        private DB() { }

        ~DB()
        {
            UnloadAll();
        }

        public static T Get<T>() where T : ScriptableObject
        {
            if (!Instance.tables.TryGetValue(typeof(T), out var table))
            {
                Debug.LogWarning($"Table of type {typeof(T).Name} is not registered");
                return null;
            }
            return table as T;
        }

        public static bool TryGet<T>(out T table) where T : ScriptableObject
        {
            table = null;
            if (!Instance.tables.TryGetValue(typeof(T), out var so))
                return false;
            table = so as T;
            return table != null;
        }

        public static void Register<T>(T table) where T : ScriptableObject
        {
            if (table == null)
            {
                Debug.LogWarning($"Cannot register null table for type {typeof(T).Name}");
                return;
            }
            Instance.tables[typeof(T)] = table;
        }

        public static void RegisterAddressable<T>(string addressableKey) where T : ScriptableObject
        {
            Instance.addressableKeys[typeof(T).FullName] = addressableKey;
        }

        public static T LoadAndRegister<T>(string addressableKey) where T : ScriptableObject
        {
            var table = Addressables.LoadAssetAsync<T>(addressableKey).WaitForCompletion();
            if (table == null)
            {
                Debug.LogError($"Failed to load table from Addressable key: {addressableKey}");
                return null;
            }
            Register(table);
            return table;
        }

        public static void Unregister<T>() where T : ScriptableObject
        {
            var type = typeof(T);
            if (!Instance.tables.TryGetValue(type, out var table))
                return;
            SafeRelease(table);
            Instance.tables.Remove(type);
        }

        public static void UnloadAll()
        {
            foreach (var table in Instance.tables.Values)
                SafeRelease(table);
            Instance.tables.Clear();
        }

        public static void Reload()
        {
            var keys = Instance.addressableKeys.ToList();
            UnloadAll();
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

        public int TableCount => tables.Count;
        public IEnumerable<Type> RegisteredTypes => tables.Keys;
        public IEnumerable<ScriptableObject> AllTables => tables.Values;

#if UNITY_EDITOR
        public Dictionary<Type, ScriptableObject> GetTableList() => new(tables);
        public string[] GetTableNames() => tables.Keys.Select(type => type.Name).ToArray();

        public ScriptableObject GetTable(string name)
            => tables.FirstOrDefault(pair => pair.Key.Name == name).Value;

        [UnityEditor.MenuItem("Tools/XScriptableDB/Reload DB")]
        public static void ReloadDB() => Reload();

        [UnityEditor.MenuItem("Tools/XScriptableDB/Unload DB")]
        public static void UnloadDB()
        {
            UnloadAll();
            instance = null;
        }

        [UnityEditor.MenuItem("Tools/XScriptableDB/Export to TSV")]
        public static void ExportToTsv()
        {
            var folderPath = UnityEditor.EditorUtility.SaveFolderPanel(
                "Select Export Folder",
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "Masters");
            if (string.IsNullOrEmpty(folderPath))
                return;
            foreach (var table in Instance.tables.Values)
            {
                if (table is not IExportable exporter)
                    continue;
                var filePath = Path.Combine(folderPath, table.name + ".tsv");
                exporter.Export(filePath, Encoding.UTF8);
                Debug.Log($"Exported: {filePath}");
            }
        }

        [UnityEditor.MenuItem("Tools/XScriptableDB/Import from TSV")]
        public static void ImportFromTsv()
        {
            var folderPath = UnityEditor.EditorUtility.OpenFolderPanel(
                "Select Import Folder",
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "Masters");
            if (string.IsNullOrEmpty(folderPath))
                return;
            var directoryInfo = new DirectoryInfo(folderPath);
            var files = directoryInfo.GetFiles("*.tsv", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var tableName = Path.GetFileNameWithoutExtension(file.Name);
                var targetTable = Instance.tables.Values.FirstOrDefault(
                    table => tableName == table.GetType().Name || tableName == table.name);
                if (targetTable is not IImportable importable)
                    continue;
                Debug.Log($"Importing: {file.Name} -> {targetTable.name}");
                importable.Import(file.FullName);
                UnityEditor.EditorUtility.SetDirty(targetTable);
            }
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
#endif
    }
}
