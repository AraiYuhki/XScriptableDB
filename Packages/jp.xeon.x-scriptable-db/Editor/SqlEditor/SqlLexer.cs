using System.Collections.Generic;
using System.Text;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SQL字句解析器。
    /// </summary>
    public class SqlLexer
    {
        private readonly string input;
        private int position;

        private static readonly Dictionary<string, TokenType> Keywords = new(System.StringComparer.OrdinalIgnoreCase)
        {
            { "SELECT", TokenType.Select },
            { "FROM", TokenType.From },
            { "WHERE", TokenType.Where },
            { "AND", TokenType.And },
            { "OR", TokenType.Or },
            { "NOT", TokenType.Not },
            { "ORDER", TokenType.Order },
            { "BY", TokenType.By },
            { "ASC", TokenType.Asc },
            { "DESC", TokenType.Desc },
            { "LIMIT", TokenType.Limit },
            { "OFFSET", TokenType.Offset },
            { "UPDATE", TokenType.Update },
            { "SET", TokenType.Set },
            { "DELETE", TokenType.Delete },
            { "INSERT", TokenType.Insert },
            { "INTO", TokenType.Into },
            { "VALUES", TokenType.Values },
            { "IN", TokenType.In },
            { "LIKE", TokenType.Like },
            { "IS", TokenType.Is },
            { "NULL", TokenType.Null },
            { "BETWEEN", TokenType.Between },
            // JOIN keywords
            { "JOIN", TokenType.Join },
            { "INNER", TokenType.Inner },
            { "LEFT", TokenType.Left },
            { "RIGHT", TokenType.Right },
            { "CROSS", TokenType.Cross },
            { "OUTER", TokenType.Outer },
            { "ON", TokenType.On },
            // Aggregate keywords
            { "GROUP", TokenType.Group },
            { "HAVING", TokenType.Having },
            { "DISTINCT", TokenType.Distinct },
            { "COUNT", TokenType.Count },
            { "SUM", TokenType.Sum },
            { "AVG", TokenType.Avg },
            { "MIN", TokenType.Min },
            { "MAX", TokenType.Max },
            // Set operations
            { "UNION", TokenType.Union },
            { "INTERSECT", TokenType.Intersect },
            { "EXCEPT", TokenType.Except },
            { "ALL", TokenType.All },
            // CASE expression
            { "CASE", TokenType.Case },
            { "WHEN", TokenType.When },
            { "THEN", TokenType.Then },
            { "ELSE", TokenType.Else },
            { "END", TokenType.End },
            // String functions
            { "UPPER", TokenType.Upper },
            { "LOWER", TokenType.Lower },
            { "CONCAT", TokenType.Concat },
            { "SUBSTRING", TokenType.Substring },
            { "TRIM", TokenType.Trim },
            { "LENGTH", TokenType.Length }
        };

        public SqlLexer(string input)
        {
            this.input = input ?? string.Empty;
            position = 0;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (position < input.Length)
            {
                SkipWhitespace();
                if (position >= input.Length)
                    break;

                var token = NextToken();
                if (token != null)
                    tokens.Add(token);
            }

            tokens.Add(new Token(TokenType.Eof, "", position));
            return tokens;
        }

        private void SkipWhitespace()
        {
            while (position < input.Length && char.IsWhiteSpace(input[position]))
                position++;
        }

        private Token NextToken()
        {
            var startPos = position;
            var c = input[position];

            // Single character tokens
            switch (c)
            {
                case '*': position++; return new Token(TokenType.Star, "*", startPos);
                case ',': position++; return new Token(TokenType.Comma, ",", startPos);
                case '.': position++; return new Token(TokenType.Dot, ".", startPos);
                case '(': position++; return new Token(TokenType.LeftParen, "(", startPos);
                case ')': position++; return new Token(TokenType.RightParen, ")", startPos);
                case '=': position++; return new Token(TokenType.Equal, "=", startPos);
                case '+': position++; return new Token(TokenType.Plus, "+", startPos);
                case '/': position++; return new Token(TokenType.Slash, "/", startPos);
                case '%': position++; return new Token(TokenType.Percent, "%", startPos);
            }

            // Two character operators
            if (c == '!' && position + 1 < input.Length && input[position + 1] == '=')
            {
                position += 2;
                return new Token(TokenType.NotEqual, "!=", startPos);
            }

            if (c == '<')
            {
                if (position + 1 < input.Length && input[position + 1] == '>')
                {
                    position += 2;
                    return new Token(TokenType.NotEqual, "<>", startPos);
                }
                if (position + 1 < input.Length && input[position + 1] == '=')
                {
                    position += 2;
                    return new Token(TokenType.LessOrEqual, "<=", startPos);
                }
                position++;
                return new Token(TokenType.LessThan, "<", startPos);
            }

            if (c == '>')
            {
                if (position + 1 < input.Length && input[position + 1] == '=')
                {
                    position += 2;
                    return new Token(TokenType.GreaterOrEqual, ">=", startPos);
                }
                position++;
                return new Token(TokenType.GreaterThan, ">", startPos);
            }

            // Minus (演算子として処理)
            if (c == '-')
            {
                position++;
                return new Token(TokenType.Minus, "-", startPos);
            }

            // String literal
            if (c == '\'' || c == '"')
                return ReadStringLiteral(c);

            // Number literal
            if (char.IsDigit(c))
                return ReadNumberLiteral();

            // Identifier or keyword
            if (char.IsLetter(c) || c == '_')
                return ReadIdentifierOrKeyword();

            // Unknown character
            position++;
            return new Token(TokenType.Unknown, c.ToString(), startPos);
        }

        private Token ReadStringLiteral(char quote)
        {
            var startPos = position;
            position++; // Skip opening quote

            var sb = new StringBuilder();
            while (position < input.Length)
            {
                var c = input[position];
                if (c == quote)
                {
                    // Check for escaped quote
                    if (position + 1 < input.Length && input[position + 1] == quote)
                    {
                        sb.Append(quote);
                        position += 2;
                        continue;
                    }
                    position++; // Skip closing quote
                    break;
                }
                sb.Append(c);
                position++;
            }

            return new Token(TokenType.StringLiteral, sb.ToString(), startPos);
        }

        private Token ReadNumberLiteral()
        {
            var startPos = position;
            var sb = new StringBuilder();

            while (position < input.Length && (char.IsDigit(input[position]) || input[position] == '.'))
            {
                sb.Append(input[position]);
                position++;
            }

            return new Token(TokenType.NumberLiteral, sb.ToString(), startPos);
        }

        private Token ReadIdentifierOrKeyword()
        {
            var startPos = position;
            var sb = new StringBuilder();

            while (position < input.Length && (char.IsLetterOrDigit(input[position]) || input[position] == '_'))
            {
                sb.Append(input[position]);
                position++;
            }

            var value = sb.ToString();

            // Check if it's a keyword
            if (Keywords.TryGetValue(value, out var keywordType))
                return new Token(keywordType, value, startPos);

            return new Token(TokenType.Identifier, value, startPos);
        }
    }
}
