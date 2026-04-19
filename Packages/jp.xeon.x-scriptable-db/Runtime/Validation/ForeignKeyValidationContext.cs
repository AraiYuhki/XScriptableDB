using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// 外部キー検証のコンテキスト。
    /// </summary>
    public class ForeignKeyValidationContext
    {
        private readonly Dictionary<Type, ITableAsset> tables = new();
        private readonly Dictionary<Type, HashSet<object>> keyCache = new();

        /// <summary>
        /// テーブルを登録します。
        /// </summary>
        /// <typeparam name="T">テーブルの型</typeparam>
        /// <param name="table">テーブルアセット</param>
        public void RegisterTable<T>(T table) where T : ITableAsset
        {
            tables[typeof(T)] = table;
        }

        /// <summary>
        /// テーブルを登録します。
        /// </summary>
        /// <param name="tableType">テーブルの型</param>
        /// <param name="table">テーブルアセット</param>
        public void RegisterTable(Type tableType, ITableAsset table)
        {
            tables[tableType] = table;
        }

        /// <summary>
        /// 登録されているテーブルを返します。
        /// </summary>
        public ITableAsset GetTable(Type tableType)
        {
            return tables.TryGetValue(tableType, out var table) ? table : null;
        }

        /// <summary>
        /// テーブルのキーセットを返します（キャッシュ付き）。
        /// </summary>
        public HashSet<object> GetKeySet(Type tableType)
        {
            if (keyCache.TryGetValue(tableType, out var cachedKeys))
            {
                return cachedKeys;
            }

            var table = GetTable(tableType);
            if (table == null) return null;

            var keySet = new HashSet<object>();
            var keyField = GetPrimaryKeyField(table.RecordType);
            keyCache[tableType] = keySet;
            if (keyField == null)
                return keySet;
            
            foreach (var record in table.Records)
            {
                if (record == null)
                    continue;
                var key = keyField.GetValue(record);
                if (key != null)
                {
                    keySet.Add(key);
                }
            }
            
            return keySet;
        }

        /// <summary>
        /// キーのキャッシュをクリアします。
        /// </summary>
        public void ClearCache()
        {
            keyCache.Clear();
        }

        /// <summary>
        /// PrimaryKeyフィールドを返します。
        /// </summary>
        private FieldInfo GetPrimaryKeyField(Type recordType)
        {
            return recordType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(f => f.GetCustomAttribute<PrimaryKeyAttribute>() != null);
        }
    }
}