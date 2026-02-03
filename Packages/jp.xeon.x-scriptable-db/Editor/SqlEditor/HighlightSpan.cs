using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// ハイライトされたテキスト部分。
    /// </summary>
    public struct HighlightSpan
    {
        public int Start;
        public int Length;
        public Color Color;
        public TokenType TokenType;

        public int End => Start + Length;
    }
}