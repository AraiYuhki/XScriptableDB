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

        // TODO: 複合キーに対応する
        [SerializeField]
        private List<string> indices = new();

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

        public List<string> Indices
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
