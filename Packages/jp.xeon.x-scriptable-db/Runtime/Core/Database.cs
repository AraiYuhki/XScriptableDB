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
    /// Database manager.
    /// Provides table registration and type-safe access.
    /// </summary>
    public class Database
    {
        private static Database instance;
        private static readonly object lockObject = new();

        /// <summary>
        /// Singleton instance.
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
        /// Registers a table.
        /// </summary>
        /// <typeparam name="TTable">Type of the table</typeparam>
        /// <param name="table">The table to register</param>
        public static void Register<TTable>(TTable table) where TTable : ScriptableObject, ITableAsset
        {
            if (table == null)
            {
                Debug.LogWarning($"Cannot register null table for type {typeof(TTable).Name}");
                return;
            }
            Instance.tables[typeof(TTable)] = table;
        }

        /// <summary>
        /// Retrieves a table.
        /// </summary>
        /// <typeparam name="TTable">Type of the table</typeparam>
        /// <returns>The table, or null if not found</returns>
        public static TTable Get<TTable>() where TTable : ScriptableObject, ITableAsset
        {
            if (!Instance.tables.TryGetValue(typeof(TTable), out var table))
            {
                Debug.LogWarning($"Table of type {typeof(TTable).Name} is not registered");
                return null;
            }
            return table as TTable;
        }

        /// <summary>
        /// Retrieves a table.
        /// </summary>
        /// <typeparam name="TTable">Type of the table</typeparam>
        /// <param name="table">The retrieved table</param>
        /// <returns>True if found</returns>
        public static bool TryGet<TTable>(out TTable table) where TTable : ScriptableObject, ITableAsset
        {
            table = null;
            if (!Instance.tables.TryGetValue(typeof(TTable), out var so))
                return false;
            table = so as TTable;
            return table != null;
        }

        /// <summary>
        /// Registers an Addressable key.
        /// </summary>
        /// <typeparam name="TTable">Type of the table</typeparam>
        /// <param name="addressableKey">The Addressable key</param>
        public static void RegisterAddressable<TTable>(string addressableKey) where TTable : ScriptableObject, ITableAsset
        {
            Instance.addressableKeys[typeof(TTable).FullName] = addressableKey;
        }

        /// <summary>
        /// Loads a table from Addressables and registers it.
        /// </summary>
        /// <typeparam name="TTable">Type of the table</typeparam>
        /// <param name="addressableKey">The Addressable key</param>
        /// <returns>The loaded table</returns>
        public static TTable LoadAndRegister<TTable>(string addressableKey) where TTable : ScriptableObject, ITableAsset
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
        /// Unregisters a table.
        /// </summary>
        /// <typeparam name="TTable">Type of the table</typeparam>
        public static void Unregister<TTable>() where TTable : ScriptableObject, ITableAsset
        {
            var type = typeof(TTable);
            if (!Instance.tables.TryGetValue(type, out var table))
                return;
            SafeRelease(table);
            Instance.tables.Remove(type);
        }

        /// <summary>
        /// Unregisters all tables.
        /// </summary>
        public static void Clear()
        {
            foreach (var table in Instance.tables.Values)
                SafeRelease(table);
            Instance.tables.Clear();
        }

        /// <summary>
        /// Resets the instance.
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
        /// Reloads all tables using their registered Addressable keys.
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
        /// Number of registered tables.
        /// </summary>
        public static int TableCount => Instance.tables.Count;

        /// <summary>
        /// List of registered table types.
        /// </summary>
        public static IEnumerable<Type> RegisteredTypes => Instance.tables.Keys;

        /// <summary>
        /// All registered tables.
        /// </summary>
        public static IEnumerable<ScriptableObject> AllTables => Instance.tables.Values;

        /// <summary>
        /// Returns whether a table of the specified type is registered.
        /// </summary>
        /// <typeparam name="TTable">Type of the table</typeparam>
        /// <returns>True if registered</returns>
        public static bool IsRegistered<TTable>() where TTable : ScriptableObject
        {
            return Instance.tables.ContainsKey(typeof(TTable));
        }

        /// <summary>
        /// Searches for a record in the table of the specified type.
        /// </summary>
        /// <typeparam name="TTable">Type of the table</typeparam>
        /// <typeparam name="TRecord">Type of the record</typeparam>
        /// <typeparam name="TKey">Type of the PrimaryKey</typeparam>
        /// <param name="key">The PrimaryKey to search for</param>
        /// <returns>The found record, or default if not found</returns>
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
        [MenuItem("Tools/XScriptableDB/Clear Database")]
        private static void ClearDatabase()
        {
            Clear();
            Debug.Log("Database cleared");
        }

        [MenuItem("Tools/XScriptableDB/Reload Database")]
        private static void ReloadDatabase() => Reload();
#endif
    }
}
