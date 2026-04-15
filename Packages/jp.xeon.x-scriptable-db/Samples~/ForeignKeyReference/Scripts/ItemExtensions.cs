namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// Extension methods related to items.
    /// Helpers for concisely retrieving foreign key references.
    /// </summary>
    public static class ItemExtensions
    {
        /// <summary>
        /// Gets the category of an item.
        /// </summary>
        public static CategoryRecord GetCategory(this ItemRecord item, CategoryTable categoryTable)
        {
            return categoryTable.FindByKey(item.CategoryId);
        }

        /// <summary>
        /// Gets the rarity of an item.
        /// </summary>
        public static RarityRecord GetRarity(this ItemRecord item, RarityTable rarityTable)
        {
            return rarityTable.FindByKey(item.RarityId);
        }

        /// <summary>
        /// Calculates the actual selling price of an item (after applying the rarity multiplier).
        /// </summary>
        public static int GetActualPrice(this ItemRecord item, RarityTable rarityTable)
        {
            var rarity = item.GetRarity(rarityTable);
            if (rarity == null)
                return item.BasePrice;

            return (int)(item.BasePrice * rarity.PriceMultiplier);
        }

        /// <summary>
        /// Gets the result item of a recipe.
        /// </summary>
        public static ItemRecord GetResultItem(this RecipeRecord recipe, ItemTable itemTable)
        {
            return itemTable.FindByKey(recipe.ResultItemId);
        }

        /// <summary>
        /// Gets material 1 of a recipe.
        /// </summary>
        public static ItemRecord GetMaterial1(this RecipeRecord recipe, ItemTable itemTable)
        {
            if (!recipe.HasMaterial1)
                return null;
            return itemTable.FindByKey(recipe.Material1Id);
        }

        /// <summary>
        /// Gets material 2 of a recipe.
        /// </summary>
        public static ItemRecord GetMaterial2(this RecipeRecord recipe, ItemTable itemTable)
        {
            if (!recipe.HasMaterial2)
                return null;
            return itemTable.FindByKey(recipe.Material2Id);
        }

        /// <summary>
        /// Gets material 3 of a recipe.
        /// </summary>
        public static ItemRecord GetMaterial3(this RecipeRecord recipe, ItemTable itemTable)
        {
            if (!recipe.HasMaterial3)
                return null;
            return itemTable.FindByKey(recipe.Material3Id);
        }
    }
}
