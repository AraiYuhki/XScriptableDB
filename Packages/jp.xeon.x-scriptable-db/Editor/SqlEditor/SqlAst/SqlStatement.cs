using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SQL•¶‚ÌŠî’êƒNƒ‰ƒXB
    /// </summary>
    public abstract class SqlStatement
    {
        public Xeon.XScriptableDB.Editor.SqlStatementType StatementType { get; protected set; }
        public string TableName { get; set; }
        public SqlExpression WhereClause { get; set; }
    }
}
