using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB
{
    public abstract class TableBase<T> : ScriptableObject, ITable<T>, IImportable, IExportable
        where T : CsvData, new()
    {
        [SerializeField]
        protected List<T> data = new List<T>();

        public IReadOnlyList<T> All => data;
        public int Count => data.Count;

        private void OnEnable()
        {
            Initialize();
        }

        protected abstract void Initialize();

        public void Export(string filePath, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(filePath))
                return;
            encoding ??= Encoding.UTF8;
            try
            {
                File.WriteAllText(filePath, CsvParser.ToCSV(data), encoding);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"Failed to export {filePath}:{GetType().Name}");
            }
        }

        public void Import(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;
            data = CsvParser.ParseFile<T>(filePath);
            Initialize();
        }
    }

    public abstract class LookupTableBase<T, TKey> : TableBase<T>, ILookupTable<T, TKey>
        where T : CsvData, IPrimaryKey<TKey>, new()
    {
        private Dictionary<TKey, T> index;

        protected override void Initialize()
        {
            BuildIndex();
        }

        protected virtual void BuildIndex()
        {
            index = new Dictionary<TKey, T>(data.Count);
            foreach (var record in data)
            {
                if (index.ContainsKey(record.PrimaryKey))
                {
                    Debug.LogWarning($"Duplicate PrimaryKey detected: {record.PrimaryKey} in {GetType().Name}");
                    continue;
                }
                index[record.PrimaryKey] = record;
            }
        }

        public T FindByPrimaryKey(TKey key)
        {
            if (index == null)
                BuildIndex();
            return index.TryGetValue(key, out var record) ? record : default;
        }

        public bool TryFindByPrimaryKey(TKey key, out T record)
        {
            if (index == null)
                BuildIndex();
            return index.TryGetValue(key, out record);
        }
    }
}
