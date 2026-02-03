using System;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace XScriptableDB.Samples
{
    /// <summary>
    /// アイテムデータのレコードクラス。
    /// </summary>
    [Serializable]
    public class ItemRecord
    {
        /// <summary>
        /// アイテムID（主キー）。
        /// PrimaryKey属性を付けることで、バイナリサーチによるO(log n)の高速検索が可能。
        /// </summary>
        [PrimaryKey]
        [CsvColumn("ID")]
        public int Id;

        /// <summary>
        /// カテゴリ（副キー）。
        /// SecondaryKey属性を付けることで、ハッシュインデックスによるO(1)の検索が可能。
        /// </summary>
        [SecondaryKey]
        [CsvColumn("カテゴリ")]
        public string Category;

        /// <summary>
        /// アイテム名。
        /// </summary>
        [CsvColumn("名前")]
        public string Name;

        /// <summary>
        /// 説明文。
        /// </summary>
        [CsvColumn("説明")]
        public string Description;

        /// <summary>
        /// 価格。
        /// </summary>
        [CsvColumn("価格")]
        public int Price;

        /// <summary>
        /// 攻撃力。
        /// </summary>
        [CsvColumn("攻撃力")]
        public int Attack;

        /// <summary>
        /// 防御力。
        /// </summary>
        [CsvColumn("防御力")]
        public int Defense;

        /// <summary>
        /// レアリティ（副キー）。
        /// 複数のSecondaryKeyを設定可能。
        /// </summary>
        [SecondaryKey]
        [CsvColumn("レアリティ")]
        public int Rarity;

        public override string ToString()
        {
            return $"[{Id}] {Name} ({Category}) - {Price}G";
        }
    }
}
