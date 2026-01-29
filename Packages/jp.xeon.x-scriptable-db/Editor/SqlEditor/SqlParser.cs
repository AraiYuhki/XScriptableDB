using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SQLトークンの種類。
    /// </summary>
    public enum TokenType
    {
        // Keywords
        Select, From, Where, And, Or, Not,
        Order, By, Asc, Desc, Limit, Offset,
        Update, Set, Delete, Insert, Into, Values,
        In, Like, Is, Null, Between,

        // Symbols
        Star, Comma, Dot, LeftParen, RightParen,
        Equal, NotEqual, LessThan, LessOrEqual, GreaterThan, GreaterOrEqual,

        // Literals
        Identifier, StringLiteral, NumberLiteral,

        // Special
        Eof, Unknown
    }

    /// <summary>
    /// SQLトークン。
    /// </summary>
    public class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }
        public int Position { get; set; }

        public Token(TokenType type, string value, int position)
        {
            Type = type;
            Value = value;
            Position = position;
        }

        public override string ToString() => $"{Type}: {Value}";
    }

    /// <summary>
    /// SQL字句解析器。
    /// </summary>
    public class SqlLexer
    {
        private readonly string input;
        private int position;

        private static readonly Dictionary<string, TokenType> Keywords = new(StringComparer.OrdinalIgnoreCase)
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
            { "BETWEEN", TokenType.Between }
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

            // String literal
            if (c == '\'' || c == '"')
            {
                return ReadStringLiteral(c);
            }

            // Number literal
            if (char.IsDigit(c) || (c == '-' && position + 1 < input.Length && char.IsDigit(input[position + 1])))
            {
                return ReadNumberLiteral();
            }

            // Identifier or keyword
            if (char.IsLetter(c) || c == '_')
            {
                return ReadIdentifierOrKeyword();
            }

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

            if (input[position] == '-')
            {
                sb.Append('-');
                position++;
            }

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

    /// <summary>
    /// SQL構文解析器。
    /// </summary>
    public class SqlParser
    {
        private List<Token> tokens;
        private int current;

        /// <summary>
        /// SQL文を解析する。
        /// </summary>
        /// <param name="sql">SQL文字列</param>
        /// <returns>解析されたSQL文</returns>
        public SqlStatement Parse(string sql)
        {
            var lexer = new SqlLexer(sql);
            tokens = lexer.Tokenize();
            current = 0;

            if (Check(TokenType.Select))
                return ParseSelect();

            if (Check(TokenType.Update))
                return ParseUpdate();

            if (Check(TokenType.Delete))
                return ParseDelete();

            throw new SqlParseException("Expected SELECT, UPDATE, or DELETE", CurrentPosition);
        }

        private SelectStatement ParseSelect()
        {
            var stmt = new SelectStatement();

            Expect(TokenType.Select, "SELECT");
            stmt.Columns = ParseSelectColumns();

            Expect(TokenType.From, "FROM");
            stmt.TableName = ParseTableName();

            if (Match(TokenType.Where))
                stmt.WhereClause = ParseExpression();

            if (Match(TokenType.Order))
            {
                Expect(TokenType.By, "BY");
                stmt.OrderBy = ParseOrderBy();
            }

            if (Match(TokenType.Limit))
            {
                var limitToken = Expect(TokenType.NumberLiteral, "number");
                stmt.Limit = int.Parse(limitToken.Value);
            }

            if (Match(TokenType.Offset))
            {
                var offsetToken = Expect(TokenType.NumberLiteral, "number");
                stmt.Offset = int.Parse(offsetToken.Value);
            }

            return stmt;
        }

        private UpdateStatement ParseUpdate()
        {
            var stmt = new UpdateStatement();

            Expect(TokenType.Update, "UPDATE");
            stmt.TableName = ParseTableName();

            Expect(TokenType.Set, "SET");
            stmt.SetItems = ParseSetItems();

            if (Match(TokenType.Where))
                stmt.WhereClause = ParseExpression();

            return stmt;
        }

        private DeleteStatement ParseDelete()
        {
            var stmt = new DeleteStatement();

            Expect(TokenType.Delete, "DELETE");
            Expect(TokenType.From, "FROM");
            stmt.TableName = ParseTableName();

            if (Match(TokenType.Where))
                stmt.WhereClause = ParseExpression();

            return stmt;
        }

        private List<SelectColumn> ParseSelectColumns()
        {
            var columns = new List<SelectColumn>();

            do
            {
                if (Match(TokenType.Star))
                {
                    columns.Add(new SelectColumn { IsWildcard = true });
                }
                else
                {
                    var column = new SelectColumn
                    {
                        Expression = ParseColumnExpression()
                    };

                    // Check for AS alias
                    if (MatchKeyword("AS"))
                    {
                        var aliasToken = Expect(TokenType.Identifier, "alias");
                        column.Alias = aliasToken.Value;
                    }

                    columns.Add(column);
                }
            } while (Match(TokenType.Comma));

            return columns;
        }

        private string ParseTableName()
        {
            var token = Expect(TokenType.Identifier, "table name");
            return token.Value;
        }

        private List<SetItem> ParseSetItems()
        {
            var items = new List<SetItem>();

            do
            {
                var columnToken = Expect(TokenType.Identifier, "column name");
                Expect(TokenType.Equal, "=");
                var value = ParseValue();

                items.Add(new SetItem
                {
                    ColumnName = columnToken.Value,
                    Value = value
                });
            } while (Match(TokenType.Comma));

            return items;
        }

        private List<OrderByItem> ParseOrderBy()
        {
            var items = new List<OrderByItem>();

            do
            {
                var item = new OrderByItem
                {
                    Expression = ParseColumnExpression()
                };

                if (Match(TokenType.Desc))
                    item.Order = SortOrder.Descending;
                else
                    Match(TokenType.Asc); // Optional ASC

                items.Add(item);
            } while (Match(TokenType.Comma));

            return items;
        }

        private SqlExpression ParseExpression()
        {
            return ParseOrExpression();
        }

        private SqlExpression ParseOrExpression()
        {
            var left = ParseAndExpression();

            while (Match(TokenType.Or))
            {
                var right = ParseAndExpression();
                left = new LogicalExpression
                {
                    Left = left,
                    Operator = LogicalOperator.Or,
                    Right = right
                };
            }

            return left;
        }

        private SqlExpression ParseAndExpression()
        {
            var left = ParseComparisonExpression();

            while (Match(TokenType.And))
            {
                var right = ParseComparisonExpression();
                left = new LogicalExpression
                {
                    Left = left,
                    Operator = LogicalOperator.And,
                    Right = right
                };
            }

            return left;
        }

        private SqlExpression ParseComparisonExpression()
        {
            // Handle parentheses
            if (Match(TokenType.LeftParen))
            {
                var expr = ParseExpression();
                Expect(TokenType.RightParen, ")");
                return expr;
            }

            var left = ParseValue();

            // IS NULL / IS NOT NULL
            if (Match(TokenType.Is))
            {
                var isNot = Match(TokenType.Not);
                Expect(TokenType.Null, "NULL");

                return new ComparisonExpression
                {
                    Left = left,
                    Operator = isNot ? ComparisonOperator.IsNotNull : ComparisonOperator.IsNull
                };
            }

            // Comparison operators
            ComparisonOperator? op = null;

            if (Match(TokenType.Equal)) op = ComparisonOperator.Equal;
            else if (Match(TokenType.NotEqual)) op = ComparisonOperator.NotEqual;
            else if (Match(TokenType.LessThan)) op = ComparisonOperator.LessThan;
            else if (Match(TokenType.LessOrEqual)) op = ComparisonOperator.LessOrEqual;
            else if (Match(TokenType.GreaterThan)) op = ComparisonOperator.GreaterThan;
            else if (Match(TokenType.GreaterOrEqual)) op = ComparisonOperator.GreaterOrEqual;
            else if (Match(TokenType.Like)) op = ComparisonOperator.Like;
            else if (Match(TokenType.In))
            {
                var inList = ParseInList();
                return new ComparisonExpression
                {
                    Left = left,
                    Operator = ComparisonOperator.In,
                    Right = inList
                };
            }

            if (op == null)
                return left;

            var right = ParseValue();

            return new ComparisonExpression
            {
                Left = left,
                Operator = op.Value,
                Right = right
            };
        }

        private InListExpression ParseInList()
        {
            Expect(TokenType.LeftParen, "(");

            var list = new InListExpression();
            do
            {
                list.Values.Add(ParseValue());
            } while (Match(TokenType.Comma));

            Expect(TokenType.RightParen, ")");
            return list;
        }

        private SqlExpression ParseValue()
        {
            if (Match(TokenType.StringLiteral))
                return new LiteralExpression(Previous().Value);

            if (Match(TokenType.NumberLiteral))
            {
                var value = Previous().Value;
                if (value.Contains("."))
                    return new LiteralExpression(double.Parse(value, CultureInfo.InvariantCulture));
                return new LiteralExpression(long.Parse(value));
            }

            if (Match(TokenType.Null))
                return new LiteralExpression(null);

            return ParseColumnExpression();
        }

        private SqlExpression ParseColumnExpression()
        {
            var token = Expect(TokenType.Identifier, "column name");
            var columnName = token.Value;
            string tableAlias = null;

            // Check for table.column notation
            if (Match(TokenType.Dot))
            {
                tableAlias = columnName;
                var columnToken = Expect(TokenType.Identifier, "column name");
                columnName = columnToken.Value;
            }

            return new ColumnExpression(columnName, tableAlias);
        }

        #region Helper Methods

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Peek().Type == type;
        }

        private bool Match(TokenType type)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
            return false;
        }

        private bool MatchKeyword(string keyword)
        {
            if (IsAtEnd()) return false;
            var token = Peek();
            if (token.Type == TokenType.Identifier &&
                token.Value.Equals(keyword, StringComparison.OrdinalIgnoreCase))
            {
                Advance();
                return true;
            }
            return false;
        }

        private Token Expect(TokenType type, string expected)
        {
            if (Check(type))
                return Advance();

            throw new SqlParseException($"Expected {expected}", CurrentPosition);
        }

        private Token Advance()
        {
            if (!IsAtEnd())
                current++;
            return Previous();
        }

        private bool IsAtEnd() => Peek().Type == TokenType.Eof;

        private Token Peek() => tokens[current];

        private Token Previous() => tokens[current - 1];

        private int CurrentPosition => IsAtEnd() ? -1 : Peek().Position;

        #endregion
    }

    /// <summary>
    /// SQL解析エラー。
    /// </summary>
    public class SqlParseException : Exception
    {
        public int Position { get; }

        public SqlParseException(string message, int position)
            : base($"{message} at position {position}")
        {
            Position = position;
        }
    }
}
