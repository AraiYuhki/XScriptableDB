using UnityEngine;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples.CompositeIndex
{
    /// <summary>
    /// Sample demonstrating how to use composite SecondaryKeys.
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
        /// Demo of composite SecondaryKey search.
        /// </summary>
        private void DemoCompositeSecondaryKey()
        {
            Debug.Log("=== Composite SecondaryKey Search ===");

            // Search by category + subcategory (O(1))
            var electronicsMouse = productTable.FindByCategory("Electronics", "Mouse");
            Debug.Log($"Electronics/Mouse: {electronicsMouse.Length} results");
            foreach (var product in electronicsMouse)
            {
                Debug.Log($"  {product}");
            }

            // Different combination
            var clothingTops = productTable.FindByCategory("Clothing", "Tops");
            Debug.Log($"Clothing/Tops: {clothingTops.Length} results");
            foreach (var product in clothingTops)
            {
                Debug.Log($"  {product}");
            }

            // Using the method directly
            var outdoorBags = productTable.FindAllBySecondaryKeyAsArray(
                "CategorySubCategory", "Outdoor", "Bags");
            Debug.Log($"Outdoor/Bags: {outdoorBags.Length} results");
        }

        /// <summary>
        /// Demo of single SecondaryKey search.
        /// </summary>
        private void DemoSingleSecondaryKey()
        {
            Debug.Log("=== Single SecondaryKey Search ===");

            // Search by brand (O(1))
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
        /// Demo showing when to use composite vs single keys.
        /// </summary>
        private void DemoComparison()
        {
            Debug.Log("=== Usage Comparison ===");

            // Composite key: best for narrowing down subcategories within a specific category
            Debug.Log("Composite key: Search for 'Keyboard' in 'Electronics' category");
            var keyboards = productTable.FindByCategory("Electronics", "Keyboard");
            Debug.Log($"  Result: {keyboards.Length} results (retrieved in O(1))");

            // Single key: best for cross-cutting searches on a specific attribute
            Debug.Log("Single key: Search all products from 'TechBrand'");
            var techProducts = productTable.FindByBrand("TechBrand");
            Debug.Log($"  Result: {techProducts.Length} results (retrieved in O(1))");

            // Comparison without using composite key
            Debug.Log("Reference: Same search using Where() (O(n))");
            var count = productTable.Count(
                p => p.Category == "Electronics" && p.SubCategory == "Keyboard");
            Debug.Log($"  Result: {count} results (requires full scan)");
        }
    }
}
