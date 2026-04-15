using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// Context for foreign key validation.
    /// </summary>
    public class ForeignKeyValidationContext
    {
        private readonly Dictionary<Type, ITableAsset> tables = new();
        private readonly Dictionary<Type, HashSet<object>> keyCache = new();

        /// <summary>
        /// Registers a table.
        /// </summary>
        /// <typeparam name="T">Type of the table</typeparam>
        /// <param name="table">Table asset</param>
        public void RegisterTable<T>(T table) where T : ITableAsset
        {
            tables[typeof(T)] = table;
        }

        /// <summary>
        /// Registers a table.
        /// </summary>
        /// <param name="tableType">Type of the table</param>
        /// <param name="table">Table asset</param>
        public void RegisterTable(Type tableType, ITableAsset table)
        {
            tables[tableType] = table;
        }

        /// <summary>
        /// Returns the registered table.
        /// </summary>
        public ITableAsset GetTable(Type tableType)
        {
            return tables.TryGetValue(tableType, out var table) ? table : null;
        }

        /// <summary>
        /// Returns the key set for a table (with caching).
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
        /// Clears the key cache.
        /// </summary>
        public void ClearCache()
        {
            keyCache.Clear();
        }

        /// <summary>
        /// Returns the PrimaryKey field.
        /// </summary>
        private FieldInfo GetPrimaryKeyField(Type recordType)
        {
            return recordType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(f => f.GetCustomAttribute<PrimaryKeyAttribute>() != null);
        }
    }
}