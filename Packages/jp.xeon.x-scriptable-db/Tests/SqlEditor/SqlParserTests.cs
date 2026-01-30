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
            var lexer = new SqlLexer("123 45.67");
            var tokens = lexer.Tokenize();

            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.NumberLiteral));
            Assert.That(tokens[0].Value, Is.EqualTo("123"));

            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.NumberLiteral));
            Assert.That(tokens[1].Value, Is.EqualTo("45.67"));
        }

        [Test]
        public void Lexer_MinusOperator_TokenizesCorrectly()
        {
            var lexer = new SqlLexer("-89 1 - 2");
            var tokens = lexer.Tokenize();

            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Minus));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.NumberLiteral));
            Assert.That(tokens[1].Value, Is.EqualTo("89"));
            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.NumberLiteral));
            Assert.That(tokens[2].Value, Is.EqualTo("1"));
            Assert.That(tokens[3].Type, Is.EqualTo(TokenType.Minus));
            Assert.That(tokens[4].Type, Is.EqualTo(TokenType.NumberLiteral));
            Assert.That(tokens[4].Value, Is.EqualTo("2"));
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

        #region JOIN Tests

        [Test]
        public void Parse_InnerJoin_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT * FROM Items i INNER JOIN Categories c ON i.CategoryId = c.Id") as SelectStatement;

            Assert.That(stmt, Is.Not.Null);
            Assert.That(stmt.Joins.Count, Is.EqualTo(1));
            Assert.That(stmt.Joins[0].JoinType, Is.EqualTo(JoinType.Inner));
            Assert.That(stmt.Joins[0].TableName, Is.EqualTo("Categories"));
            Assert.That(stmt.Joins[0].Alias, Is.EqualTo("c"));
            Assert.That(stmt.Joins[0].OnCondition, Is.Not.Null);
        }

        [Test]
        public void Parse_LeftJoin_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT * FROM Items LEFT JOIN Categories ON Items.CategoryId = Categories.Id") as SelectStatement;

            Assert.That(stmt.Joins[0].JoinType, Is.EqualTo(JoinType.Left));
        }

        [Test]
        public void Parse_RightJoin_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT * FROM Items RIGHT JOIN Categories ON Items.CategoryId = Categories.Id") as SelectStatement;

            Assert.That(stmt.Joins[0].JoinType, Is.EqualTo(JoinType.Right));
        }

        [Test]
        public void Parse_CrossJoin_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT * FROM Items CROSS JOIN Categories") as SelectStatement;

            Assert.That(stmt.Joins[0].JoinType, Is.EqualTo(JoinType.Cross));
            Assert.That(stmt.Joins[0].OnCondition, Is.Null);
        }

        [Test]
        public void Parse_MultipleJoins_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT * FROM Items i INNER JOIN Categories c ON i.CategoryId = c.Id LEFT JOIN Suppliers s ON i.SupplierId = s.Id") as SelectStatement;

            Assert.That(stmt.Joins.Count, Is.EqualTo(2));
            Assert.That(stmt.Joins[0].JoinType, Is.EqualTo(JoinType.Inner));
            Assert.That(stmt.Joins[1].JoinType, Is.EqualTo(JoinType.Left));
        }

        #endregion

        #region Aggregate Function Tests

        [Test]
        public void Parse_CountStar_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT COUNT(*) FROM Items") as SelectStatement;

            Assert.That(stmt.Columns.Count, Is.EqualTo(1));
            var aggExpr = stmt.Columns[0].Expression as AggregateExpression;
            Assert.That(aggExpr, Is.Not.Null);
            Assert.That(aggExpr.Function, Is.EqualTo(AggregateFunction.Count));
            Assert.That(aggExpr.Argument, Is.Null);
        }

        [Test]
        public void Parse_CountColumn_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT COUNT(Id) FROM Items") as SelectStatement;

            var aggExpr = stmt.Columns[0].Expression as AggregateExpression;
            Assert.That(aggExpr.Function, Is.EqualTo(AggregateFunction.Count));
            Assert.That(aggExpr.Argument, Is.TypeOf<ColumnExpression>());
        }

        [Test]
        public void Parse_CountDistinct_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT COUNT(DISTINCT CategoryId) FROM Items") as SelectStatement;

            var aggExpr = stmt.Columns[0].Expression as AggregateExpression;
            Assert.That(aggExpr.Function, Is.EqualTo(AggregateFunction.Count));
            Assert.That(aggExpr.IsDistinct, Is.True);
        }

        [Test]
        public void Parse_SumFunction_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT SUM(Price) FROM Items") as SelectStatement;

            var aggExpr = stmt.Columns[0].Expression as AggregateExpression;
            Assert.That(aggExpr.Function, Is.EqualTo(AggregateFunction.Sum));
        }

        [Test]
        public void Parse_AvgFunction_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT AVG(Price) FROM Items") as SelectStatement;

            var aggExpr = stmt.Columns[0].Expression as AggregateExpression;
            Assert.That(aggExpr.Function, Is.EqualTo(AggregateFunction.Avg));
        }

        [Test]
        public void Parse_MinMaxFunctions_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT MIN(Price), MAX(Price) FROM Items") as SelectStatement;

            var minExpr = stmt.Columns[0].Expression as AggregateExpression;
            var maxExpr = stmt.Columns[1].Expression as AggregateExpression;
            Assert.That(minExpr.Function, Is.EqualTo(AggregateFunction.Min));
            Assert.That(maxExpr.Function, Is.EqualTo(AggregateFunction.Max));
        }

        [Test]
        public void Parse_MultipleAggregates_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT COUNT(*), SUM(Price), AVG(Price) FROM Items") as SelectStatement;

            Assert.That(stmt.Columns.Count, Is.EqualTo(3));
            Assert.That(stmt.Columns[0].Expression, Is.TypeOf<AggregateExpression>());
            Assert.That(stmt.Columns[1].Expression, Is.TypeOf<AggregateExpression>());
            Assert.That(stmt.Columns[2].Expression, Is.TypeOf<AggregateExpression>());
        }

        #endregion

        #region GROUP BY / HAVING Tests

        [Test]
        public void Parse_GroupBy_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT CategoryId, COUNT(*) FROM Items GROUP BY CategoryId") as SelectStatement;

            Assert.That(stmt.GroupBy.Count, Is.EqualTo(1));
            var groupExpr = stmt.GroupBy[0].Expression as ColumnExpression;
            Assert.That(groupExpr.ColumnName, Is.EqualTo("CategoryId"));
        }

        [Test]
        public void Parse_GroupByMultiple_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT CategoryId, Status, COUNT(*) FROM Items GROUP BY CategoryId, Status") as SelectStatement;

            Assert.That(stmt.GroupBy.Count, Is.EqualTo(2));
        }

        [Test]
        public void Parse_GroupByWithHaving_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT CategoryId, COUNT(*) FROM Items GROUP BY CategoryId HAVING COUNT(*) > 5") as SelectStatement;

            Assert.That(stmt.HavingClause, Is.Not.Null);
            var comparison = stmt.HavingClause as ComparisonExpression;
            Assert.That(comparison, Is.Not.Null);
            Assert.That(comparison.Left, Is.TypeOf<AggregateExpression>());
        }

        #endregion

        #region DISTINCT Tests

        [Test]
        public void Parse_SelectDistinct_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT DISTINCT CategoryId FROM Items") as SelectStatement;

            Assert.That(stmt.IsDistinct, Is.True);
        }

        #endregion

        #region CASE Expression Tests

        [Test]
        public void Parse_CaseExpression_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT CASE WHEN Price > 1000 THEN 'High' ELSE 'Low' END FROM Items") as SelectStatement;

            var caseExpr = stmt.Columns[0].Expression as CaseExpression;
            Assert.That(caseExpr, Is.Not.Null);
            Assert.That(caseExpr.WhenClauses.Count, Is.EqualTo(1));
            Assert.That(caseExpr.ElseExpression, Is.Not.Null);
        }

        [Test]
        public void Parse_CaseExpressionWithAlias_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT CASE WHEN Active = 1 THEN 'Yes' ELSE 'No' END AS Status FROM Items") as SelectStatement;

            Assert.That(stmt.Columns[0].Alias, Is.EqualTo("Status"));
        }

        [Test]
        public void Parse_CaseExpressionMultipleWhen_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT CASE WHEN Price < 100 THEN 'Low' WHEN Price < 1000 THEN 'Medium' ELSE 'High' END FROM Items") as SelectStatement;

            var caseExpr = stmt.Columns[0].Expression as CaseExpression;
            Assert.That(caseExpr.WhenClauses.Count, Is.EqualTo(2));
        }

        #endregion

        #region String Function Tests

        [Test]
        public void Parse_UpperFunction_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT UPPER(Name) FROM Items") as SelectStatement;

            var funcExpr = stmt.Columns[0].Expression as FunctionCallExpression;
            Assert.That(funcExpr, Is.Not.Null);
            Assert.That(funcExpr.FunctionName, Is.EqualTo("UPPER"));
            Assert.That(funcExpr.Arguments.Count, Is.EqualTo(1));
        }

        [Test]
        public void Parse_LowerFunction_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT LOWER(Name) FROM Items") as SelectStatement;

            var funcExpr = stmt.Columns[0].Expression as FunctionCallExpression;
            Assert.That(funcExpr.FunctionName, Is.EqualTo("LOWER"));
        }

        [Test]
        public void Parse_ConcatFunction_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT CONCAT(FirstName, ' ', LastName) FROM Users") as SelectStatement;

            var funcExpr = stmt.Columns[0].Expression as FunctionCallExpression;
            Assert.That(funcExpr.FunctionName, Is.EqualTo("CONCAT"));
            Assert.That(funcExpr.Arguments.Count, Is.EqualTo(3));
        }

        [Test]
        public void Parse_SubstringFunction_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT SUBSTRING(Name, 1, 5) FROM Items") as SelectStatement;

            var funcExpr = stmt.Columns[0].Expression as FunctionCallExpression;
            Assert.That(funcExpr.FunctionName, Is.EqualTo("SUBSTRING"));
            Assert.That(funcExpr.Arguments.Count, Is.EqualTo(3));
        }

        [Test]
        public void Parse_TrimFunction_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT TRIM(Name) FROM Items") as SelectStatement;

            var funcExpr = stmt.Columns[0].Expression as FunctionCallExpression;
            Assert.That(funcExpr.FunctionName, Is.EqualTo("TRIM"));
        }

        [Test]
        public void Parse_LengthFunction_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT LENGTH(Name) FROM Items") as SelectStatement;

            var funcExpr = stmt.Columns[0].Expression as FunctionCallExpression;
            Assert.That(funcExpr.FunctionName, Is.EqualTo("LENGTH"));
        }

        #endregion

        #region Arithmetic Expression Tests

        [Test]
        public void Parse_AddExpression_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT Price + Tax FROM Items") as SelectStatement;

            var arithExpr = stmt.Columns[0].Expression as ArithmeticExpression;
            Assert.That(arithExpr, Is.Not.Null);
            Assert.That(arithExpr.Operator, Is.EqualTo(ArithmeticOperator.Add));
        }

        [Test]
        public void Parse_SubtractExpression_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT Price - Discount FROM Items") as SelectStatement;

            var arithExpr = stmt.Columns[0].Expression as ArithmeticExpression;
            Assert.That(arithExpr.Operator, Is.EqualTo(ArithmeticOperator.Subtract));
        }

        [Test]
        public void Parse_MultiplyExpression_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT Price * Quantity FROM Items") as SelectStatement;

            var arithExpr = stmt.Columns[0].Expression as ArithmeticExpression;
            Assert.That(arithExpr.Operator, Is.EqualTo(ArithmeticOperator.Multiply));
        }

        [Test]
        public void Parse_DivideExpression_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT Total / Count FROM Items") as SelectStatement;

            var arithExpr = stmt.Columns[0].Expression as ArithmeticExpression;
            Assert.That(arithExpr.Operator, Is.EqualTo(ArithmeticOperator.Divide));
        }

        [Test]
        public void Parse_ComplexArithmeticExpression_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT Price * Quantity + Tax FROM Items") as SelectStatement;

            // Should parse as (Price * Quantity) + Tax due to operator precedence
            var arithExpr = stmt.Columns[0].Expression as ArithmeticExpression;
            Assert.That(arithExpr.Operator, Is.EqualTo(ArithmeticOperator.Add));
            Assert.That(arithExpr.Left, Is.TypeOf<ArithmeticExpression>());
        }

        #endregion

        #region Subquery Tests

        [Test]
        public void Parse_SubqueryInWhere_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT * FROM Items WHERE CategoryId IN (SELECT Id FROM Categories WHERE Active = 1)") as SelectStatement;

            var comparison = stmt.WhereClause as ComparisonExpression;
            Assert.That(comparison.Operator, Is.EqualTo(ComparisonOperator.In));
            // Note: IN with subquery is parsed as InListExpression containing a SubqueryExpression
        }

        #endregion

        #region Table Alias Tests

        [Test]
        public void Parse_TableWithAlias_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT i.Name FROM Items i") as SelectStatement;

            Assert.That(stmt.FromTable.TableName, Is.EqualTo("Items"));
            Assert.That(stmt.FromTable.Alias, Is.EqualTo("i"));
        }

        [Test]
        public void Parse_TableWithAsAlias_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT i.Name FROM Items AS i") as SelectStatement;

            Assert.That(stmt.FromTable.TableName, Is.EqualTo("Items"));
            Assert.That(stmt.FromTable.Alias, Is.EqualTo("i"));
        }

        [Test]
        public void Parse_ColumnWithTablePrefix_ParsesCorrectly()
        {
            var stmt = parser.Parse("SELECT Items.Name FROM Items") as SelectStatement;

            var colExpr = stmt.Columns[0].Expression as ColumnExpression;
            Assert.That(colExpr.TableAlias, Is.EqualTo("Items"));
            Assert.That(colExpr.ColumnName, Is.EqualTo("Name"));
        }

        #endregion
    }
}
