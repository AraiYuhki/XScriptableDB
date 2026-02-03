using System;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// インデックスエントリ。
    /// </summary>
    [Serializable]
    public class IndexEntry
    {
        /// <summary>
        /// キーのハッシュ値。
        /// </summary>
        public int keyHash;

        /// <summary>
        /// キーの文字列表現（デバッグ・文字列検索用）。
        /// </summary>
        public string keyString;

        /// <summary>
        /// このキーに対応するレコードのインデックス配列。
        /// </summary>
        public int[] recordIndices;
    }
}