using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SQL文の種類。
    /// </summary>
    public enum SqlStatementType
    {
        Select,
        Update,
        Delete
    }

    /// <summary>
    /// JOIN の種類。
    /// </summary>
    public enum JoinType
    {
        Inner,
        Left,
        Right,
        Cross
    }

    /// <summary>
    /// 集計関数の種類。
    /// </summary>
    public enum AggregateFunction
    {
        Count,
        Sum,
        Avg,
        Min,
        Max
    }

    /// <summary>
    /// 算術演算子。
    /// </summary>
    public enum ArithmeticOperator
    {
        Add,        // +
        Subtract,   // -
        Multiply,   // *
        Divide,     // /
        Modulo      // %
    }

    /// <summary>
    /// 比較演算子。
    /// </summary>
    public enum ComparisonOperator
    {
        Equal,          // =
        NotEqual,       // != or <>
        LessThan,       // <
        LessOrEqual,    // <=
        GreaterThan,    // >
        GreaterOrEqual, // >=
        Like,           // LIKE
        In,             // IN
        IsNull,         // IS NULL
        IsNotNull       // IS NOT NULL
    }

    /// <summary>
    /// 論理演算子。
    /// </summary>
    public enum LogicalOperator
    {
        And,
        Or
    }

    /// <summary>
    /// ソート順。
    /// </summary>
    public enum SortOrder
    {
        Ascending,
        Descending
    }

    /// <summary>
    /// SQL式の基底クラス。
    /// </summary>
    public abstract class SqlExpression
    {
    }

    /// <summary>
    /// リテラル値。
    /// </summary>
    public class LiteralExpression : SqlExpression
    {
        public object Value { get; set; }
        public Type ValueType { get; set; }

        public LiteralExpression(object value)
        {
            Value = value;
            ValueType = value?.GetType();
        }

        public override string ToString() => Value?.ToString() ?? "NULL";
    }

    /// <summary>
    /// カラム参照。
    /// </summary>
    public class ColumnExpression : SqlExpression
    {
        public string ColumnName { get; set; }
        public string TableAlias { get; set; }

        public ColumnExpression(string columnName, string tableAlias = null)
        {
            ColumnName = columnName;
            TableAlias = tableAlias;
        }

        public override string ToString() =>
            string.IsNullOrEmpty(TableAlias) ? ColumnName : $"{TableAlias}.{ColumnName}";
    }

    /// <summary>
    /// 比較式。
    /// </summary>
    public class ComparisonExpression : SqlExpression
    {
        public SqlExpression Left { get; set; }
        public ComparisonOperator Operator { get; set; }
        public SqlExpression Right { get; set; }

        public override string ToString()
        {
            var op = Operator switch
            {
                ComparisonOperator.Equal => "=",
                ComparisonOperator.NotEqual => "!=",
                ComparisonOperator.LessThan => "<",
                ComparisonOperator.LessOrEqual => "<=",
                ComparisonOperator.GreaterThan => ">",
                ComparisonOperator.GreaterOrEqual => ">=",
                ComparisonOperator.Like => "LIKE",
                ComparisonOperator.In => "IN",
                ComparisonOperator.IsNull => "IS NULL",
                ComparisonOperator.IsNotNull => "IS NOT NULL",
                _ => "?"
            };

            if (Operator == ComparisonOperator.IsNull || Operator == ComparisonOperator.IsNotNull)
                return $"{Left} {op}";

            return $"{Left} {op} {Right}";
        }
    }

    /// <summary>
    /// 論理式（AND/OR）。
    /// </summary>
    public class LogicalExpression : SqlExpression
    {
        public SqlExpression Left { get; set; }
        public LogicalOperator Operator { get; set; }
        public SqlExpression Right { get; set; }

        public override string ToString()
        {
            var op = Operator == LogicalOperator.And ? "AND" : "OR";
            return $"({Left} {op} {Right})";
        }
    }

    /// <summary>
    /// IN式の値リスト。
    /// </summary>
    public class InListExpression : SqlExpression
    {
        public List<SqlExpression> Values { get; set; } = new();

        public override string ToString()
        {
            return $"({string.Join(", ", Values)})";
        }
    }

    /// <summary>
    /// 集計関数式。
    /// </summary>
    public class AggregateExpression : SqlExpression
    {
        public AggregateFunction Function { get; set; }
        public SqlExpression Argument { get; set; }
        public bool IsDistinct { get; set; }

        public override string ToString()
        {
            var distinct = IsDistinct ? "DISTINCT " : "";
            var arg = Argument?.ToString() ?? "*";
            return $"{Function.ToString().ToUpper()}({distinct}{arg})";
        }
    }

    /// <summary>
    /// 算術式。
    /// </summary>
    public class ArithmeticExpression : SqlExpression
    {
        public SqlExpression Left { get; set; }
        public ArithmeticOperator Operator { get; set; }
        public SqlExpression Right { get; set; }

        public override string ToString()
        {
            var op = Operator switch
            {
                ArithmeticOperator.Add => "+",
                ArithmeticOperator.Subtract => "-",
                ArithmeticOperator.Multiply => "*",
                ArithmeticOperator.Divide => "/",
                ArithmeticOperator.Modulo => "%",
                _ => "?"
            };
            return $"({Left} {op} {Right})";
        }
    }

    /// <summary>
    /// CASE式。
    /// </summary>
    public class CaseExpression : SqlExpression
    {
        public List<WhenClause> WhenClauses { get; set; } = new();
        public SqlExpression ElseExpression { get; set; }

        public override string ToString()
        {
            var whens = string.Join(" ", WhenClauses);
            var elseStr = ElseExpression != null ? $" ELSE {ElseExpression}" : "";
            return $"CASE {whens}{elseStr} END";
        }
    }

    /// <summary>
    /// WHEN句。
    /// </summary>
    public class WhenClause
    {
        public SqlExpression Condition { get; set; }
        public SqlExpression Result { get; set; }

        public override string ToString() => $"WHEN {Condition} THEN {Result}";
    }

    /// <summary>
    /// 関数呼び出し式。
    /// </summary>
    public class FunctionCallExpression : SqlExpression
    {
        public string FunctionName { get; set; }
        public List<SqlExpression> Arguments { get; set; } = new();

        public override string ToString()
        {
            var args = string.Join(", ", Arguments);
            return $"{FunctionName}({args})";
        }
    }

    /// <summary>
    /// サブクエリ式。
    /// </summary>
    public class SubqueryExpression : SqlExpression
    {
        public SelectStatement Subquery { get; set; }

        public override string ToString() => $"({Subquery})";
    }

    /// <summary>
    /// JOIN句。
    /// </summary>
    public class JoinClause
    {
        public JoinType JoinType { get; set; }
        public string TableName { get; set; }
        public string Alias { get; set; }
        public SqlExpression OnCondition { get; set; }

        public override string ToString()
        {
            var joinStr = JoinType switch
            {
                JoinType.Inner => "INNER JOIN",
                JoinType.Left => "LEFT JOIN",
                JoinType.Right => "RIGHT JOIN",
                JoinType.Cross => "CROSS JOIN",
                _ => "JOIN"
            };
            var alias = string.IsNullOrEmpty(Alias) ? "" : $" {Alias}";
            var on = JoinType == JoinType.Cross ? "" : $" ON {OnCondition}";
            return $"{joinStr} {TableName}{alias}{on}";
        }
    }

    /// <summary>
    /// テーブル参照（エイリアス付き）。
    /// </summary>
    public class TableReference
    {
        public string TableName { get; set; }
        public string Alias { get; set; }

        public override string ToString() =>
            string.IsNullOrEmpty(Alias) ? TableName : $"{TableName} {Alias}";
    }

    /// <summary>
    /// GROUP BY句の項目。
    /// </summary>
    public class GroupByItem
    {
        public SqlExpression Expression { get; set; }

        public override string ToString() => Expression.ToString();
    }

    /// <summary>
    /// SELECT句の列指定。
    /// </summary>
    public class SelectColumn
    {
        public SqlExpression Expression { get; set; }
        public string Alias { get; set; }
        public bool IsWildcard { get; set; }

        public override string ToString()
        {
            if (IsWildcard)
                return "*";
            return string.IsNullOrEmpty(Alias) ? Expression.ToString() : $"{Expression} AS {Alias}";
        }
    }

    /// <summary>
    /// ORDER BY句の項目。
    /// </summary>
    public class OrderByItem
    {
        public SqlExpression Expression { get; set; }
        public SortOrder Order { get; set; } = SortOrder.Ascending;

        public override string ToString()
        {
            var orderStr = Order == SortOrder.Descending ? " DESC" : "";
            return $"{Expression}{orderStr}";
        }
    }

    /// <summary>
    /// SET句の項目（UPDATE用）。
    /// </summary>
    public class SetItem
    {
        public string ColumnName { get; set; }
        public SqlExpression Value { get; set; }

        public override string ToString() => $"{ColumnName} = {Value}";
    }

    /// <summary>
    /// SQL文の基底クラス。
    /// </summary>
    public abstract class SqlStatement
    {
        public SqlStatementType StatementType { get; protected set; }
        public string TableName { get; set; }
        public SqlExpression WhereClause { get; set; }
    }

    /// <summary>
    /// SELECT文。
    /// </summary>
    public class SelectStatement : SqlStatement
    {
        public bool IsDistinct { get; set; }
        public List<SelectColumn> Columns { get; set; } = new();
        public TableReference FromTable { get; set; }
        public List<JoinClause> Joins { get; set; } = new();
        public List<GroupByItem> GroupBy { get; set; } = new();
        public SqlExpression HavingClause { get; set; }
        public List<OrderByItem> OrderBy { get; set; } = new();
        public int? Limit { get; set; }
        public int? Offset { get; set; }

        public SelectStatement()
        {
            StatementType = SqlStatementType.Select;
        }

        public override string ToString()
        {
            var distinct = IsDistinct ? "DISTINCT " : "";
            var columns = Columns.Count > 0 ? string.Join(", ", Columns) : "*";
            var fromStr = FromTable?.ToString() ?? TableName;
            var sql = $"SELECT {distinct}{columns} FROM {fromStr}";

            foreach (var join in Joins)
                sql += $" {join}";

            if (WhereClause != null)
                sql += $" WHERE {WhereClause}";

            if (GroupBy.Count > 0)
                sql += $" GROUP BY {string.Join(", ", GroupBy)}";

            if (HavingClause != null)
                sql += $" HAVING {HavingClause}";

            if (OrderBy.Count > 0)
                sql += $" ORDER BY {string.Join(", ", OrderBy)}";

            if (Limit.HasValue)
                sql += $" LIMIT {Limit.Value}";

            if (Offset.HasValue)
                sql += $" OFFSET {Offset.Value}";

            return sql;
        }
    }

    /// <summary>
    /// UPDATE文。
    /// </summary>
    public class UpdateStatement : SqlStatement
    {
        public List<SetItem> SetItems { get; set; } = new();

        public UpdateStatement()
        {
            StatementType = SqlStatementType.Update;
        }

        public override string ToString()
        {
            var setClause = string.Join(", ", SetItems);
            var sql = $"UPDATE {TableName} SET {setClause}";

            if (WhereClause != null)
                sql += $" WHERE {WhereClause}";

            return sql;
        }
    }

    /// <summary>
    /// DELETE文。
    /// </summary>
    public class DeleteStatement : SqlStatement
    {
        public DeleteStatement()
        {
            StatementType = SqlStatementType.Delete;
        }

        public override string ToString()
        {
            var sql = $"DELETE FROM {TableName}";

            if (WhereClause != null)
                sql += $" WHERE {WhereClause}";

            return sql;
        }
    }
}
