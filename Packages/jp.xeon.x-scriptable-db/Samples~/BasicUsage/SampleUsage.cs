using System.Linq;
using UnityEngine;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples
{
    /// <summary>
    /// XScriptableDBの基本的な使い方を示すサンプル。
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
            // 基本的な検索
            // ========================================

            // PrimaryKeyによる検索（O(log n)）
            Debug.Log("=== PrimaryKey Search ===");
            var item = itemTable.FindByKey(1001);
            if (item != null)
            {
                Debug.Log($"Found: {item}");
            }

            // TryFindByKeyによる安全な検索
            if (itemTable.TryFindByKey(1002, out var item2))
            {
                Debug.Log($"Found with TryFindByKey: {item2}");
            }

            // ========================================
            // SecondaryKeyによる検索
            // ========================================

            Debug.Log("=== SecondaryKey Search ===");

            // カテゴリによる検索（O(1)）
            var weapons = itemTable.FindAllBySecondaryKeyAsArray("Category", "Weapon");
            Debug.Log($"Weapons count: {weapons.Length}");
            foreach (var weapon in weapons)
            {
                Debug.Log($"  - {weapon}");
            }

            // レアリティによる検索
            var rareItems = itemTable.FindAllBySecondaryKeyAsArray("Rarity", 3);
            Debug.Log($"Rarity 3 items: {rareItems.Length}");

            // ========================================
            // QueryResultを使用した検索（GC Alloc 0）
            // ========================================

            Debug.Log("=== QueryResult Search (GC Alloc 0) ===");

            // QueryBySecondaryKeyでGC AllocなしのQueryResultを取得
            var result = itemTable.QueryBySecondaryKey("Category", "Weapon");
            Debug.Log($"Weapons via QueryResult: {result.Count}");
            foreach (var weaponItem in result)
            {
                Debug.Log($"  - {weaponItem.Name}: {weaponItem.Price}G");
            }

            // Where()による条件付き検索（IEnumerable<T>を返します）
            var expensiveItems = itemTable.Where(r => r.Price > 500);
            Debug.Log("Items with price > 500:");
            foreach (var expensiveItem in expensiveItems)
            {
                Debug.Log($"  - {expensiveItem.Name}: {expensiveItem.Price}G");
            }

            // ========================================
            // 拡張メソッド
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
            // テーブルの便利メソッド（カスタム実装）
            // ========================================

            Debug.Log("=== Custom Methods ===");

            // ItemTableに実装された便利メソッド
            var armors = itemTable.GetItemsByCategory("Armor");
            Debug.Log($"Armors: {armors.Length}");

            var legendaryItems = itemTable.GetItemsByRarity(5);
            Debug.Log($"Legendary items: {legendaryItems.Length}");
        }
    }
}
