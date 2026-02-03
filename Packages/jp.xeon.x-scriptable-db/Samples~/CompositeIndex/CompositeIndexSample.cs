using UnityEngine;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples.CompositeIndex
{
    /// <summary>
    /// 複合SecondaryKeyの使い方を示すサンプル。
    /// </summary>
    public class CompositeIndexSample : MonoBehaviour
    {
        [SerializeField]
        private ProductTable productTable;

        private void Start()
        {
            if (productTable == null)
            {
                Debug.LogError("ProductTableが設定されていません");
                return;
            }

            DemoCompositeSecondaryKey();
            DemoSingleSecondaryKey();
            DemoComparison();
        }

        /// <summary>
        /// 複合SecondaryKeyによる検索のデモ。
        /// </summary>
        private void DemoCompositeSecondaryKey()
        {
            Debug.Log("=== 複合SecondaryKey検索 ===");

            // カテゴリ + サブカテゴリで検索（O(1)）
            var electronicsMouse = productTable.FindByCategory("Electronics", "Mouse");
            Debug.Log($"Electronics/Mouse: {electronicsMouse.Length}件");
            foreach (var product in electronicsMouse)
            {
                Debug.Log($"  {product}");
            }

            // 別の組み合わせ
            var clothingTops = productTable.FindByCategory("Clothing", "Tops");
            Debug.Log($"Clothing/Tops: {clothingTops.Length}件");
            foreach (var product in clothingTops)
            {
                Debug.Log($"  {product}");
            }

            // 直接メソッドを使用する場合
            var outdoorBags = productTable.FindAllByCompositeSecondaryKey(
                "CategorySubCategory", "Outdoor", "Bags");
            Debug.Log($"Outdoor/Bags: {outdoorBags.Length}件");
        }

        /// <summary>
        /// 単一SecondaryKeyによる検索のデモ。
        /// </summary>
        private void DemoSingleSecondaryKey()
        {
            Debug.Log("=== 単一SecondaryKey検索 ===");

            // ブランドで検索（O(1)）
            var techBrandProducts = productTable.FindByBrand("TechBrand");
            Debug.Log($"TechBrand製品: {techBrandProducts.Length}件");
            foreach (var product in techBrandProducts)
            {
                Debug.Log($"  {product}");
            }

            var outdoorGearProducts = productTable.FindByBrand("OutdoorGear");
            Debug.Log($"OutdoorGear製品: {outdoorGearProducts.Length}件");
        }

        /// <summary>
        /// 複合キーと単一キーの使い分けのデモ。
        /// </summary>
        private void DemoComparison()
        {
            Debug.Log("=== 使い分けの例 ===");

            // 複合キー: 特定のカテゴリ内のサブカテゴリを絞り込む場合に最適
            Debug.Log("複合キー: 「Electronics」カテゴリの「Keyboard」を検索");
            var keyboards = productTable.FindByCategory("Electronics", "Keyboard");
            Debug.Log($"  結果: {keyboards.Length}件（O(1)で取得）");

            // 単一キー: 特定の属性で横断的に検索する場合に最適
            Debug.Log("単一キー: 「TechBrand」の全製品を検索");
            var techProducts = productTable.FindByBrand("TechBrand");
            Debug.Log($"  結果: {techProducts.Length}件（O(1)で取得）");

            // 複合キーを使わない場合との比較
            Debug.Log("参考: Where()で同じ検索をした場合（O(n)）");
            using var result = productTable.Where(
                p => p.Category == "Electronics" && p.SubCategory == "Keyboard");
            Debug.Log($"  結果: {result.Count}件（全件スキャンが必要）");
        }
    }
}
