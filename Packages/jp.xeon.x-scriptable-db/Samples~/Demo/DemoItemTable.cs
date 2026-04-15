using UnityEngine;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples.Demo
{
    /// <summary>
    /// Demo item table.
    /// </summary>
    [CreateAssetMenu(fileName = "DemoItemTable", menuName = "XScriptableDB/Samples/DemoItemTable")]
    public class DemoItemTable : TableAsset<DemoItemRecord, int>
    {
    }
}
