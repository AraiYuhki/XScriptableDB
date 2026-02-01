using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SELECTï∂ÅB
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
}
