namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// WHENãÂÅB
    /// </summary>
    public class WhenClause
    {
        public SqlExpression Condition { get; set; }
        public SqlExpression Result { get; set; }

        public override string ToString() => $"WHEN {Condition} THEN {Result}";
    }
}
