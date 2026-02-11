using UnityEngine;
using Xeon.XScriptableDB;

namespace XScriptableDB.Samples.Demo
{
    /// <summary>
    /// デモ用のアイテムテーブル。
    /// </summary>
    [CreateAssetMenu(fileName = "DemoItemTable", menuName = "XScriptableDB/Samples/DemoItemTable")]
    public class DemoItemTable : TableAsset<DemoItemRecord, int>
    {
    }
}
