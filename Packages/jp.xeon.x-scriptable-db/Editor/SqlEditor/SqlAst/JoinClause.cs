using System;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// JOINãÂÅB
    /// </summary>
    public class JoinClause
    {
        public Xeon.XScriptableDB.Editor.JoinType JoinType { get; set; }
        public string TableName { get; set; }
        public string Alias { get; set; }
        public SqlExpression OnCondition { get; set; }

        public override string ToString()
        {
            var joinStr = JoinType switch
            {
                Xeon.XScriptableDB.Editor.JoinType.Inner => "INNER JOIN",
                Xeon.XScriptableDB.Editor.JoinType.Left => "LEFT JOIN",
                Xeon.XScriptableDB.Editor.JoinType.Right => "RIGHT JOIN",
                Xeon.XScriptableDB.Editor.JoinType.Cross => "CROSS JOIN",
                _ => "JOIN"
            };
            var alias = string.IsNullOrEmpty(Alias) ? "" : $" {Alias}";
            var on = JoinType == Xeon.XScriptableDB.Editor.JoinType.Cross ? "" : $" ON {OnCondition}";
            return $"{joinStr} {TableName}{alias}{on}";
        }
    }
}
