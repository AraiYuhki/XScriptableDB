using UnityEngine;
using Xeon.XScriptableDB;
namespace Xeon.XScriptableDB.Sample
{
    [CreateAssetMenu(fileName = "Item", menuName = "Xeon/XScriptableDB/Item")]
    public class ItemTable : TableAsset<ItemRecord, int>
    {
    }
}
