using UnityEngine;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples
{
    /// <summary>
    /// アイテムテーブル。
    /// TableAsset<TRecord, TKey>を継承して作成します。
    /// </summary>
    /// <remarks>
    /// CreateAssetMenuを使用することで、Unityのメニューから作成可能になります。
    /// </remarks>
    [CreateAssetMenu(fileName = "ItemTable", menuName = "XScriptableDB/Samples/ItemTable")]
    public class ItemTable : TableAsset<ItemRecord, int>
    {
        // TableAssetの基本機能はすべて継承されます。
        // 追加のメソッドやプロパティが必要な場合はここに実装します。

        /// <summary>
        /// 指定カテゴリのアイテムを取得する便利メソッド。
        /// </summary>
        public ItemRecord[] GetItemsByCategory(string category)
        {
            return FindAllBySecondaryKeyAsArray("Category", category);
        }

        /// <summary>
        /// 指定レアリティのアイテムを取得する便利メソッド。
        /// </summary>
        public ItemRecord[] GetItemsByRarity(int rarity)
        {
            return FindAllBySecondaryKeyAsArray("Rarity", rarity);
        }
    }
}
