using UnityEngine;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples.CompositeIndex
{
    /// <summary>
    /// Product table.
    /// Supports fast lookup via composite SecondaryKey.
    /// </summary>
    [CreateAssetMenu(fileName = "ProductTable", menuName = "XScriptableDB/Samples/ProductTable")]
    public class ProductTable : TableAsset<ProductRecord, int>
    {
        /// <summary>
        /// Searches for products by category and subcategory (O(1)).
        /// </summary>
        /// <param name="category">Category</param>
        /// <param name="subCategory">Subcategory</param>
        /// <returns>Array of matching products</returns>
        public ProductRecord[] FindByCategory(string category, string subCategory)
        {
            return FindAllBySecondaryKeyAsArray("CategorySubCategory", category, subCategory);
        }

        /// <summary>
        /// Searches for products by brand (O(1)).
        /// </summary>
        /// <param name="brand">Brand name</param>
        /// <returns>Array of matching products</returns>
        public ProductRecord[] FindByBrand(string brand)
        {
            return FindAllBySecondaryKeyAsArray("brand", brand);
        }
    }
}
