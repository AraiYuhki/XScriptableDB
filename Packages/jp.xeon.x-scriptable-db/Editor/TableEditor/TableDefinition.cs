using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    [Serializable]
    public class TableDefinition
    {
        [SerializeField]
        private string tableName;
        [SerializeField]
        private bool isReadOnly = true;

        [SerializeField]
        private List<ColumnDefinition> columns = new();

        [SerializeField]
        private List<IndexDefinition> indices = new();

        /// <summary>
        /// 旧形式（List<string>）のインデックスを新形式に変換する。
        /// </summary>
        public void MigrateFromLegacyIndices(List<string> legacyIndices)
        {
            if (legacyIndices == null)
                return;

            indices.Clear();
            foreach (var columnName in legacyIndices)
            {
                if (string.IsNullOrEmpty(columnName))
                    continue;
                indices.Add(new IndexDefinition(columnName, columnName));
            }
        }

        public string TableName
        {
            get => tableName;
            set => tableName = value;
        }

        public bool IsReadOnly
        {
            get => isReadOnly;
            set => isReadOnly = value;
        }

        public List<ColumnDefinition> Columns
        {
            get => columns;
            set => columns = value;
        }

        public List<IndexDefinition> Indices
        {
            get => indices;
            set => indices = value;
        }

        public override string ToString()
        {
            return $"Table: {tableName}\nColumns:\n{string.Join('\n', columns)}\n{string.Join('\n', indices)}";
        }
    }
}
