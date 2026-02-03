using UnityEngine;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// アイテムテーブル。
    /// </summary>
    [CreateAssetMenu(
        fileName = "ItemTable",
        menuName = "XScriptableDB/Samples/ForeignKeyReference/ItemTable")]
    public class ItemTable : TableAsset<ItemRecord, int>
    {
    }
}
