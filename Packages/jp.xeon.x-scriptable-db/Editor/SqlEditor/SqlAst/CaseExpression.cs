using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// CASEéÆÅB
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
}
