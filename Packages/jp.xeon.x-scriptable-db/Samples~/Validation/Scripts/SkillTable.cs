using UnityEngine;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Samples.Validation
{
    /// <summary>
    /// Skill table.
    /// For the validation sample.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SkillTable",
        menuName = "XScriptableDB/Samples/Validation/SkillTable")]
    public class SkillTable : TableAsset<SkillRecord, int>
    {
    }
}
