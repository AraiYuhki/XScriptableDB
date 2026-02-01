using System;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SET句の項目（UPDATE用）。
    /// </summary>
    public class SetItem
    {
        public string ColumnName { get; set; }
        public SqlExpression Value { get; set; }

        public override string ToString() => $"{ColumnName} = {Value}";
    }
}
