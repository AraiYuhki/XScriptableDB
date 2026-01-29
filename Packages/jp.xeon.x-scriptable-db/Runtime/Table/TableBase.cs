using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Xeon.XScriptableDB.IO;
#if SIMPLE_CSV_SUPPORT
using System;
using System.IO;
#endif

namespace Xeon.XScriptableDB
{
    public abstract class TableBase<T> : ScriptableObject, IImportable, IExportable
        where T :
#if SIMPLE_CSV_SUPPORT
        CsvData,
#endif
        new()
    {
        [SerializeField]
        protected List<T> data = new List<T>();

        public List<T> All => data;

        private void OnEnable()
        {
            Initialize();
        }

        protected abstract void Initialize();

#if SIMPLE_CSV_SUPPORT

        public void Export(string filePath, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            encoding ??= Encoding.UTF8;
            try
            {
                File.WriteAllText(filePath, CsvParser.ToCSV(All), encoding);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"Failed to export {filePath}:{this.GetType().Name}");
            }
        }

        public void Import(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            data = CsvParser.Parse<T>(filePath);
            Initialize();
        }
#else
        public virtual void Import(string filePath) { }
        public virtual void Export(string filePath, Encoding encoding = null) { }
#endif
    }
}
