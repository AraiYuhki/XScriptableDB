using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;
using Xeon.XScriptableDB.Validation;

namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// アイテムマスターのレコードクラス。
    /// カテゴリとレアリティへの外部キー参照を保持します。
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
        /// アイテムID（主キー）
        /// </summary>
        public int Id => id;

        /// <summary>
        /// アイテム名
        /// </summary>
        public string Name => name;

        /// <summary>
        /// 説明
        /// </summary>
        public string Description => description;

        /// <summary>
        /// カテゴリID（外部キー）
        /// </summary>
        public int CategoryId => categoryId;

        /// <summary>
        /// レアリティID（外部キー）
        /// </summary>
        public int RarityId => rarityId;

        /// <summary>
        /// 基本価格
        /// </summary>
        public int BasePrice => basePrice;

        /// <summary>
        /// スタック上限
        /// </summary>
        public int StackLimit => stackLimit;

        public override string ToString() => $"[{id}] {name}";
    }
}
