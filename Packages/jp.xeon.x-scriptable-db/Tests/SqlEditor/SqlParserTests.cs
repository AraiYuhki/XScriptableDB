using NUnit.Framework;
using System;
using System.Linq;
using Xeon.XScriptableDB.Editor;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// SqlParser のテスト。
    /// </summary>
    public class SqlParserTests
    {
        private SqlParser parser;

        [SetUp]
        public void SetUp()
        {
            parser = new SqlParser();
        }

        #region Lexer Tests

        [Test]
        public void Lexer_SimpleSelect_TokenizesCorrectly()
        {
            var lexer = new SqlLexer("SELECT * FROM table1");
            var tokens = lexer.Tokenize();

            Assert.That(tokens.Count, Is.EqualTo(5)); // SELECT, *, FROM, table1, EOF
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Select));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Star));
            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.From));
            Assert.That(tokens[3].Type, Is.EqualTo(TokenType.Identifier));
            Assert.That(tokens[3].Value, Is.EqualTo("table1"));
            Assert.That(tokens[4].Type, Is.EqualTo(TokenType.Eof));
        }

        [Test]
        public void Lexer_StringLiteral_TokenizesCorrectly()
        {
            var lexer = new SqlLexer("'hello world'");
            var tokens = lexer.Tokenize();

            Assert.That(tokens.Count, Is.EqualTo(2)); // string, EOF
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.StringLiteral));
            Assert.That(tokens[0].Value, Is.EqualTo("hello world"));
        }

        [Test]
        public void Lexer_EscapedQuote_TokenizesCorrectly()
        {
            var lexer = new SqlLexer("'it''s a test'");
            var tokens = lexer.Tokenize();

            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.StringLiteral));
            Assert.That(tokens[0].Value, Is.EqualTo("it's a test"));
        }

        [Test]
        public void Lexer_NumberLiteral_TokenizesCorrectly()
        {
            var lexer = new SqlLexer("123 45.67 -89");
            var tokens = lexer.Tokenize();

            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.NumberLiteral));
            Assert.That(tokens[0].Value, Is.EqualTo("123"));

            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.NumberLiteral));
            Assert.That(tokens[1].Value, Is.EqualTo("45.67"));

            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.NumberLiteral));
            Assert.That(tokens[2].Value, Is.EqualTo("-89"));
        }

        [Test]
        public void Lexer_ComparisonOperators_TokenizesCorrectly()
        {
            var lexer = new SqlLexer("= != <> < <= > >=");
            var tokens = lexer.Tokenize();

            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Equal));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.NotEqual));
            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.NotEqual));
            Assert.That(tokens[3].Type, Is.EqualTo(TokenType.LessThan));
            Assert.That(tokens[4].Type, Is.EqualTo(TokenType.LessOrEqual));
            Assert.That(tokens[5].Type, Is.EqualTo(TokenType.GreaterThan));
            Assert.That(tokens[6].Type, Is.EqualTo(TokenType.GreaterOrEqual));
        }

        [Test]
        public void Lexer_Keywords_CaseInsensitive()
        {
            var lexer = new SqlLexer("select SELECT Select");
            var tokens = lexer.Tokenize();

            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Select));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Select));
            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.Select));
        }

        #endregion

        #region SELECT Statement Tests

        [Test]
        public void Parse_SimpleSelect_ReturnsSelectStatement()
        {
            var stmt = parser.Parse("SELECT * FROM Users") as SelectStatement;

            Assert.That(stmt, Is.Not.Null);
            Assert.That(stmt.StatementType, Is.EqualTo(SqlStatementType.Select));
            Assert.That(stmt.TableName, Is.EqualTo("Users"));
            Assert.That(stmt.Columns.Count, Is.EqualTo(1));
            Assert.That(stmt.Columns[0].IsWildcard, Is.True);
        }

        [Test]
        public void Parse_SelectWithColumns_ParsesColumns()
        {
            var stmt = parser.Parse("SELECT Id, Name, Value FROM Items") as SelectStatement;

            Assert.That(stmt.Columns.Count, Is.EqualTo(3));
            Assert.That((stmt.Columns[0].Expression as ColumnExpression)?.ColumnName, Is.EqualTo("Id"));
            Assert.That((stmt.Columns[1].Expression as ColumnExpression)?.ColumnName, Is.EqualTo("Name"));
            Assert.That((stmt.Columns[2].Expression as ColumnExpression)?.ColumnName, Is.EqualTo("Value"));
        }

        [Test]
        public void Parse_SelectWithAlias_ParsesAlias()
        {
            var stmt = parser.Parse("SELECT Id AS ItemId FROM Items") as SelectStatement;

            Assert.That(stmt.Columns[0].Alias, Is.EqualTo("ItemId"));
        }

        [Test]
        public void Parse_SelectWithWhere_ParsesWhereClause()
        {
            var stmt = parser.Parse("SELECT * FROM Users WHERE Id = 1") as SelectStatement;

            Assert.That(stmt.WhereClause, Is.Not.Null);
            var comparison = stmt.WhereClause as ComparisonExpression;
            Assert.That(comparison, Is.Not.Null);
            Assert.That(comparison.Operator, Is.EqualTo(ComparisonOperator.Equal));
        }

        [Test]
        public void Parse_SelectWithOrderBy_ParsesOrderBy()
        {
            var stmt = parser.Parse("SELECT * FROM Users ORDER BY Name ASC, Id DESC") as SelectStatement;

            Assert.That(stmt.OrderBy.Count, Is.EqualTo(2));
            Assert.That((stmt.OrderBy[0].Expression as ColumnExpression)?.ColumnName, Is.EqualTo("Name"));
            Assert.That(stmt.OrderBy[0].Order, Is.EqualTo(SortOrder.Ascending));
            Assert.That((stmt.OrderBy[1].Expression as ColumnExpression)?.ColumnName, Is.EqualTo("Id"));
            Assert.That(stmt.OrderBy[1].Order, Is.EqualTo(SortOrder.Descending));
        }

        [Test]
        public void Parse_SelectWithLimit_ParsesLimit()
        {
            var stmt = parser.Parse("SELECT * FROM Users LIMIT 10") as SelectStatement;

            Assert.That(stmt.Limit, Is.EqualTo(10));
        }

        [Test]
        public void Parse_SelectWithOffset_ParsesOffset()
        {
            var stmt = parser.Parse("SELECT * FROM Users LIMIT 10 OFFSET 20") as SelectStatement;

            Assert.That(stmt.Limit, Is.EqualTo(10));
            Assert.That(stmt.Offset, Is.EqualTo(20));
        }

        #endregion

        #region WHERE Clause Tests

        [Test]
        public void Parse_WhereWithAnd_ParsesLogicalExpression()
        {
            var stmt = parser.Parse("SELECT * FROM Users WHERE Id > 5 AND Name = 'Test'") as SelectStatement;

            var logical = stmt.WhereClause as LogicalExpression;
            Assert.That(logical, Is.Not.Null);
            Assert.That(logical.Operator, Is.EqualTo(LogicalOperator.And));
        }

        [Test]
        public void Parse_WhereWithOr_ParsesLogicalExpression()
        {
            var stmt = parser.Parse("SELECT * FROM Users WHERE Id = 1 OR Id = 2") as SelectStatement;

            var logical = stmt.WhereClause as LogicalExpression;
            Assert.That(logical, Is.Not.Null);
            Assert.That(logical.Operator, Is.EqualTo(LogicalOperator.Or));
        }

        [Test]
        public void Parse_WhereWithIsNull_ParsesNullCheck()
        {
            var stmt = parser.Parse("SELECT * FROM Users WHERE Email IS NULL") as SelectStatement;

            var comparison = stmt.WhereClause as ComparisonExpression;
            Assert.That(comparison, Is.Not.Null);
            Assert.That(comparison.Operator, Is.EqualTo(ComparisonOperator.IsNull));
        }

        [Test]
        public void Parse_WhereWithIsNotNull_ParsesNullCheck()
        {
            var stmt = parser.Parse("SELECT * FROM Users WHERE Email IS NOT NULL") as SelectStatement;

            var comparison = stmt.WhereClause as ComparisonExpression;
            Assert.That(comparison, Is.Not.Null);
            Assert.That(comparison.Operator, Is.EqualTo(ComparisonOperator.IsNotNull));
        }

        [Test]
        public void Parse_WhereWithIn_ParsesInExpression()
        {
            var stmt = parser.Parse("SELECT * FROM Users WHERE Id IN (1, 2, 3)") as SelectStatement;

            var comparison = stmt.WhereClause as ComparisonExpression;
            Assert.That(comparison, Is.Not.Null);
            Assert.That(comparison.Operator, Is.EqualTo(ComparisonOperator.In));

            var inList = comparison.Right as InListExpression;
            Assert.That(inList, Is.Not.Null);
            Assert.That(inList.Values.Count, Is.EqualTo(3));
        }

        [Test]
        public void Parse_WhereWithLike_ParsesLikeExpression()
        {
            var stmt = parser.Parse("SELECT * FROM Users WHERE Name LIKE '%test%'") as SelectStatement;

            var comparison = stmt.WhereClause as ComparisonExpression;
            Assert.That(comparison, Is.Not.Null);
            Assert.That(comparison.Operator, Is.EqualTo(ComparisonOperator.Like));
        }

        [Test]
        public void Parse_WhereWithParentheses_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT * FROM Users WHERE (Id = 1 OR Id = 2) AND Active = 1") as SelectStatement;

            var logical = stmt.WhereClause as LogicalExpression;
            Assert.That(logical, Is.Not.Null);
            Assert.That(logical.Operator, Is.EqualTo(LogicalOperator.And));
            Assert.That(logical.Left, Is.TypeOf<LogicalExpression>());
        }

        #endregion

        #region UPDATE Statement Tests

        [Test]
        public void Parse_SimpleUpdate_ReturnsUpdateStatement()
        {
            var stmt = parser.Parse("UPDATE Users SET Name = 'NewName'") as UpdateStatement;

            Assert.That(stmt, Is.Not.Null);
            Assert.That(stmt.StatementType, Is.EqualTo(SqlStatementType.Update));
            Assert.That(stmt.TableName, Is.EqualTo("Users"));
            Assert.That(stmt.SetItems.Count, Is.EqualTo(1));
            Assert.That(stmt.SetItems[0].ColumnName, Is.EqualTo("Name"));
        }

        [Test]
        public void Parse_UpdateWithMultipleColumns_ParsesAllSetItems()
        {
            var stmt = parser.Parse("UPDATE Users SET Name = 'Test', Value = 100") as UpdateStatement;

            Assert.That(stmt.SetItems.Count, Is.EqualTo(2));
            Assert.That(stmt.SetItems[0].ColumnName, Is.EqualTo("Name"));
            Assert.That(stmt.SetItems[1].ColumnName, Is.EqualTo("Value"));
        }

        [Test]
        public void Parse_UpdateWithWhere_ParsesWhereClause()
        {
            var stmt = parser.Parse("UPDATE Users SET Name = 'Test' WHERE Id = 1") as UpdateStatement;

            Assert.That(stmt.WhereClause, Is.Not.Null);
        }

        #endregion

        #region DELETE Statement Tests

        [Test]
        public void Parse_SimpleDelete_ReturnsDeleteStatement()
        {
            var stmt = parser.Parse("DELETE FROM Users") as DeleteStatement;

            Assert.That(stmt, Is.Not.Null);
            Assert.That(stmt.StatementType, Is.EqualTo(SqlStatementType.Delete));
            Assert.That(stmt.TableName, Is.EqualTo("Users"));
        }

        [Test]
        public void Parse_DeleteWithWhere_ParsesWhereClause()
        {
            var stmt = parser.Parse("DELETE FROM Users WHERE Id = 1") as DeleteStatement;

            Assert.That(stmt.WhereClause, Is.Not.Null);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Parse_InvalidSql_ThrowsSqlParseException()
        {
            Assert.Throws<SqlParseException>(() => parser.Parse("INVALID SQL"));
        }

        [Test]
        public void Parse_MissingFrom_ThrowsSqlParseException()
        {
            Assert.Throws<SqlParseException>(() => parser.Parse("SELECT *"));
        }

        [Test]
        public void Parse_MissingTableName_ThrowsSqlParseException()
        {
            Assert.Throws<SqlParseException>(() => parser.Parse("SELECT * FROM"));
        }

        [Test]
        public void Parse_EmptyString_ThrowsSqlParseException()
        {
            Assert.Throws<SqlParseException>(() => parser.Parse(""));
        }

        #endregion

        #region Literal Expression Tests

        [Test]
        public void Parse_StringLiteral_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT * FROM Users WHERE Name = 'Test Value'") as SelectStatement;

            var comparison = stmt.WhereClause as ComparisonExpression;
            var literal = comparison?.Right as LiteralExpression;
            Assert.That(literal, Is.Not.Null);
            Assert.That(literal.Value, Is.EqualTo("Test Value"));
        }

        [Test]
        public void Parse_IntegerLiteral_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT * FROM Users WHERE Id = 42") as SelectStatement;

            var comparison = stmt.WhereClause as ComparisonExpression;
            var literal = comparison?.Right as LiteralExpression;
            Assert.That(literal, Is.Not.Null);
            Assert.That(literal.Value, Is.EqualTo(42L));
        }

        [Test]
        public void Parse_FloatLiteral_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT * FROM Items WHERE Price = 19.99") as SelectStatement;

            var comparison = stmt.WhereClause as ComparisonExpression;
            var literal = comparison?.Right as LiteralExpression;
            Assert.That(literal, Is.Not.Null);
            Assert.That(literal.Value, Is.EqualTo(19.99));
        }

        [Test]
        public void Parse_NullLiteral_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT * FROM Users WHERE Email = NULL") as SelectStatement;

            var comparison = stmt.WhereClause as ComparisonExpression;
            var literal = comparison?.Right as LiteralExpression;
            Assert.That(literal, Is.Not.Null);
            Assert.That(literal.Value, Is.Null);
        }

        #endregion
    }
}
