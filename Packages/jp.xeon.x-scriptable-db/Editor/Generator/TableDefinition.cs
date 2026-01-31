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

        // TODO: ï°çáÉLÅ[Ç…ëŒâûÇ∑ÇÈ
        [SerializeField]
        private List<string> indecies = new();

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

        public List<string> Indecies
        {
            get => indecies;
            set => indecies = value;
        }

        public override string ToString()
        {
            return $"Table: {tableName}\nColumns:\n{string.Join('\n', columns)}\n{string.Join('\n', indecies)}";
        }
    }
}
