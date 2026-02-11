using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// レアリティマスタのレコードクラス。
    /// アイテムの希少度を定義する。
    /// </summary>
    [Serializable]
    public class RarityRecord
    {
        [SerializeField, CsvColumn("ID"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("レアリティ名")]
        private string name;

        [SerializeField, CsvColumn("カラーR")]
        private float colorR;

        [SerializeField, CsvColumn("カラーG")]
        private float colorG;

        [SerializeField, CsvColumn("カラーB")]
        private float colorB;

        [SerializeField, CsvColumn("価格倍率")]
        private float priceMultiplier;

        [SerializeField, CsvColumn("ドロップ率")]
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

        public override string ToString() => $"[{id}] {name} (×{priceMultiplier})";
    }
}
