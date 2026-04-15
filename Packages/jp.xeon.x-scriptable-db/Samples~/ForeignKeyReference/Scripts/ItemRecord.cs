using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;
using Xeon.XScriptableDB.Validation;

namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// Record class for the item master.
    /// Holds foreign key references to category and rarity.
    /// </summary>
    [Serializable]
    public class ItemRecord
    {
        [SerializeField, CsvColumn("ID"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("ItemName")]
        private string name;

        [SerializeField, CsvColumn("Description")]
        private string description;

        [SerializeField, CsvColumn("CategoryID"), SecondaryKey, ForeignKey(typeof(CategoryTable))]
        private int categoryId;

        [SerializeField, CsvColumn("RarityID"), SecondaryKey, ForeignKey(typeof(RarityTable))]
        private int rarityId;

        [SerializeField, CsvColumn("BasePrice")]
        private int basePrice;

        [SerializeField, CsvColumn("StackLimit")]
        private int stackLimit;

        /// <summary>
        /// Item ID (primary key)
        /// </summary>
        public int Id => id;

        /// <summary>
        /// Item name
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Description
        /// </summary>
        public string Description => description;

        /// <summary>
        /// Category ID (foreign key)
        /// </summary>
        public int CategoryId => categoryId;

        /// <summary>
        /// Rarity ID (foreign key)
        /// </summary>
        public int RarityId => rarityId;

        /// <summary>
        /// Base price
        /// </summary>
        public int BasePrice => basePrice;

        /// <summary>
        /// Stack limit
        /// </summary>
        public int StackLimit => stackLimit;

        public override string ToString() => $"[{id}] {name}";
    }
}
