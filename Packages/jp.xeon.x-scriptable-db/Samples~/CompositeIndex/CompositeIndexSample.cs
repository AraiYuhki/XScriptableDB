using UnityEngine;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples.CompositeIndex
{
    /// <summary>
    /// 複合SecondaryKeyの使用方法を示すサンプル。
    /// </summary>
    public class CompositeIndexSample : MonoBehaviour
    {
        [SerializeField]
        private ProductTable productTable;

        private void Start()
        {
            if (productTable == null)
            {
                Debug.LogError("ProductTable is not set");
                return;
            }

            DemoCompositeSecondaryKey();
            DemoSingleSecondaryKey();
            DemoComparison();
        }

        /// <summary>
        /// 複合SecondaryKey検索のデモ。
        /// </summary>
        private void DemoCompositeSecondaryKey()
        {
            Debug.Log("=== Composite SecondaryKey Search ===");

            // カテゴリとサブカテゴリの組み合わせで検索（O(1)）
            var electronicsMouse = productTable.FindByCategory("Electronics", "Mouse");
            Debug.Log($"Electronics/Mouse: {electronicsMouse.Length} results");
            foreach (var product in electronicsMouse)
            {
                Debug.Log($"  {product}");
            }

            // 異なる組み合わせ
            var clothingTops = productTable.FindByCategory("Clothing", "Tops");
            Debug.Log($"Clothing/Tops: {clothingTops.Length} results");
            foreach (var product in clothingTops)
            {
                Debug.Log($"  {product}");
            }

            // メソッドを直接使用
            var outdoorBags = productTable.FindAllBySecondaryKeyAsArray(
                "CategorySubCategory", "Outdoor", "Bags");
            Debug.Log($"Outdoor/Bags: {outdoorBags.Length} results");
        }

        /// <summary>
        /// 単一のSecondaryKey検索のデモ。
        /// </summary>
        private void DemoSingleSecondaryKey()
        {
            Debug.Log("=== Single SecondaryKey Search ===");

            // ブランドによる検索（O(1)）
            var techBrandProducts = productTable.FindByBrand("TechBrand");
            Debug.Log($"TechBrand products: {techBrandProducts.Length} results");
            foreach (var product in techBrandProducts)
            {
                Debug.Log($"  {product}");
            }

            var outdoorGearProducts = productTable.FindByBrand("OutdoorGear");
            Debug.Log($"OutdoorGear products: {outdoorGearProducts.Length} results");
        }

        /// <summary>
        /// 複合キーと単一キーの使い分けを示すデモ。
        /// </summary>
        private void DemoComparison()
        {
            Debug.Log("=== Usage Comparison ===");

            // 複合キー：特定のカテゴリ内のサブカテゴリを絞り込むのに最適です
            Debug.Log("Composite key: Search for 'Keyboard' in 'Electronics' category");
            var keyboards = productTable.FindByCategory("Electronics", "Keyboard");
            Debug.Log($"  Result: {keyboards.Length} results (retrieved in O(1))");

            // 単一キー：特定の属性に関する横断的な検索に最適です
            Debug.Log("Single key: Search all products from 'TechBrand'");
            var techProducts = productTable.FindByBrand("TechBrand");
            Debug.Log($"  Result: {techProducts.Length} results (retrieved in O(1))");

            // 複合キーを使用しない場合の比較
            Debug.Log("Reference: Same search using Where() (O(n))");
            var count = productTable.Count(
                p => p.Category == "Electronics" && p.SubCategory == "Keyboard");
            Debug.Log($"  Result: {count} results (requires full scan)");
        }
    }
}
