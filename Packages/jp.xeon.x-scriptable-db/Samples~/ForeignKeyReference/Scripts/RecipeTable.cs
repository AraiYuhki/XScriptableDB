using UnityEngine;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// Recipe table.
    /// </summary>
    [CreateAssetMenu(
        fileName = "RecipeTable",
        menuName = "XScriptableDB/Samples/ForeignKeyReference/RecipeTable")]
    public class RecipeTable : TableAsset<RecipeRecord, int>
    {
    }
}
