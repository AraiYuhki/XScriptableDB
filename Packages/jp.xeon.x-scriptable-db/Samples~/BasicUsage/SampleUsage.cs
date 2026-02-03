using UnityEngine;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples
{
    /// <summary>
    /// XScriptableDBの基本的な使い方のサンプル。
    /// </summary>
    public class SampleUsage : MonoBehaviour
    {
        [SerializeField]
        private ItemTable itemTable;

        private void Start()
        {
            if (itemTable == null)
            {
                Debug.LogError("ItemTableが設定されていません");
                return;
            }

            // ========================================
            // 基本的な検索
            // ========================================

            // PrimaryKeyで検索（O(log n)）
            Debug.Log("=== PrimaryKey検索 ===");
            var item = itemTable.Find(1001);
            if (item != null)
            {
                Debug.Log($"Found: {item}");
            }

            // TryFindで安全に検索
            if (itemTable.TryFind(1002, out var item2))
            {
                Debug.Log($"Found with TryFind: {item2}");
            }

            // ========================================
            // SecondaryKeyによる検索
            // ========================================

            Debug.Log("=== SecondaryKey検索 ===");

            // カテゴリで検索（O(1)）
            var weapons = itemTable.FindAllBySecondaryKey("Category", "Weapon");
            Debug.Log($"Weapons count: {weapons.Length}");
            foreach (var weapon in weapons)
            {
                Debug.Log($"  - {weapon}");
            }

            // レアリティで検索
            var rareItems = itemTable.FindAllBySecondaryKey("Rarity", 3);
            Debug.Log($"Rarity 3 items: {rareItems.Length}");

            // ========================================
            // QueryResultを使用した検索（GC Alloc 0）
            // ========================================

            Debug.Log("=== QueryResult検索（GC Alloc 0）===");

            // usingステートメントで自動的にDisposeされます
            using (var result = itemTable.Where(r => r.Price > 500))
            {
                Debug.Log($"Items with price > 500: {result.Count}");

                // foreachで列挙（ref readonlyでコピーを避ける）
                foreach (ref readonly var expensiveItem in result)
                {
                    Debug.Log($"  - {expensiveItem.Name}: {expensiveItem.Price}G");
                }
            }

            // 複合条件
            using (var result = itemTable.Where(r => r.Category == "Weapon" && r.Attack > 20))
            {
                Debug.Log($"Strong weapons: {result.Count}");
            }

            // ========================================
            // 拡張メソッド
            // ========================================

            Debug.Log("=== 拡張メソッド ===");

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

            Debug.Log("=== カスタムメソッド ===");

            // ItemTableに実装した便利メソッド
            var armors = itemTable.GetItemsByCategory("Armor");
            Debug.Log($"Armors: {armors.Length}");

            var legendaryItems = itemTable.GetItemsByRarity(5);
            Debug.Log($"Legendary items: {legendaryItems.Length}");
        }
    }
}
