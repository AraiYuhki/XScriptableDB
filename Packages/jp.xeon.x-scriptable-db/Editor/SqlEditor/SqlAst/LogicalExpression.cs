using System;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// 論理式（AND/OR）。
    /// </summary>
    public class LogicalExpression : SqlExpression
    {
        public SqlExpression Left { get; set; }
        public Xeon.XScriptableDB.Editor.LogicalOperator Operator { get; set; }
        public SqlExpression Right { get; set; }

        public override string ToString()
        {
            var op = Operator == Xeon.XScriptableDB.Editor.LogicalOperator.And ? "AND" : "OR";
            return $"({Left} {op} {Right})";
        }
    }
}
