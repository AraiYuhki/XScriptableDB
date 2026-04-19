using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// レアリティマスターのレコードクラス。
    /// アイテムのレアリティレベルを定義します。
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
        /// レアリティID（主キー）
        /// </summary>
        public int Id => id;

        /// <summary>
        /// レアリティ名
        /// </summary>
        public string Name => name;

        /// <summary>
        /// 表示色
        /// </summary>
        public Color Color => new(colorR, colorG, colorB, 1f);

        /// <summary>
        /// 価格倍率
        /// </summary>
        public float PriceMultiplier => priceMultiplier;

        /// <summary>
        /// ドロップ率
        /// </summary>
        public float DropRate => dropRate;

        /// <summary>
        /// 星の数（レアリティIDと同じ）
        /// </summary>
        public int StarCount => id;

        public override string ToString() => $"[{id}] {name} (x{priceMultiplier})";
    }
}
