using UnityEngine;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples
{
    /// <summary>
    /// アイテムテーブル。
    /// TableAsset&lt;TRecord, TKey&gt;を継承して作成します。
    /// </summary>
    /// <remarks>
    /// CreateAssetMenuを使用すると、Unityメニューから作成可能になります。
    /// </remarks>
    [CreateAssetMenu(fileName = "ItemTable", menuName = "XScriptableDB/Samples/ItemTable")]
    public class ItemTable : TableAsset<ItemRecord, int>
    {
        // TableAssetのすべての基本機能が継承されます。
        // 必要に応じてここに追加のメソッドやプロパティを実装します。

        /// <summary>
        /// 指定されたカテゴリのアイテムを取得する便利なメソッド。
        /// </summary>
        public ItemRecord[] GetItemsByCategory(string category)
        {
            return FindAllBySecondaryKeyAsArray("Category", category);
        }

        /// <summary>
        /// 指定されたレアリティのアイテムを取得する便利なメソッド。
        /// </summary>
        public ItemRecord[] GetItemsByRarity(int rarity)
        {
            return FindAllBySecondaryKeyAsArray("Rarity", rarity);
        }
    }
}
