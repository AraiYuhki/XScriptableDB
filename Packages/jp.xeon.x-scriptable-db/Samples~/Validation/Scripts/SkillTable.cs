using UnityEngine;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Samples.Validation
{
    /// <summary>
    /// スキルテーブル。
    /// バリデーションサンプル用です。
    /// </summary>
    [CreateAssetMenu(
        fileName = "SkillTable",
        menuName = "XScriptableDB/Samples/Validation/SkillTable")]
    public class SkillTable : TableAsset<SkillRecord, int>
    {
    }
}
