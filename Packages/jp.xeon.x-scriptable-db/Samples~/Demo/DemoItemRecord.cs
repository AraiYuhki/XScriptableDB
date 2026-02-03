using System;
using UnityEngine;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples.Demo
{
    /// <summary>
    /// デモ用のアイテムレコード。
    /// </summary>
    [Serializable]
    public class DemoItemRecord
    {
        [SerializeField, PrimaryKey]
        private int id;

        [SerializeField]
        private string name;

        [SerializeField, SecondaryKey]
        private string category;

        [SerializeField]
        private int rarity;

        [SerializeField]
        private int price;

        // 複合インデックス: Category + Rarity
        [SecondaryKey(Name = "CategoryRarity", Order = 0)]
        public string CategoryForIndex => category;

        [SecondaryKey(Name = "CategoryRarity", Order = 1)]
        public int RarityForIndex => rarity;

        public int Id => id;
        public string Name => name;
        public string Category => category;
        public int Rarity => rarity;
        public int Price => price;

        public override string ToString()
        {
            var stars = new string('*', rarity);
            return $"[{Id}] {Name} ({Category}) {stars} - {Price}G";
        }
    }
}
