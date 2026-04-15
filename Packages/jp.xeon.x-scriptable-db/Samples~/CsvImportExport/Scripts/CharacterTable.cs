using UnityEngine;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Samples.CsvImportExport
{
    /// <summary>
    /// Character table.
    /// For the CSV/TSV import/export sample.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CharacterTable",
        menuName = "XScriptableDB/Samples/CsvImportExport/CharacterTable")]
    public class CharacterTable : TableAsset<CharacterRecord, int>
    {
    }
}
