using UnityEngine;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Samples.Validation
{
    /// <summary>
    /// Weapon table.
    /// For the foreign key validation sample.
    /// </summary>
    [CreateAssetMenu(
        fileName = "WeaponTable",
        menuName = "XScriptableDB/Samples/Validation/WeaponTable")]
    public class WeaponTable : TableAsset<WeaponRecord, int>
    {
    }
}
