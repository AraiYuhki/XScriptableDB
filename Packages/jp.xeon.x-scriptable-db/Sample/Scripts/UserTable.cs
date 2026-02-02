using UnityEngine;
using Xeon.XScriptableDB;
namespace Xeon.XScriptableDB.Sample
{
    [CreateAssetMenu(fileName = "User", menuName = "Xeon/XScriptableDB/User")]
    public class UserTable : TableAsset<UserRecord, int>
    {
    }
}
