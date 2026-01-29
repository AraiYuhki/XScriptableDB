using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// XScriptableDBのデータベースマネージャー。
    /// XTableAssetの自動登録と型安全なアクセスを提供する。
    /// </summary>
    public class XDatabase
    {
        private static XDatabase instance;
        private static readonly object lockObject = new();

        /// <summary>
        /// シングルトンインスタンス。
        /// </summary>
        public static XDatabase Instance
        {
            get
            {
                if (instance != null)
                    return instance;
                lock (lockObject)
                {
                    instance ??= new XDatabase();
                }
                return instance;
            }
        }

        private readonly Dictionary<Type, ScriptableObject> tables = new();

        private XDatabase() { }

        ~XDatabase()
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
        /// テーブルの登録を解除する。
        /// </summary>
        /// <typeparam name="TTable">テーブルの型</typeparam>
        public static void Unregister<TTable>() where TTable : ScriptableObject
        {
            Instance.tables.Remove(typeof(TTable));
        }

        /// <summary>
        /// 全てのテーブルの登録を解除する。
        /// </summary>
        public static void Clear()
        {
            Instance.tables.Clear();
        }

        /// <summary>
        /// インスタンスをリセットする。
        /// </summary>
        public static void Reset()
        {
            lock (lockObject)
            {
                instance?.tables.Clear();
                instance = null;
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
            where TTable : XTableAsset<TRecord, TKey>
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
        /// IXTableAssetとして全テーブルを取得する（Editor専用）。
        /// </summary>
        public static IEnumerable<IXTableAsset> GetAllXTableAssets()
        {
            foreach (var table in Instance.tables.Values)
            {
                if (table is IXTableAsset xTable)
                    yield return xTable;
            }
        }

        [UnityEditor.MenuItem("Tools/XScriptableDB/Clear XDatabase")]
        private static void ClearXDatabase()
        {
            Clear();
            Debug.Log("XDatabase cleared");
        }
#endif
    }
}
