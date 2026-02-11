using System;
using UnityEngine;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples.CompositeIndex
{
    /// <summary>
    /// 複合SecondaryKeyを使用した商品レコードのサンプル。
    /// CategoryとSubCategoryの組み合わせで高速検索が可能。
    /// </summary>
    [Serializable]
    public class ProductRecord
    {
        [SerializeField, PrimaryKey]
        private int id;

        [SerializeField]
        private string name;

        // 複合インデックス: CategorySubCategory
        // 同じName属性を持つフィールドがグループ化される
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
            return $"[{Id}] {Name} ({Category}/{SubCategory}) - {Brand} - {Price}円 (在庫:{Stock})";
        }
    }
}
