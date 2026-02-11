using UnityEngine;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// カテゴリテーブル。
    /// </summary>
    [CreateAssetMenu(
        fileName = "CategoryTable",
        menuName = "XScriptableDB/Samples/ForeignKeyReference/CategoryTable")]
    public class CategoryTable : TableAsset<CategoryRecord, int>
    {
    }
}
