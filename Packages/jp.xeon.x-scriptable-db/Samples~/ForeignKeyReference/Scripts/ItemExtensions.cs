namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// アイテム関連の拡張メソッド。
    /// 外部キー参照の取得を簡潔に記述するためのヘルパー。
    /// </summary>
    public static class ItemExtensions
    {
        /// <summary>
        /// アイテムのカテゴリを取得する。
        /// </summary>
        public static CategoryRecord GetCategory(this ItemRecord item, CategoryTable categoryTable)
        {
            return categoryTable.FindByKey(item.CategoryId);
        }

        /// <summary>
        /// アイテムのレアリティを取得する。
        /// </summary>
        public static RarityRecord GetRarity(this ItemRecord item, RarityTable rarityTable)
        {
            return rarityTable.FindByKey(item.RarityId);
        }

        /// <summary>
        /// アイテムの実売価格を計算する（レアリティ倍率適用後）。
        /// </summary>
        public static int GetActualPrice(this ItemRecord item, RarityTable rarityTable)
        {
            var rarity = item.GetRarity(rarityTable);
            if (rarity == null)
                return item.BasePrice;

            return (int)(item.BasePrice * rarity.PriceMultiplier);
        }

        /// <summary>
        /// レシピの完成品アイテムを取得する。
        /// </summary>
        public static ItemRecord GetResultItem(this RecipeRecord recipe, ItemTable itemTable)
        {
            return itemTable.FindByKey(recipe.ResultItemId);
        }

        /// <summary>
        /// レシピの素材1アイテムを取得する。
        /// </summary>
        public static ItemRecord GetMaterial1(this RecipeRecord recipe, ItemTable itemTable)
        {
            if (!recipe.HasMaterial1)
                return null;
            return itemTable.FindByKey(recipe.Material1Id);
        }

        /// <summary>
        /// レシピの素材2アイテムを取得する。
        /// </summary>
        public static ItemRecord GetMaterial2(this RecipeRecord recipe, ItemTable itemTable)
        {
            if (!recipe.HasMaterial2)
                return null;
            return itemTable.FindByKey(recipe.Material2Id);
        }

        /// <summary>
        /// レシピの素材3アイテムを取得する。
        /// </summary>
        public static ItemRecord GetMaterial3(this RecipeRecord recipe, ItemTable itemTable)
        {
            if (!recipe.HasMaterial3)
                return null;
            return itemTable.FindByKey(recipe.Material3Id);
        }
    }
}
