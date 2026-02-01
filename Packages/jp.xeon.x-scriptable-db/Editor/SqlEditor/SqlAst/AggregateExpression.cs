namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// èWåvä÷êîéÆÅB
    /// </summary>
    public class AggregateExpression : SqlExpression
    {
        public Xeon.XScriptableDB.Editor.AggregateFunction Function { get; set; }
        public SqlExpression Argument { get; set; }
        public bool IsDistinct { get; set; }

        public override string ToString()
        {
            var distinct = IsDistinct ? "DISTINCT " : "";
            var arg = Argument?.ToString() ?? "*";
            return $"{Function.ToString().ToUpper()}({distinct}{arg})";
        }
    }
}
