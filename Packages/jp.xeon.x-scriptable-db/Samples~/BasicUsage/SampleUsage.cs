using System.Linq;
using UnityEngine;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples
{
    /// <summary>
    /// Sample demonstrating basic usage of XScriptableDB.
    /// </summary>
    public class SampleUsage : MonoBehaviour
    {
        [SerializeField]
        private ItemTable itemTable;

        private void Start()
        {
            if (itemTable == null)
            {
                Debug.LogError("ItemTable is not set");
                return;
            }

            // ========================================
            // Basic search
            // ========================================

            // Search by PrimaryKey (O(log n))
            Debug.Log("=== PrimaryKey Search ===");
            var item = itemTable.FindByKey(1001);
            if (item != null)
            {
                Debug.Log($"Found: {item}");
            }

            // Safe search with TryFindByKey
            if (itemTable.TryFindByKey(1002, out var item2))
            {
                Debug.Log($"Found with TryFindByKey: {item2}");
            }

            // ========================================
            // Search by SecondaryKey
            // ========================================

            Debug.Log("=== SecondaryKey Search ===");

            // Search by category (O(1))
            var weapons = itemTable.FindAllBySecondaryKeyAsArray("Category", "Weapon");
            Debug.Log($"Weapons count: {weapons.Length}");
            foreach (var weapon in weapons)
            {
                Debug.Log($"  - {weapon}");
            }

            // Search by rarity
            var rareItems = itemTable.FindAllBySecondaryKeyAsArray("Rarity", 3);
            Debug.Log($"Rarity 3 items: {rareItems.Length}");

            // ========================================
            // Search using QueryResult (GC Alloc 0)
            // ========================================

            Debug.Log("=== QueryResult Search (GC Alloc 0) ===");

            // Get a zero-GC-alloc QueryResult with QueryBySecondaryKey
            var result = itemTable.QueryBySecondaryKey("Category", "Weapon");
            Debug.Log($"Weapons via QueryResult: {result.Count}");
            foreach (var weaponItem in result)
            {
                Debug.Log($"  - {weaponItem.Name}: {weaponItem.Price}G");
            }

            // Conditional search with Where() (returns IEnumerable<T>)
            var expensiveItems = itemTable.Where(r => r.Price > 500);
            Debug.Log("Items with price > 500:");
            foreach (var expensiveItem in expensiveItems)
            {
                Debug.Log($"  - {expensiveItem.Name}: {expensiveItem.Price}G");
            }

            // ========================================
            // Extension methods
            // ========================================

            Debug.Log("=== Extension Methods ===");

            // FirstOrDefault
            var firstWeapon = itemTable.FirstOrDefault(r => r.Category == "Weapon");
            if (firstWeapon != null)
            {
                Debug.Log($"First weapon: {firstWeapon}");
            }

            // Any
            bool hasExpensiveItems = itemTable.Any(r => r.Price > 10000);
            Debug.Log($"Has expensive items (>10000G): {hasExpensiveItems}");

            // Count
            int armorCount = itemTable.Count(r => r.Category == "Armor");
            Debug.Log($"Armor count: {armorCount}");

            // All
            bool allHavePrice = itemTable.All(r => r.Price > 0);
            Debug.Log($"All items have price > 0: {allHavePrice}");

            // ========================================
            // Table convenience methods (custom implementation)
            // ========================================

            Debug.Log("=== Custom Methods ===");

            // Convenience methods implemented in ItemTable
            var armors = itemTable.GetItemsByCategory("Armor");
            Debug.Log($"Armors: {armors.Length}");

            var legendaryItems = itemTable.GetItemsByRarity(5);
            Debug.Log($"Legendary items: {legendaryItems.Length}");
        }
    }
}
