using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Migrates from the legacy format (list of strings) to a list of IndexDefinitions.
        /// </summary>
        public void MigrateFromLegacyIndices(List<string> legacyIndices)
        {
            if (legacyIndices == null || legacyIndices.Count == 0)
                return;

            indices = legacyIndices.Select(IndexDefinition.FromLegacy).ToList();
        }

        public override string ToString()
        {
            var indexStr = string.Join('\n', indices.Select(idx => idx.ToString()));
            return $"Table: {tableName}\nColumns:\n{string.Join('\n', columns)}\nIndices:\n{indexStr}";
        }
    }

    /// <summary>
    /// Legacy format table definition (used for migration from YAML).
    /// </summary>
    [Serializable]
    internal class LegacyTableDefinition
    {
        public string tableName;
        public bool isReadOnly = true;
        public List<ColumnDefinition> columns = new();
        public List<string> indices = new();

        public TableDefinition ToTableDefinition()
        {
            var definition = new TableDefinition
            {
                TableName = tableName,
                IsReadOnly = isReadOnly,
                Columns = columns
            };
            definition.MigrateFromLegacyIndices(indices);
            return definition;
        }
    }
}
