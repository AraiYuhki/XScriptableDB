namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// ƒJƒ‰ƒ€QÆB
    /// </summary>
    public class ColumnExpression : SqlExpression
    {
        public string ColumnName { get; set; }
        public string TableAlias { get; set; }

        public ColumnExpression(string columnName, string tableAlias = null)
        {
            ColumnName = columnName;
            TableAlias = tableAlias;
        }

        public override string ToString() =>
            string.IsNullOrEmpty(TableAlias) ? ColumnName : $"{TableAlias}.{ColumnName}";
    }
}
