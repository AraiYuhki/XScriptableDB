namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// アイテムに関する拡張メソッド。
    /// 外部キー参照を簡潔に取得するためのヘルパーです。
    /// </summary>
    public static class ItemExtensions
    {
        /// <summary>
        /// アイテムのカテゴリを取得します。
        /// </summary>
        public static CategoryRecord GetCategory(this ItemRecord item, CategoryTable categoryTable)
        {
            return categoryTable.FindByKey(item.CategoryId);
        }

        /// <summary>
        /// アイテムのレアリティを取得します。
        /// </summary>
        public static RarityRecord GetRarity(this ItemRecord item, RarityTable rarityTable)
        {
            return rarityTable.FindByKey(item.RarityId);
        }

        /// <summary>
        /// （レアリティ倍率を適用した）アイテムの実際の販売価格を計算します。
        /// </summary>
        public static int GetActualPrice(this ItemRecord item, RarityTable rarityTable)
        {
            var rarity = item.GetRarity(rarityTable);
            if (rarity == null)
                return item.BasePrice;

            return (int)(item.BasePrice * rarity.PriceMultiplier);
        }

        /// <summary>
        /// レシピの完成品アイテムを取得します。
        /// </summary>
        public static ItemRecord GetResultItem(this RecipeRecord recipe, ItemTable itemTable)
        {
            return itemTable.FindByKey(recipe.ResultItemId);
        }

        /// <summary>
        /// レシピの素材1を取得します。
        /// </summary>
        public static ItemRecord GetMaterial1(this RecipeRecord recipe, ItemTable itemTable)
        {
            if (!recipe.HasMaterial1)
                return null;
            return itemTable.FindByKey(recipe.Material1Id);
        }

        /// <summary>
        /// レシピの素材2を取得します。
        /// </summary>
        public static ItemRecord GetMaterial2(this RecipeRecord recipe, ItemTable itemTable)
        {
            if (!recipe.HasMaterial2)
                return null;
            return itemTable.FindByKey(recipe.Material2Id);
        }

        /// <summary>
        /// レシピの素材3を取得します。
        /// </summary>
        public static ItemRecord GetMaterial3(this RecipeRecord recipe, ItemTable itemTable)
        {
            if (!recipe.HasMaterial3)
                return null;
            return itemTable.FindByKey(recipe.Material3Id);
        }
    }
}
