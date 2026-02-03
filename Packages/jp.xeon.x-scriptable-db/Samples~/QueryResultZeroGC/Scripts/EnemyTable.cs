using UnityEngine;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Samples.QueryResultZeroGC
{
    /// <summary>
    /// 敵テーブル。
    /// Zero GCサンプル用。
    /// </summary>
    [CreateAssetMenu(
        fileName = "EnemyTable",
        menuName = "XScriptableDB/Samples/QueryResultZeroGC/EnemyTable")]
    public class EnemyTable : TableAsset<EnemyRecord, int>
    {
    }
}
