using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Definition of a SecondaryKey index.
    /// Can represent both single-field and composite indexes.
    /// </summary>
    /// <remarks>
    /// Setters are exposed for editing because this class is intended for use in the Editor.
    /// </remarks>
    [Serializable]
    public class IndexDefinition
    {
        [SerializeField]
        private string name;

        [SerializeField]
        private List<string> columns = new();

        [SerializeField]
        private bool allowDuplicates = true;

        /// <summary>
        /// The name of the index.
        /// </summary>
        public string Name
        {
            get => name;
            set => name = value;
        }

        /// <summary>
        /// List of column names that make up the index.
        /// When multiple columns are present, it is a composite index.
        /// </summary>
        public List<string> Columns
        {
            get => columns;
            set => columns = value ?? new List<string>();
        }

        /// <summary>
        /// Whether to allow multiple records with the same key value.
        /// </summary>
        public bool AllowDuplicates
        {
            get => allowDuplicates;
            set => allowDuplicates = value;
        }

        /// <summary>
        /// Whether this is a composite index.
        /// </summary>
        public bool IsComposite => columns.Count > 1;

        public IndexDefinition()
        {
        }

        public IndexDefinition(string name)
        {
            this.name = name;
            columns = new List<string> { name };
        }

        public IndexDefinition(string name, params string[] columnNames)
        {
            this.name = name;
            columns = new List<string>(columnNames);
        }

        /// <summary>
        /// Creates an index definition from a single column.
        /// </summary>
        public static IndexDefinition FromSingleColumn(string columnName)
        {
            return new IndexDefinition(columnName) { columns = new List<string> { columnName } };
        }

        /// <summary>
        /// Creates a composite index definition.
        /// </summary>
        public static IndexDefinition FromComposite(string name, params string[] columnNames)
        {
            return new IndexDefinition(name, columnNames);
        }

        /// <summary>
        /// Migrates from the legacy format (string).
        /// </summary>
        public static IndexDefinition FromLegacy(string legacyIndexName)
        {
            return FromSingleColumn(legacyIndexName);
        }

        public override string ToString()
        {
            var columnsStr = string.Join(", ", columns);
            var duplicateStr = allowDuplicates ? "" : " (unique)";
            return $"{name}: [{columnsStr}]{duplicateStr}";
        }
    }
}
