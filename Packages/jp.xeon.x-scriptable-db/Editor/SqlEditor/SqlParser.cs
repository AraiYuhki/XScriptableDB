using System;
using System.Collections.Generic;
using System.Globalization;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SQL parser.
    /// </summary>
    public class SqlParser
    {
        private List<Token> tokens;
        private int current;

        /// <summary>
        /// Parses an SQL statement.
        /// </summary>
        /// <param name="sql">SQL string</param>
        /// <returns>Parsed SQL statement</returns>
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

            // DISTINCT
            if (Match(TokenType.Distinct))
                stmt.IsDistinct = true;

            stmt.Columns = ParseSelectColumns();

            Expect(TokenType.From, "FROM");
            stmt.FromTable = ParseTableReference();
            stmt.TableName = stmt.FromTable.TableName;

            // JOIN clause
            while (IsJoinKeyword())
                stmt.Joins.Add(ParseJoinClause());

            if (Match(TokenType.Where))
                stmt.WhereClause = ParseExpression();

            // GROUP BY
            if (Match(TokenType.Group))
            {
                Expect(TokenType.By, "BY");
                stmt.GroupBy = ParseGroupBy();

                // HAVING
                if (Match(TokenType.Having))
                    stmt.HavingClause = ParseExpression();
            }

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

        private bool IsJoinKeyword()
        {
            return Check(TokenType.Join) || Check(TokenType.Inner) ||
                   Check(TokenType.Left) || Check(TokenType.Right) ||
                   Check(TokenType.Cross);
        }

        private JoinClause ParseJoinClause()
        {
            var join = new JoinClause();

            // Determine JOIN type
            if (Match(TokenType.Inner))
            {
                join.JoinType = JoinType.Inner;
                Expect(TokenType.Join, "JOIN");
            }
            else if (Match(TokenType.Left))
            {
                join.JoinType = JoinType.Left;
                Match(TokenType.Outer); // OUTER is optional
                Expect(TokenType.Join, "JOIN");
            }
            else if (Match(TokenType.Right))
            {
                join.JoinType = JoinType.Right;
                Match(TokenType.Outer); // OUTER is optional
                Expect(TokenType.Join, "JOIN");
            }
            else if (Match(TokenType.Cross))
            {
                join.JoinType = JoinType.Cross;
                Expect(TokenType.Join, "JOIN");
            }
            else if (Match(TokenType.Join))
            {
                join.JoinType = JoinType.Inner; // Bare JOIN is treated as INNER JOIN
            }

            // Table name
            var tableRef = ParseTableReference();
            join.TableName = tableRef.TableName;
            join.Alias = tableRef.Alias;

            // ON condition (except CROSS JOIN)
            if (join.JoinType != JoinType.Cross)
            {
                Expect(TokenType.On, "ON");
                join.OnCondition = ParseExpression();
            }

            return join;
        }

        private TableReference ParseTableReference()
        {
            var tableRef = new TableReference();
            tableRef.TableName = ParseTableName();

            // Alias (AS is optional)
            if (MatchKeyword("AS"))
            {
                var aliasToken = Expect(TokenType.Identifier, "alias");
                tableRef.Alias = aliasToken.Value;
            }
            else if (Check(TokenType.Identifier) && !IsReservedKeyword())
            {
                tableRef.Alias = Advance().Value;
            }

            return tableRef;
        }

        private bool IsReservedKeyword()
        {
            if (IsAtEnd())
                return false;

            var token = Peek();
            return token.Type != TokenType.Identifier;
        }

        private List<GroupByItem> ParseGroupBy()
        {
            var items = new List<GroupByItem>();

            do
            {
                items.Add(new GroupByItem { Expression = ParseColumnExpression() });
            } while (Match(TokenType.Comma));

            return items;
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
                        Expression = ParseSelectExpression()
                    };

                    // Check for AS alias
                    if (MatchKeyword("AS"))
                    {
                        var aliasToken = Expect(TokenType.Identifier, "alias");
                        column.Alias = aliasToken.Value;
                    }
                    else if (Check(TokenType.Identifier) && !IsReservedKeyword())
                    {
                        // Alias without AS
                        column.Alias = Advance().Value;
                    }

                    columns.Add(column);
                }
            } while (Match(TokenType.Comma));

            return columns;
        }

        private SqlExpression ParseSelectExpression()
        {
            return ParseAdditiveExpression();
        }

        private SqlExpression ParseAdditiveExpression()
        {
            var left = ParseMultiplicativeExpression();

            while (Check(TokenType.Plus) || Check(TokenType.Minus))
            {
                ArithmeticOperator op;
                if (Match(TokenType.Plus))
                    op = ArithmeticOperator.Add;
                else if (Match(TokenType.Minus))
                    op = ArithmeticOperator.Subtract;
                else
                    break;

                var right = ParseMultiplicativeExpression();
                left = new ArithmeticExpression { Left = left, Operator = op, Right = right };
            }

            return left;
        }

        private SqlExpression ParseMultiplicativeExpression()
        {
            var left = ParseUnaryExpression();

            while (Check(TokenType.Star) || Check(TokenType.Slash) || Check(TokenType.Percent))
            {
                ArithmeticOperator op;
                if (Match(TokenType.Star))
                    op = ArithmeticOperator.Multiply;
                else if (Match(TokenType.Slash))
                    op = ArithmeticOperator.Divide;
                else if (Match(TokenType.Percent))
                    op = ArithmeticOperator.Modulo;
                else
                    break;

                var right = ParseUnaryExpression();
                left = new ArithmeticExpression { Left = left, Operator = op, Right = right };
            }

            return left;
        }

        private SqlExpression ParseUnaryExpression()
        {
            // Unary minus
            if (Match(TokenType.Minus))
            {
                var expr = ParseUnaryExpression();
                return new ArithmeticExpression
                {
                    Left = new LiteralExpression(0),
                    Operator = ArithmeticOperator.Subtract,
                    Right = expr
                };
            }

            return ParsePrimaryExpression();
        }

        private SqlExpression ParsePrimaryExpression()
        {
            // Parentheses
            if (Match(TokenType.LeftParen))
            {
                // Determine if subquery or grouping
                if (Check(TokenType.Select))
                {
                    var subquery = ParseSelect();
                    Expect(TokenType.RightParen, ")");
                    return new SubqueryExpression { Subquery = subquery };
                }

                var expr = ParseSelectExpression();
                Expect(TokenType.RightParen, ")");
                return expr;
            }

            // CASE expression
            if (Check(TokenType.Case))
                return ParseCaseExpression();

            // Aggregate function
            if (IsAggregateFunction())
                return ParseAggregateFunction();

            // String function
            if (IsStringFunction())
                return ParseStringFunction();

            // Literal
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

            // Column reference
            return ParseColumnExpression();
        }

        private bool IsAggregateFunction()
        {
            if (!(Check(TokenType.Count) || Check(TokenType.Sum) ||
                  Check(TokenType.Avg) || Check(TokenType.Min) || Check(TokenType.Max)))
                return false;

            // Check if the next token is ( (true only for function calls)
            return current + 1 < tokens.Count && tokens[current + 1].Type == TokenType.LeftParen;
        }

        private SqlExpression ParseAggregateFunction()
        {
            AggregateFunction func;

            if (Match(TokenType.Count))
                func = AggregateFunction.Count;
            else if (Match(TokenType.Sum))
                func = AggregateFunction.Sum;
            else if (Match(TokenType.Avg))
                func = AggregateFunction.Avg;
            else if (Match(TokenType.Min))
                func = AggregateFunction.Min;
            else if (Match(TokenType.Max))
                func = AggregateFunction.Max;
            else
                throw new SqlParseException("Expected aggregate function", CurrentPosition);

            Expect(TokenType.LeftParen, "(");

            var aggExpr = new AggregateExpression { Function = func };

            // DISTINCT
            if (Match(TokenType.Distinct))
                aggExpr.IsDistinct = true;

            // Special case for COUNT(*)
            if (func == AggregateFunction.Count && Match(TokenType.Star))
            {
                aggExpr.Argument = null; // COUNT(*) has no argument
            }
            else
            {
                aggExpr.Argument = ParseSelectExpression();
            }

            Expect(TokenType.RightParen, ")");
            return aggExpr;
        }

        private bool IsStringFunction()
        {
            if (!(Check(TokenType.Upper) || Check(TokenType.Lower) ||
                  Check(TokenType.Concat) || Check(TokenType.Substring) ||
                  Check(TokenType.Trim) || Check(TokenType.Length)))
                return false;

            // Check if the next token is ( (true only for function calls)
            return current + 1 < tokens.Count && tokens[current + 1].Type == TokenType.LeftParen;
        }

        private SqlExpression ParseStringFunction()
        {
            string funcName;

            if (Match(TokenType.Upper))
                funcName = "UPPER";
            else if (Match(TokenType.Lower))
                funcName = "LOWER";
            else if (Match(TokenType.Concat))
                funcName = "CONCAT";
            else if (Match(TokenType.Substring))
                funcName = "SUBSTRING";
            else if (Match(TokenType.Trim))
                funcName = "TRIM";
            else if (Match(TokenType.Length))
                funcName = "LENGTH";
            else
                throw new SqlParseException("Expected string function", CurrentPosition);

            Expect(TokenType.LeftParen, "(");

            var args = new List<SqlExpression>();
            do
            {
                args.Add(ParseSelectExpression());
            } while (Match(TokenType.Comma));

            Expect(TokenType.RightParen, ")");

            return new FunctionCallExpression { FunctionName = funcName, Arguments = args };
        }

        private SqlExpression ParseCaseExpression()
        {
            Expect(TokenType.Case, "CASE");

            var caseExpr = new CaseExpression();

            while (Match(TokenType.When))
            {
                var condition = ParseExpression();
                Expect(TokenType.Then, "THEN");
                var result = ParseSelectExpression();
                caseExpr.WhenClauses.Add(new WhenClause { Condition = condition, Result = result });
            }

            if (Match(TokenType.Else))
                caseExpr.ElseExpression = ParseSelectExpression();

            Expect(TokenType.End, "END");
            return caseExpr;
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

            // Parse expression including arithmetic, functions, and aggregates
            var left = ParseSelectExpression();

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

            // Parse expression including arithmetic, functions, and aggregates
            var right = ParseSelectExpression();

            return new ComparisonExpression
            {
                Left = left,
                Operator = op.Value,
                Right = right
            };
        }

        private SqlExpression ParseInList()
        {
            Expect(TokenType.LeftParen, "(");

            // Check for subquery
            if (Check(TokenType.Select))
            {
                var subquery = ParseSelect();
                Expect(TokenType.RightParen, ")");
                return new SubqueryExpression { Subquery = subquery };
            }

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
            var token = ExpectIdentifierOrKeyword("column name");
            var columnName = token.Value;
            string tableAlias = null;

            // Check for table.column notation
            if (Match(TokenType.Dot))
            {
                tableAlias = columnName;
                var columnToken = ExpectIdentifierOrKeyword("column name");
                columnName = columnToken.Value;
            }

            return new ColumnExpression(columnName, tableAlias);
        }

        private Token ExpectIdentifierOrKeyword(string expected)
        {
            var token = Peek();

            // Identifiers are always allowed
            if (token.Type == TokenType.Identifier)
            {
                Advance();
                return token;
            }

            // Keywords used as function names are also allowed as column names
            if (IsKeywordUsableAsColumnName(token.Type))
            {
                Advance();
                return token;
            }

            throw new SqlParseException($"Expected {expected} at position {token.Position}", token.Position);
        }

        private bool IsKeywordUsableAsColumnName(TokenType type)
        {
            return type == TokenType.Count || type == TokenType.Sum ||
                   type == TokenType.Avg || type == TokenType.Min || type == TokenType.Max ||
                   type == TokenType.Upper || type == TokenType.Lower ||
                   type == TokenType.Concat || type == TokenType.Substring ||
                   type == TokenType.Trim || type == TokenType.Length;
        }
        
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
    }
}
