using UnityEngine;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Samples.CsvImportExport
{
    /// <summary>
    /// キャラクターテーブル。
    /// CSV/TSVインポート・エクスポートのサンプル用。
    /// </summary>
    [CreateAssetMenu(
        fileName = "CharacterTable",
        menuName = "XScriptableDB/Samples/CsvImportExport/CharacterTable")]
    public class CharacterTable : TableAsset<CharacterRecord, int>
    {
    }
}
