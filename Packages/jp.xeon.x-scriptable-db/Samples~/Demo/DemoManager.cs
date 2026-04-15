using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples.Demo
{
    /// <summary>
    /// Manager that demos XScriptableDB features.
    /// Calls each feature from UI buttons and displays results.
    /// </summary>
    public class DemoManager : MonoBehaviour
    {
        [Header("Tables")]
        [SerializeField] private DemoItemTable itemTable;

        [Header("UI")]
        [SerializeField] private Text outputText;
        [SerializeField] private InputField searchInput;

        private StringBuilder outputBuilder = new StringBuilder();

        private void Start()
        {
            ClearOutput();
            Log("Welcome to the XScriptableDB Demo!");
            Log("Click the buttons on the left to try each feature.");
            Log("");
            Log($"ItemTable: {itemTable?.Count ?? 0} records");
        }

        /// <summary>
        /// Displays all records.
        /// </summary>
        public void ShowAllRecords()
        {
            ClearOutput();
            Log("=== All Records ===");

            foreach (var item in itemTable.All)
            {
                Log($"  {item}");
            }

            Log($"\nTotal: {itemTable.Count} records");
        }

        /// <summary>
        /// Searches by PrimaryKey.
        /// </summary>
        public void SearchByPrimaryKey()
        {
            ClearOutput();
            Log("=== PrimaryKey Search (O(log n)) ===");

            if (!int.TryParse(searchInput.text, out var id))
            {
                Log("Enter an ID (e.g.: 1001)");
                return;
            }

            var item = itemTable.FindByKey(id);
            if (item != null)
            {
                Log($"Found: {item}");
            }
            else
            {
                Log($"ID {id} was not found");
            }
        }

        /// <summary>
        /// Searches by SecondaryKey.
        /// </summary>
        public void SearchBySecondaryKey()
        {
            ClearOutput();
            Log("=== SecondaryKey Search (O(1)) ===");

            var category = searchInput.text;
            if (string.IsNullOrEmpty(category))
            {
                Log("Enter a category (e.g.: Weapon, Armor, Potion)");
                return;
            }

            var items = itemTable.FindAllBySecondaryKeyAsArray("category", category);
            Log($"Category = \"{category}\": {items.Length} results");

            foreach (var item in items)
            {
                Log($"  {item}");
            }
        }

        /// <summary>
        /// Searches by composite SecondaryKey.
        /// </summary>
        public void SearchByCompositeKey()
        {
            ClearOutput();
            Log("=== Composite SecondaryKey Search (O(1)) ===");

            var parts = searchInput.text.Split('/');
            if (parts.Length != 2)
            {
                Log("Enter category/rarity (e.g.: Weapon/3)");
                return;
            }

            var category = parts[0].Trim();
            if (!int.TryParse(parts[1].Trim(), out var rarity))
            {
                Log("Enter rarity as a number");
                return;
            }

            var items = itemTable.FindAllBySecondaryKeyAsArray("CategoryRarity", category, rarity);
            Log($"Category=\"{category}\", Rarity={rarity}: {items.Length} results");

            foreach (var item in items)
            {
                Log($"  {item}");
            }
        }

        /// <summary>
        /// Searches using a Where clause.
        /// </summary>
        public void SearchWithWhere()
        {
            ClearOutput();
            Log("=== Where Search (GC Alloc 0) ===");

            if (!int.TryParse(searchInput.text, out var minPrice))
            {
                minPrice = 500;
            }

            Log($"Items with Price > {minPrice}:");

            var count = 0;
            foreach (var item in itemTable.Where(r => r.Price > minPrice))
            {
                count++;
                Log($"  {item}");
            }
            Log($"  {count} results found");
        }

        /// <summary>
        /// Executes a range search.
        /// </summary>
        public void SearchInRange()
        {
            ClearOutput();
            Log("=== Range Search ===");

            var parts = searchInput.text.Split('-');
            int minId = 1001, maxId = 1005;

            if (parts.Length == 2)
            {
                int.TryParse(parts[0].Trim(), out minId);
                int.TryParse(parts[1].Trim(), out maxId);
            }

            Log($"Items with ID {minId} to {maxId}:");

            foreach (var item in itemTable.FindInRange(minId, maxId))
            {
                Log($"  {item}");
            }
        }

        /// <summary>
        /// Demo of aggregate functions.
        /// </summary>
        public void ShowAggregation()
        {
            ClearOutput();
            Log("=== Aggregate Functions ===");

            // Count
            var weaponCount = itemTable.Count(r => r.Category == "Weapon");
            Log($"Weapon count: {weaponCount}");

            // Any
            var hasExpensive = itemTable.Any(r => r.Price > 5000);
            Log($"Has items over 5000G: {hasExpensive}");

            // All
            var allHavePrice = itemTable.All(r => r.Price > 0);
            Log($"All items have a price: {allHavePrice}");

            // FirstOrDefault
            var cheapest = itemTable.FirstOrDefault(r => r.Price < 100);
            Log($"Cheapest item: {cheapest?.Name ?? "none"}");
        }

        /// <summary>
        /// Performance comparison.
        /// </summary>
        public void ShowPerformanceComparison()
        {
            ClearOutput();
            Log("=== Performance Comparison ===");
            Log("");
            Log("Search method complexity:");
            Log("  PrimaryKey (Find)      : O(log n) - Binary search");
            Log("  SecondaryKey           : O(1)     - Hash lookup");
            Log("  Composite SecondaryKey : O(1)     - Hash lookup");
            Log("  Where                  : O(n)     - Full scan");
            Log("");
            Log("Setting SecondaryKeys appropriately speeds up");
            Log("frequently searched conditions.");
        }

        private void ClearOutput()
        {
            outputBuilder.Clear();
            UpdateOutputText();
        }

        private void Log(string message)
        {
            outputBuilder.AppendLine(message);
            UpdateOutputText();
        }

        private void UpdateOutputText()
        {
            if (outputText != null)
            {
                outputText.text = outputBuilder.ToString();
            }
            else
            {
                Debug.Log(outputBuilder.ToString());
            }
        }
    }
}
