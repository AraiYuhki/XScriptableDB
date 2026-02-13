using UnityEngine;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples.CompositeIndex
{
    /// <summary>
    /// 商品テーブル。
    /// 複合SecondaryKeyによる高速検索をサポート。
    /// </summary>
    [CreateAssetMenu(fileName = "ProductTable", menuName = "XScriptableDB/Samples/ProductTable")]
    public class ProductTable : TableAsset<ProductRecord, int>
    {
        /// <summary>
        /// カテゴリとサブカテゴリで商品を検索する（O(1)）。
        /// </summary>
        /// <param name="category">カテゴリ</param>
        /// <param name="subCategory">サブカテゴリ</param>
        /// <returns>該当する商品の配列</returns>
        public ProductRecord[] FindByCategory(string category, string subCategory)
        {
            return FindAllBySecondaryKeyAsArray("CategorySubCategory", category, subCategory);
        }

        /// <summary>
        /// ブランドで商品を検索する（O(1)）。
        /// </summary>
        /// <param name="brand">ブランド名</param>
        /// <returns>該当する商品の配列</returns>
        public ProductRecord[] FindByBrand(string brand)
        {
            return FindAllBySecondaryKeyAsArray("brand", brand);
        }
    }
}
