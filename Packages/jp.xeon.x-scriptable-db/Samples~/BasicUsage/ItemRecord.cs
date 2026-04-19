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
        /// PrimaryKey属性を追加することで、バイナリサーチによるO(log n)の高速検索が可能になります。
        /// </summary>
        [PrimaryKey]
        [CsvColumn("ID")]
        public int Id;

        /// <summary>
        /// カテゴリ（副キー）。
        /// SecondaryKey属性を追加することで、ハッシュインデックスによるO(1)の検索が可能になります。
        /// </summary>
        [SecondaryKey]
        [CsvColumn("Category")]
        public string Category;

        /// <summary>
        /// アイテム名。
        /// </summary>
        [CsvColumn("Name")]
        public string Name;

        /// <summary>
        /// 説明。
        /// </summary>
        [CsvColumn("Description")]
        public string Description;

        /// <summary>
        /// 価格。
        /// </summary>
        [CsvColumn("Price")]
        public int Price;

        /// <summary>
        /// 攻撃力。
        /// </summary>
        [CsvColumn("Attack")]
        public int Attack;

        /// <summary>
        /// 防御力。
        /// </summary>
        [CsvColumn("Defense")]
        public int Defense;

        /// <summary>
        /// レアリティ（副キー）。
        /// 複数のSecondaryKeyを設定できます。
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
