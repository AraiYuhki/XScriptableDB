using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// Logic for the Foreign Key Reference sample.
    /// Intended to be called from a GUI.
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
        /// Gets detailed information for an item.
        /// </summary>
        public ItemDetailInfo GetItemDetail(int itemId)
        {
            var item = itemTable.FindByKey(itemId);
            if (item == null)
                return null;

            var category = item.GetCategory(categoryTable);
            var rarity = item.GetRarity(rarityTable);
            var actualPrice = item.GetActualPrice(rarityTable);

            // Search for recipes that can craft this item
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
        /// Gets a list of items filtered by category.
        /// </summary>
        public IEnumerable<ItemRecord> GetItemsByCategory(int categoryId)
        {
            return itemTable.FindAllBySecondaryKey("categoryId", categoryId);
        }

        /// <summary>
        /// Gets a list of items filtered by rarity.
        /// </summary>
        public IEnumerable<ItemRecord> GetItemsByRarity(int rarityId)
        {
            return itemTable.FindAllBySecondaryKey("rarityId", rarityId);
        }

        /// <summary>
        /// Gets a list of items with at least the specified rarity.
        /// </summary>
        public IEnumerable<ItemRecord> GetItemsByMinRarity(int minRarityId)
        {
            return itemTable.All.Where(item => item.RarityId >= minRarityId);
        }

        /// <summary>
        /// Gets detailed information for a recipe.
        /// </summary>
        public RecipeDetailInfo GetRecipeDetail(int recipeId)
        {
            var recipe = recipeTable.FindByKey(recipeId);
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
        /// Searches for recipes that use the specified item as a material.
        /// </summary>
        public IEnumerable<RecipeRecord> FindRecipesUsingItem(int itemId)
        {
            return recipeTable.All.Where(r =>
                r.Material1Id == itemId ||
                r.Material2Id == itemId ||
                r.Material3Id == itemId);
        }

        /// <summary>
        /// Gets all categories (sorted by display order).
        /// </summary>
        public IEnumerable<CategoryRecord> GetAllCategories()
        {
            return categoryTable.All.OrderBy(c => c.SortOrder);
        }

        /// <summary>
        /// Gets all rarities.
        /// </summary>
        public IEnumerable<RarityRecord> GetAllRarities()
        {
            return rarityTable.All;
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        public IEnumerable<ItemRecord> GetAllItems()
        {
            return itemTable.All;
        }

        /// <summary>
        /// Gets all recipes.
        /// </summary>
        public IEnumerable<RecipeRecord> GetAllRecipes()
        {
            return recipeTable.All;
        }
    }

    /// <summary>
    /// Item detail information.
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
    /// Recipe detail information.
    /// </summary>
    public class RecipeDetailInfo
    {
        public RecipeRecord Recipe { get; set; }
        public ItemRecord ResultItem { get; set; }
        public int ResultCount { get; set; }
        public List<MaterialInfo> Materials { get; set; }
    }

    /// <summary>
    /// Material information.
    /// </summary>
    public class MaterialInfo
    {
        public ItemRecord Item { get; set; }
        public int Count { get; set; }
    }
}
