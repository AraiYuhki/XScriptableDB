using UnityEngine;
using Xeon.XScriptableDB;
namespace Xeon.XScriptableDB.Sample
{
    [CreateAssetMenu(fileName = "Event", menuName = "Xeon/XScriptableDB/Event")]
    public class EventTable : TableAsset<EventRecord, int>
    {
    }
}
