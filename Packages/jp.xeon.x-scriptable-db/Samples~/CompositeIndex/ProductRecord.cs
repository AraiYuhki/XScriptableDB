using System;
using UnityEngine;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples.CompositeIndex
{
    /// <summary>
    /// 複合SecondaryKeyを使用した製品レコードのサンプル。
    /// カテゴリとサブカテゴリを組み合わせることで高速検索を可能にします。
    /// </summary>
    [Serializable]
    public class ProductRecord
    {
        [SerializeField, PrimaryKey]
        private int id;

        [SerializeField]
        private string name;

        // 複合インデックス：CategorySubCategory
        // 同じName属性を共有するフィールドがグループ化されます
        [SerializeField, SecondaryKey("CategorySubCategory",  0)]
        private string category;

        [SerializeField, SecondaryKey("CategorySubCategory", 1)]
        private string subCategory;

        // 単一のSecondaryKey
        [SerializeField, SecondaryKey]
        private string brand;

        [SerializeField]
        private int price;

        [SerializeField]
        private int stock;

        // プロパティ
        public int Id => id;
        public string Name => name;
        public string Category => category;
        public string SubCategory => subCategory;
        public string Brand => brand;
        public int Price => price;
        public int Stock => stock;

        public override string ToString()
        {
            return $"[{Id}] {Name} ({Category}/{SubCategory}) - {Brand} - {Price}G (Stock:{Stock})";
        }
    }
}
