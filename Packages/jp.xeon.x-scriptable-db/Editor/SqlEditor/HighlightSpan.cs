using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// A highlighted span of text.
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