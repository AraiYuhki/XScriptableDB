using System;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// ORDER BYãÂÇÃçÄñ⁄ÅB
    /// </summary>
    public class OrderByItem
    {
        public SqlExpression Expression { get; set; }
        public Xeon.XScriptableDB.Editor.SortOrder Order { get; set; } = Xeon.XScriptableDB.Editor.SortOrder.Ascending;

        public override string ToString()
        {
            var orderStr = Order == Xeon.XScriptableDB.Editor.SortOrder.Descending ? " DESC" : "";
            return $"{Expression}{orderStr}";
        }
    }
}
