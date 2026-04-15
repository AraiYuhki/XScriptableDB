using System;
using UnityEngine;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples.CompositeIndex
{
    /// <summary>
    /// Sample product record using composite SecondaryKey.
    /// Enables fast lookup by combining Category and SubCategory.
    /// </summary>
    [Serializable]
    public class ProductRecord
    {
        [SerializeField, PrimaryKey]
        private int id;

        [SerializeField]
        private string name;

        // Composite index: CategorySubCategory
        // Fields sharing the same Name attribute are grouped together
        [SerializeField, SecondaryKey("CategorySubCategory",  0)]
        private string category;

        [SerializeField, SecondaryKey("CategorySubCategory", 1)]
        private string subCategory;

        // Single SecondaryKey
        [SerializeField, SecondaryKey]
        private string brand;

        [SerializeField]
        private int price;

        [SerializeField]
        private int stock;

        // Properties
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
