using UnityEngine;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// Rarity table.
    /// </summary>
    [CreateAssetMenu(
        fileName = "RarityTable",
        menuName = "XScriptableDB/Samples/ForeignKeyReference/RarityTable")]
    public class RarityTable : TableAsset<RarityRecord, int>
    {
    }
}
