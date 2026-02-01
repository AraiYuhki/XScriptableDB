namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// GROUP BY‹å‚Ì€–ÚB
    /// </summary>
    public class GroupByItem
    {
        public SqlExpression Expression { get; set; }

        public override string ToString() => Expression.ToString();
    }
}
