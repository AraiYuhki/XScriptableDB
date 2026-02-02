using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// ŠÖ”ŒÄ‚Ño‚µ®B
    /// </summary>
    public class FunctionCallExpression : SqlExpression
    {
        public string FunctionName { get; set; }
        public List<SqlExpression> Arguments { get; set; } = new();

        public override string ToString()
        {
            var args = string.Join(", ", Arguments);
            return $"{FunctionName}({args})";
        }
    }
}
