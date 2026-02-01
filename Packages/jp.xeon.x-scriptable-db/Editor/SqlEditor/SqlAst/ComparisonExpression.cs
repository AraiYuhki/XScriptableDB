namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// î‰äréÆÅB
    /// </summary>
    public class ComparisonExpression : SqlExpression
    {
        public SqlExpression Left { get; set; }
        public Xeon.XScriptableDB.Editor.ComparisonOperator Operator { get; set; }
        public SqlExpression Right { get; set; }

        public override string ToString()
        {
            var op = Operator switch
            {
                Xeon.XScriptableDB.Editor.ComparisonOperator.Equal => "=",
                Xeon.XScriptableDB.Editor.ComparisonOperator.NotEqual => "!=",
                Xeon.XScriptableDB.Editor.ComparisonOperator.LessThan => "<",
                Xeon.XScriptableDB.Editor.ComparisonOperator.LessOrEqual => "<=",
                Xeon.XScriptableDB.Editor.ComparisonOperator.GreaterThan => ">",
                Xeon.XScriptableDB.Editor.ComparisonOperator.GreaterOrEqual => ">=",
                Xeon.XScriptableDB.Editor.ComparisonOperator.Like => "LIKE",
                Xeon.XScriptableDB.Editor.ComparisonOperator.In => "IN",
                Xeon.XScriptableDB.Editor.ComparisonOperator.IsNull => "IS NULL",
                Xeon.XScriptableDB.Editor.ComparisonOperator.IsNotNull => "IS NOT NULL",
                _ => "?"
            };

            if (Operator == Xeon.XScriptableDB.Editor.ComparisonOperator.IsNull || Operator == Xeon.XScriptableDB.Editor.ComparisonOperator.IsNotNull)
                return $"{Left} {op}";

            return $"{Left} {op} {Right}";
        }
    }
}
