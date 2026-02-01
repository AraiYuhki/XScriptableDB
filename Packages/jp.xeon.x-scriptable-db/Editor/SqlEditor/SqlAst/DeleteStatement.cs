namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// DELETEï∂ÅB
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
