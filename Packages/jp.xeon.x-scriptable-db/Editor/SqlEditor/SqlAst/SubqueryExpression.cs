using System;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// サブクエリ式。
    /// </summary>
    public class SubqueryExpression : SqlExpression
    {
        public SelectStatement Subquery { get; set; }

        public override string ToString() => $"({Subquery})";
    }
}
