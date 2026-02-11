using UnityEngine;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Samples.Validation
{
    /// <summary>
    /// 武器テーブル。
    /// 外部キー検証サンプル用。
    /// </summary>
    [CreateAssetMenu(
        fileName = "WeaponTable",
        menuName = "XScriptableDB/Samples/Validation/WeaponTable")]
    public class WeaponTable : TableAsset<WeaponRecord, int>
    {
    }
}
