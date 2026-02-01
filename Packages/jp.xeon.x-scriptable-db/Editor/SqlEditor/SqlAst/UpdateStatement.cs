using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// UPDATEï∂ÅB
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
}
