using UnityEngine;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples.CompositeIndex
{
    /// <summary>
    /// 製品テーブル。
    /// 複合SecondaryKeyによる高速検索をサポートします。
    /// </summary>
    [CreateAssetMenu(fileName = "ProductTable", menuName = "XScriptableDB/Samples/ProductTable")]
    public class ProductTable : TableAsset<ProductRecord, int>
    {
        /// <summary>
        /// カテゴリとサブカテゴリで製品を検索します（O(1)）。
        /// </summary>
        /// <param name="category">カテゴリ</param>
        /// <param name="subCategory">サブカテゴリ</param>
        /// <returns>一致する製品の配列</returns>
        public ProductRecord[] FindByCategory(string category, string subCategory)
        {
            return FindAllBySecondaryKeyAsArray("CategorySubCategory", category, subCategory);
        }

        /// <summary>
        /// ブランドで製品を検索します（O(1)）。
        /// </summary>
        /// <param name="brand">ブランド名</param>
        /// <returns>一致する製品の配列</returns>
        public ProductRecord[] FindByBrand(string brand)
        {
            return FindAllBySecondaryKeyAsArray("brand", brand);
        }
    }
}
