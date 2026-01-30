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
        private List<ColumnDefinition> columns = new();

        public string TableName
        {
            get => tableName;
            set => tableName = value;
        }

        public List<ColumnDefinition> Columns
        {
            get => columns;
            set => columns = value;
        }

        public override string ToString()
        {
            return $"Table: {tableName}\nColumns:\n{string.Join('\n', columns)}";
        }
    }
}
