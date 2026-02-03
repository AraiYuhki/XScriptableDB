using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// Foreign Key Referenceサンプルのロジック部分。
    /// GUIから呼び出されることを想定。
    /// </summary>
    public class ForeignKeyReferenceSample : MonoBehaviour
    {
        [SerializeField]
        private CategoryTable categoryTable;

        [SerializeField]
        private RarityTable rarityTable;

        [SerializeField]
        private ItemTable itemTable;

        [SerializeField]
        private RecipeTable recipeTable;

        /// <summary>
        /// アイテムの詳細情報を取得する。
        /// </summary>
        public ItemDetailInfo GetItemDetail(int itemId)
        {
            var item = itemTable.Find(itemId);
            if (item == null)
                return null;

            var category = item.GetCategory(categoryTable);
            var rarity = item.GetRarity(rarityTable);
            var actualPrice = item.GetActualPrice(rarityTable);

            // このアイテムを作れるレシピを検索
            var recipe = recipeTable.All.FirstOrDefault(r => r.ResultItemId == itemId);

            return new ItemDetailInfo
            {
                Item = item,
                Category = category,
                Rarity = rarity,
                ActualPrice = actualPrice,
                Recipe = recipe
            };
        }

        /// <summary>
        /// カテゴリでフィルタリングしたアイテム一覧を取得する。
        /// </summary>
        public IEnumerable<ItemRecord> GetItemsByCategory(int categoryId)
        {
            return itemTable.FindAllBySecondaryKey("categoryId", categoryId);
        }

        /// <summary>
        /// レアリティでフィルタリングしたアイテム一覧を取得する。
        /// </summary>
        public IEnumerable<ItemRecord> GetItemsByRarity(int rarityId)
        {
            return itemTable.FindAllBySecondaryKey("rarityId", rarityId);
        }

        /// <summary>
        /// 指定レアリティ以上のアイテム一覧を取得する。
        /// </summary>
        public IEnumerable<ItemRecord> GetItemsByMinRarity(int minRarityId)
        {
            return itemTable.All.Where(item => item.RarityId >= minRarityId);
        }

        /// <summary>
        /// レシピの詳細情報を取得する。
        /// </summary>
        public RecipeDetailInfo GetRecipeDetail(int recipeId)
        {
            var recipe = recipeTable.Find(recipeId);
            if (recipe == null)
                return null;

            var resultItem = recipe.GetResultItem(itemTable);
            var materials = new List<MaterialInfo>();

            if (recipe.HasMaterial1)
            {
                var mat = recipe.GetMaterial1(itemTable);
                if (mat != null)
                    materials.Add(new MaterialInfo { Item = mat, Count = recipe.Material1Count });
            }

            if (recipe.HasMaterial2)
            {
                var mat = recipe.GetMaterial2(itemTable);
                if (mat != null)
                    materials.Add(new MaterialInfo { Item = mat, Count = recipe.Material2Count });
            }

            if (recipe.HasMaterial3)
            {
                var mat = recipe.GetMaterial3(itemTable);
                if (mat != null)
                    materials.Add(new MaterialInfo { Item = mat, Count = recipe.Material3Count });
            }

            return new RecipeDetailInfo
            {
                Recipe = recipe,
                ResultItem = resultItem,
                ResultCount = recipe.ResultCount,
                Materials = materials
            };
        }

        /// <summary>
        /// 指定アイテムを素材として使用しているレシピを検索する。
        /// </summary>
        public IEnumerable<RecipeRecord> FindRecipesUsingItem(int itemId)
        {
            return recipeTable.All.Where(r =>
                r.Material1Id == itemId ||
                r.Material2Id == itemId ||
                r.Material3Id == itemId);
        }

        /// <summary>
        /// 全カテゴリを取得する（表示順でソート済み）。
        /// </summary>
        public IEnumerable<CategoryRecord> GetAllCategories()
        {
            return categoryTable.All.OrderBy(c => c.SortOrder);
        }

        /// <summary>
        /// 全レアリティを取得する。
        /// </summary>
        public IEnumerable<RarityRecord> GetAllRarities()
        {
            return rarityTable.All;
        }

        /// <summary>
        /// 全アイテムを取得する。
        /// </summary>
        public IEnumerable<ItemRecord> GetAllItems()
        {
            return itemTable.All;
        }

        /// <summary>
        /// 全レシピを取得する。
        /// </summary>
        public IEnumerable<RecipeRecord> GetAllRecipes()
        {
            return recipeTable.All;
        }
    }

    /// <summary>
    /// アイテム詳細情報。
    /// </summary>
    public class ItemDetailInfo
    {
        public ItemRecord Item { get; set; }
        public CategoryRecord Category { get; set; }
        public RarityRecord Rarity { get; set; }
        public int ActualPrice { get; set; }
        public RecipeRecord Recipe { get; set; }

        public bool HasRecipe => Recipe != null;
    }

    /// <summary>
    /// レシピ詳細情報。
    /// </summary>
    public class RecipeDetailInfo
    {
        public RecipeRecord Recipe { get; set; }
        public ItemRecord ResultItem { get; set; }
        public int ResultCount { get; set; }
        public List<MaterialInfo> Materials { get; set; }
    }

    /// <summary>
    /// 素材情報。
    /// </summary>
    public class MaterialInfo
    {
        public ItemRecord Item { get; set; }
        public int Count { get; set; }
    }
}
