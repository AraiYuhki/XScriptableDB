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
        /// キーの文字列表現（デバッグおよび文字列ベースのルックアップ用）。
        /// </summary>
        public string keyString;

        /// <summary>
        /// このキーに対応するレコードインデックスの配列。
        /// </summary>
        public int[] recordIndices;
    }
}