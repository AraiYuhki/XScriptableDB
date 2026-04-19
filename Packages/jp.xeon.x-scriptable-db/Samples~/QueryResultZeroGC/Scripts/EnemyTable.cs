using UnityEngine;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Samples.QueryResultZeroGC
{
    /// <summary>
    /// エネミーテーブル。
    /// Zero GCサンプル用です。
    /// </summary>
    [CreateAssetMenu(
        fileName = "EnemyTable",
        menuName = "XScriptableDB/Samples/QueryResultZeroGC/EnemyTable")]
    public class EnemyTable : TableAsset<EnemyRecord, int>
    {
    }
}
