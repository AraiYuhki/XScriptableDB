using UnityEngine;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples
{
    /// <summary>
    /// Item table.
    /// Created by inheriting TableAsset&lt;TRecord, TKey&gt;.
    /// </summary>
    /// <remarks>
    /// Using CreateAssetMenu makes it creatable from the Unity menu.
    /// </remarks>
    [CreateAssetMenu(fileName = "ItemTable", menuName = "XScriptableDB/Samples/ItemTable")]
    public class ItemTable : TableAsset<ItemRecord, int>
    {
        // All base functionality of TableAsset is inherited.
        // Implement additional methods or properties here if needed.

        /// <summary>
        /// Convenience method to retrieve items of the specified category.
        /// </summary>
        public ItemRecord[] GetItemsByCategory(string category)
        {
            return FindAllBySecondaryKeyAsArray("Category", category);
        }

        /// <summary>
        /// Convenience method to retrieve items of the specified rarity.
        /// </summary>
        public ItemRecord[] GetItemsByRarity(int rarity)
        {
            return FindAllBySecondaryKeyAsArray("Rarity", rarity);
        }
    }
}
