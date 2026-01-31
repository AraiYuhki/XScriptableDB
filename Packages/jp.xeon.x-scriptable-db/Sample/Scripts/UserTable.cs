using UnityEngine;

namespace Xeon.XScriptableDB.Sample
{
    [CreateAssetMenu(fileName = "UserTable", menuName = "Xeon/XScriptableDB/Sample/UserTable")]
    public class UserTable : TableAsset<UserRecord, int>
    {

    }
}
