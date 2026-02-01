using System;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// ƒŠƒeƒ‰ƒ‹’lB
    /// </summary>
    public class LiteralExpression : SqlExpression
    {
        public object Value { get; set; }
        public Type ValueType { get; set; }

        public LiteralExpression(object value)
        {
            Value = value;
            ValueType = value?.GetType();
        }

        public override string ToString() => Value?.ToString() ?? "NULL";
    }
}
