namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Zp®B
    /// </summary>
    public class ArithmeticExpression : SqlExpression
    {
        public SqlExpression Left { get; set; }
        public Xeon.XScriptableDB.Editor.ArithmeticOperator Operator { get; set; }
        public SqlExpression Right { get; set; }

        public override string ToString()
        {
            var op = Operator switch
            {
                Xeon.XScriptableDB.Editor.ArithmeticOperator.Add => "+",
                Xeon.XScriptableDB.Editor.ArithmeticOperator.Subtract => "-",
                Xeon.XScriptableDB.Editor.ArithmeticOperator.Multiply => "*",
                Xeon.XScriptableDB.Editor.ArithmeticOperator.Divide => "/",
                Xeon.XScriptableDB.Editor.ArithmeticOperator.Modulo => "%",
                _ => "?"
            };
            return $"({Left} {op} {Right})";
        }
    }
}
