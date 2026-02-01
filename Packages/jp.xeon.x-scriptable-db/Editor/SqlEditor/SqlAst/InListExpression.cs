using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// IN式の値リスト。
    /// </summary>
    public class InListExpression : SqlExpression
    {
        public List<SqlExpression> Values { get; set; } = new();

        public override string ToString()
        {
            return $"({string.Join(", ", Values)})";
        }
    }
}
