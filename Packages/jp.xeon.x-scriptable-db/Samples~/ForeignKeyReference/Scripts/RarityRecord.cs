using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// Record class for the rarity master.
    /// Defines the rarity level of items.
    /// </summary>
    [Serializable]
    public class RarityRecord
    {
        [SerializeField, CsvColumn("ID"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("RarityName")]
        private string name;

        [SerializeField, CsvColumn("ColorR")]
        private float colorR;

        [SerializeField, CsvColumn("ColorG")]
        private float colorG;

        [SerializeField, CsvColumn("ColorB")]
        private float colorB;

        [SerializeField, CsvColumn("PriceMultiplier")]
        private float priceMultiplier;

        [SerializeField, CsvColumn("DropRate")]
        private float dropRate;

        /// <summary>
        /// Rarity ID (primary key)
        /// </summary>
        public int Id => id;

        /// <summary>
        /// Rarity name
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Display color
        /// </summary>
        public Color Color => new(colorR, colorG, colorB, 1f);

        /// <summary>
        /// Price multiplier
        /// </summary>
        public float PriceMultiplier => priceMultiplier;

        /// <summary>
        /// Drop rate
        /// </summary>
        public float DropRate => dropRate;

        /// <summary>
        /// Star count (same as rarity ID)
        /// </summary>
        public int StarCount => id;

        public override string ToString() => $"[{id}] {name} (x{priceMultiplier})";
    }
}
