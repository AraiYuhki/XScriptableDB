using UnityEngine;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Samples.QueryResultZeroGC
{
    /// <summary>
    /// Enemy table.
    /// For the Zero GC sample.
    /// </summary>
    [CreateAssetMenu(
        fileName = "EnemyTable",
        menuName = "XScriptableDB/Samples/QueryResultZeroGC/EnemyTable")]
    public class EnemyTable : TableAsset<EnemyRecord, int>
    {
    }
}
