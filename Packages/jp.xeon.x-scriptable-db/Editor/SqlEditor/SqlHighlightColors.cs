using System;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SQLシンタックスハイライトの設定。
    /// </summary>
    [Serializable]
    public class SqlHighlightColors
    {
        public Color KeywordColor = new Color(0.34f, 0.61f, 0.84f);      // 青
        public Color StringColor = new Color(0.81f, 0.55f, 0.31f);       // オレンジ
        public Color NumberColor = new Color(0.71f, 0.84f, 0.66f);       // 緑
        public Color OperatorColor = new Color(0.85f, 0.85f, 0.85f);     // 薄グレー
        public Color IdentifierColor = new Color(0.78f, 0.78f, 0.55f);   // 黄
        public Color CommentColor = new Color(0.5f, 0.5f, 0.5f);         // グレー
        public Color ErrorColor = new Color(1f, 0.4f, 0.4f);             // 赤

        /// <summary>
        /// デフォルトの配色を取得する。
        /// </summary>
        public static SqlHighlightColors Default => new();

        /// <summary>
        /// ライトテーマ用の配色を取得する。
        /// </summary>
        public static SqlHighlightColors Light => new()
        {
            KeywordColor = new Color(0.0f, 0.0f, 0.6f),
            StringColor = new Color(0.6f, 0.2f, 0.0f),
            NumberColor = new Color(0.0f, 0.5f, 0.0f),
            OperatorColor = new Color(0.3f, 0.3f, 0.3f),
            IdentifierColor = new Color(0.4f, 0.4f, 0.0f),
            CommentColor = new Color(0.4f, 0.4f, 0.4f),
            ErrorColor = new Color(0.8f, 0.0f, 0.0f)
        };
    }
}