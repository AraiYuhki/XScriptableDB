using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SQLシンタックスハイライター。
    /// </summary>
    public class SqlSyntaxHighlighter
    {
        private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "AND", "OR", "NOT",
            "ORDER", "BY", "ASC", "DESC", "LIMIT", "OFFSET",
            "UPDATE", "SET", "DELETE", "INSERT", "INTO", "VALUES",
            "IN", "LIKE", "IS", "NULL", "BETWEEN", "AS",
            "DISTINCT", "ALL", "JOIN", "LEFT", "RIGHT", "INNER", "OUTER",
            "ON", "GROUP", "HAVING", "UNION", "EXCEPT", "INTERSECT",
            "TRUE", "FALSE"
        };

        private SqlHighlightColors colors;

        public SqlSyntaxHighlighter(SqlHighlightColors colors = null)
        {
            this.colors = colors ?? SqlHighlightColors.Default;
        }

        /// <summary>
        /// 配色を設定する。
        /// </summary>
        public void SetColors(SqlHighlightColors newColors)
        {
            colors = newColors ?? SqlHighlightColors.Default;
        }

        /// <summary>
        /// SQLテキストをハイライトする。
        /// </summary>
        /// <param name="sql">SQL文字列</param>
        /// <returns>ハイライト情報のリスト</returns>
        public List<HighlightSpan> Highlight(string sql)
        {
            var spans = new List<HighlightSpan>();
            if (string.IsNullOrEmpty(sql)) return spans;

            var lexer = new SqlLexer(sql);
            var tokens = lexer.Tokenize();

            foreach (var token in tokens)
            {
                if (token.Type == TokenType.Eof) continue;

                var color = GetColorForToken(token);
                spans.Add(new HighlightSpan
                {
                    Start = token.Position,
                    Length = token.Value.Length,
                    Color = color,
                    TokenType = token.Type
                });
            }

            return spans;
        }

        /// <summary>
        /// トークンの色を取得する。
        /// </summary>
        private Color GetColorForToken(Token token)
        {
            // キーワード
            if (IsKeywordToken(token.Type))
                return colors.KeywordColor;

            switch (token.Type)
            {
                case TokenType.StringLiteral:
                    return colors.StringColor;

                case TokenType.NumberLiteral:
                    return colors.NumberColor;

                case TokenType.Identifier:
                    // キーワードかもしれない（ASなど）
                    if (Keywords.Contains(token.Value))
                        return colors.KeywordColor;
                    return colors.IdentifierColor;

                case TokenType.Star:
                case TokenType.Comma:
                case TokenType.Dot:
                case TokenType.LeftParen:
                case TokenType.RightParen:
                case TokenType.Equal:
                case TokenType.NotEqual:
                case TokenType.LessThan:
                case TokenType.LessOrEqual:
                case TokenType.GreaterThan:
                case TokenType.GreaterOrEqual:
                    return colors.OperatorColor;

                case TokenType.Unknown:
                    return colors.ErrorColor;

                default:
                    return colors.IdentifierColor;
            }
        }

        /// <summary>
        /// トークンがキーワードかどうか。
        /// </summary>
        private bool IsKeywordToken(TokenType type)
        {
            return type switch
            {
                TokenType.Select or TokenType.From or TokenType.Where or
                TokenType.And or TokenType.Or or TokenType.Not or
                TokenType.Order or TokenType.By or TokenType.Asc or TokenType.Desc or
                TokenType.Limit or TokenType.Offset or
                TokenType.Update or TokenType.Set or
                TokenType.Delete or TokenType.Insert or TokenType.Into or TokenType.Values or
                TokenType.In or TokenType.Like or TokenType.Is or TokenType.Null or TokenType.Between
                    => true,
                _ => false
            };
        }

        /// <summary>
        /// SQLをリッチテキスト形式に変換する。
        /// </summary>
        /// <param name="sql">SQL文字列</param>
        /// <returns>リッチテキスト（Unityのcolor tag使用）</returns>
        public string ToRichText(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return string.Empty;

            var spans = Highlight(sql);
            if (spans.Count == 0) return EscapeRichText(sql);

            var sb = new StringBuilder();
            var lastEnd = 0;

            // 文字列リテラルの場合、クォートも含める必要がある
            foreach (var span in spans)
            {
                // スパン前のテキスト
                if (span.Start > lastEnd)
                {
                    sb.Append(EscapeRichText(sql.Substring(lastEnd, span.Start - lastEnd)));
                }

                // 文字列リテラルの場合は元のクォートを含める
                var spanText = span.TokenType == TokenType.StringLiteral
                    ? GetOriginalStringLiteral(sql, span.Start, span.Length)
                    : span.Length > 0 ? sql.Substring(span.Start, span.Length) : "";

                var colorHex = ColorUtility.ToHtmlStringRGB(span.Color);
                sb.Append($"<color=#{colorHex}>{EscapeRichText(spanText)}</color>");

                lastEnd = span.TokenType == TokenType.StringLiteral
                    ? GetStringLiteralEnd(sql, span.Start)
                    : span.End;
            }

            // 残りのテキスト
            if (lastEnd < sql.Length)
            {
                sb.Append(EscapeRichText(sql.Substring(lastEnd)));
            }

            return sb.ToString();
        }

        /// <summary>
        /// 文字列リテラルの元の表記（クォート含む）を取得する。
        /// </summary>
        private string GetOriginalStringLiteral(string sql, int start, int innerLength)
        {
            if (start >= sql.Length) return "";

            var quote = sql[start];
            if (quote != '\'' && quote != '"')
            {
                // クォートでない場合は通常のテキスト
                return innerLength > 0 && start + innerLength <= sql.Length
                    ? sql.Substring(start, innerLength)
                    : "";
            }

            // クォートから終端までを取得
            var end = GetStringLiteralEnd(sql, start);
            return sql.Substring(start, end - start);
        }

        /// <summary>
        /// 文字列リテラルの終端位置を取得する。
        /// </summary>
        private int GetStringLiteralEnd(string sql, int start)
        {
            if (start >= sql.Length) return start;

            var quote = sql[start];
            if (quote != '\'' && quote != '"') return start;

            var i = start + 1;
            while (i < sql.Length)
            {
                if (sql[i] == quote)
                {
                    // エスケープされたクォートかチェック
                    if (i + 1 < sql.Length && sql[i + 1] == quote)
                    {
                        i += 2;
                        continue;
                    }
                    return i + 1;
                }
                i++;
            }
            return i;
        }

        /// <summary>
        /// リッチテキストで特殊文字をエスケープする。
        /// </summary>
        private string EscapeRichText(string text)
        {
            return text
                .Replace("<", "<<")
                .Replace(">", ">>");
        }

        /// <summary>
        /// エラー位置をハイライトしたリッチテキストを生成する。
        /// </summary>
        /// <param name="sql">SQL文字列</param>
        /// <param name="errorPosition">エラー位置</param>
        /// <param name="errorLength">エラーの長さ（デフォルト1）</param>
        /// <returns>リッチテキスト</returns>
        public string ToRichTextWithError(string sql, int errorPosition, int errorLength = 1)
        {
            if (string.IsNullOrEmpty(sql)) return string.Empty;

            var richText = ToRichText(sql);

            // エラー位置に下線を追加（Unity UIは下線をサポートしていないため、色で表現）
            if (errorPosition >= 0 && errorPosition < sql.Length)
            {
                // 簡易的な実装：エラー位置の文字を赤くする
                // TODO: より正確なエラーハイライトの実装
            }

            return richText;
        }
    }
}
