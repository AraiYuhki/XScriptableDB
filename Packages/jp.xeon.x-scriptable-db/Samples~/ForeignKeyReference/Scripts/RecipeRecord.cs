using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;
using Xeon.XScriptableDB.Validation;

namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// Record class for the recipe master.
    /// Holds multiple foreign key references to items.
    /// </summary>
    [Serializable]
    public class RecipeRecord
    {
        [SerializeField, CsvColumn("ID"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("RecipeName")]
        private string name;

        [SerializeField, CsvColumn("ResultItemID"), ForeignKey(typeof(ItemTable))]
        private int resultItemId;

        [SerializeField, CsvColumn("ResultCount")]
        private int resultCount;

        [SerializeField, CsvColumn("Material1ID")]
        private int material1Id;

        [SerializeField, CsvColumn("Material1Count")]
        private int material1Count;

        [SerializeField, CsvColumn("Material2ID")]
        private int material2Id;

        [SerializeField, CsvColumn("Material2Count")]
        private int material2Count;

        [SerializeField, CsvColumn("Material3ID")]
        private int material3Id;

        [SerializeField, CsvColumn("Material3Count")]
        private int material3Count;

        /// <summary>
        /// Recipe ID (primary key)
        /// </summary>
        public int Id => id;

        /// <summary>
        /// Recipe name
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Result item ID (foreign key)
        /// </summary>
        public int ResultItemId => resultItemId;

        /// <summary>
        /// Result count
        /// </summary>
        public int ResultCount => resultCount;

        /// <summary>
        /// Material 1 item ID (0 means not set)
        /// </summary>
        public int Material1Id => material1Id;

        /// <summary>
        /// Required count for material 1
        /// </summary>
        public int Material1Count => material1Count;

        /// <summary>
        /// Material 2 item ID (0 means not set)
        /// </summary>
        public int Material2Id => material2Id;

        /// <summary>
        /// Required count for material 2
        /// </summary>
        public int Material2Count => material2Count;

        /// <summary>
        /// Material 3 item ID (0 means not set)
        /// </summary>
        public int Material3Id => material3Id;

        /// <summary>
        /// Required count for material 3
        /// </summary>
        public int Material3Count => material3Count;

        /// <summary>
        /// Whether material 1 is set
        /// </summary>
        public bool HasMaterial1 => material1Id > 0;

        /// <summary>
        /// Whether material 2 is set
        /// </summary>
        public bool HasMaterial2 => material2Id > 0;

        /// <summary>
        /// Whether material 3 is set
        /// </summary>
        public bool HasMaterial3 => material3Id > 0;

        public override string ToString() => $"[{id}] {name}";
    }
}
