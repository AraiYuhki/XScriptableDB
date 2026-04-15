using System;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace XScriptableDB.Samples
{
    /// <summary>
    /// Record class for item data.
    /// </summary>
    [Serializable]
    public class ItemRecord
    {
        /// <summary>
        /// Item ID (primary key).
        /// Adding the PrimaryKey attribute enables O(log n) fast lookup via binary search.
        /// </summary>
        [PrimaryKey]
        [CsvColumn("ID")]
        public int Id;

        /// <summary>
        /// Category (secondary key).
        /// Adding the SecondaryKey attribute enables O(1) lookup via hash index.
        /// </summary>
        [SecondaryKey]
        [CsvColumn("Category")]
        public string Category;

        /// <summary>
        /// Item name.
        /// </summary>
        [CsvColumn("Name")]
        public string Name;

        /// <summary>
        /// Description.
        /// </summary>
        [CsvColumn("Description")]
        public string Description;

        /// <summary>
        /// Price.
        /// </summary>
        [CsvColumn("Price")]
        public int Price;

        /// <summary>
        /// Attack power.
        /// </summary>
        [CsvColumn("Attack")]
        public int Attack;

        /// <summary>
        /// Defense power.
        /// </summary>
        [CsvColumn("Defense")]
        public int Defense;

        /// <summary>
        /// Rarity (secondary key).
        /// Multiple SecondaryKeys can be set.
        /// </summary>
        [SecondaryKey]
        [CsvColumn("Rarity")]
        public int Rarity;

        public override string ToString()
        {
            return $"[{Id}] {Name} ({Category}) - {Price}G";
        }
    }
}
