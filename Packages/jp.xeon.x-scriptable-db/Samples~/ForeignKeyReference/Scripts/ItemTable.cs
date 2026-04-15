using UnityEngine;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// Item table.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ItemTable",
        menuName = "XScriptableDB/Samples/ForeignKeyReference/ItemTable")]
    public class ItemTable : TableAsset<ItemRecord, int>
    {
    }
}
