using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// Record class for the category master.
    /// Used to classify items.
    /// </summary>
    [Serializable]
    public class CategoryRecord
    {
        [SerializeField, CsvColumn("ID"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("CategoryName")]
        private string name;

        [SerializeField, CsvColumn("Description")]
        private string description;

        [SerializeField, CsvColumn("SortOrder")]
        private int sortOrder;

        /// <summary>
        /// Category ID (primary key)
        /// </summary>
        public int Id => id;

        /// <summary>
        /// Category name
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Description
        /// </summary>
        public string Description => description;

        /// <summary>
        /// Display order
        /// </summary>
        public int SortOrder => sortOrder;

        public override string ToString() => $"[{id}] {name}";
    }
}
