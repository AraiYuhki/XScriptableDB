namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// テーブル参照（エイリアス付き）。
    /// </summary>
    public class TableReference
    {
        public string TableName { get; set; }
        public string Alias { get; set; }

        public override string ToString() =>
            string.IsNullOrEmpty(Alias) ? TableName : $"{TableName} {Alias}";
    }
}
