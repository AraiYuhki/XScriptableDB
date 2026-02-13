using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples.Demo
{
    /// <summary>
    /// XScriptableDBの機能をデモするマネージャー。
    /// UIボタンから各機能を呼び出して結果を表示する。
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
            Log("XScriptableDB デモへようこそ！");
            Log("左側のボタンをクリックして各機能を試してください。");
            Log("");
            Log($"ItemTable: {itemTable?.Count ?? 0}件のレコード");
        }

        /// <summary>
        /// 全レコードを表示する。
        /// </summary>
        public void ShowAllRecords()
        {
            ClearOutput();
            Log("=== 全レコード ===");

            foreach (var item in itemTable.All)
            {
                Log($"  {item}");
            }

            Log($"\n合計: {itemTable.Count}件");
        }

        /// <summary>
        /// PrimaryKeyで検索する。
        /// </summary>
        public void SearchByPrimaryKey()
        {
            ClearOutput();
            Log("=== PrimaryKey検索（O(log n)） ===");

            if (!int.TryParse(searchInput.text, out var id))
            {
                Log("IDを入力してください（例: 1001）");
                return;
            }

            var item = itemTable.FindByKey(id);
            if (item != null)
            {
                Log($"Found: {item}");
            }
            else
            {
                Log($"ID {id} は見つかりませんでした");
            }
        }

        /// <summary>
        /// SecondaryKeyで検索する。
        /// </summary>
        public void SearchBySecondaryKey()
        {
            ClearOutput();
            Log("=== SecondaryKey検索（O(1)） ===");

            var category = searchInput.text;
            if (string.IsNullOrEmpty(category))
            {
                Log("カテゴリを入力してください（例: Weapon, Armor, Potion）");
                return;
            }

            var items = itemTable.FindAllBySecondaryKeyAsArray("category", category);
            Log($"Category = \"{category}\": {items.Length}件");

            foreach (var item in items)
            {
                Log($"  {item}");
            }
        }

        /// <summary>
        /// 複合SecondaryKeyで検索する。
        /// </summary>
        public void SearchByCompositeKey()
        {
            ClearOutput();
            Log("=== 複合SecondaryKey検索（O(1)） ===");

            var parts = searchInput.text.Split('/');
            if (parts.Length != 2)
            {
                Log("カテゴリ/レアリティを入力してください（例: Weapon/3）");
                return;
            }

            var category = parts[0].Trim();
            if (!int.TryParse(parts[1].Trim(), out var rarity))
            {
                Log("レアリティは数字で入力してください");
                return;
            }

            var items = itemTable.FindAllBySecondaryKeyAsArray("CategoryRarity", category, rarity);
            Log($"Category=\"{category}\", Rarity={rarity}: {items.Length}件");

            foreach (var item in items)
            {
                Log($"  {item}");
            }
        }

        /// <summary>
        /// Where句で検索する。
        /// </summary>
        public void SearchWithWhere()
        {
            ClearOutput();
            Log("=== Where検索（GC Alloc 0） ===");

            if (!int.TryParse(searchInput.text, out var minPrice))
            {
                minPrice = 500;
            }

            Log($"Price > {minPrice} のアイテム:");

            var count = 0;
            foreach (var item in itemTable.Where(r => r.Price > minPrice))
            {
                count++;
                Log($"  {item}");
            }
            Log($"  {count}件見つかりました");
        }

        /// <summary>
        /// 範囲検索を実行する。
        /// </summary>
        public void SearchInRange()
        {
            ClearOutput();
            Log("=== 範囲検索 ===");

            var parts = searchInput.text.Split('-');
            int minId = 1001, maxId = 1005;

            if (parts.Length == 2)
            {
                int.TryParse(parts[0].Trim(), out minId);
                int.TryParse(parts[1].Trim(), out maxId);
            }

            Log($"ID {minId} ～ {maxId} のアイテム:");

            foreach (var item in itemTable.FindInRange(minId, maxId))
            {
                Log($"  {item}");
            }
        }

        /// <summary>
        /// 集計関数のデモ。
        /// </summary>
        public void ShowAggregation()
        {
            ClearOutput();
            Log("=== 集計関数 ===");

            // Count
            var weaponCount = itemTable.Count(r => r.Category == "Weapon");
            Log($"武器の数: {weaponCount}");

            // Any
            var hasExpensive = itemTable.Any(r => r.Price > 5000);
            Log($"5000G以上のアイテムあり: {hasExpensive}");

            // All
            var allHavePrice = itemTable.All(r => r.Price > 0);
            Log($"全アイテムに価格あり: {allHavePrice}");

            // FirstOrDefault
            var cheapest = itemTable.FirstOrDefault(r => r.Price < 100);
            Log($"最安値アイテム: {cheapest?.Name ?? "なし"}");
        }

        /// <summary>
        /// パフォーマンス比較。
        /// </summary>
        public void ShowPerformanceComparison()
        {
            ClearOutput();
            Log("=== パフォーマンス比較 ===");
            Log("");
            Log("検索方法の計算量:");
            Log("  PrimaryKey (Find)      : O(log n) - バイナリサーチ");
            Log("  SecondaryKey           : O(1)     - ハッシュルックアップ");
            Log("  複合SecondaryKey       : O(1)     - ハッシュルックアップ");
            Log("  Where                  : O(n)     - 全件スキャン");
            Log("");
            Log("SecondaryKeyを適切に設定することで、");
            Log("頻繁に検索する条件を高速化できます。");
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
