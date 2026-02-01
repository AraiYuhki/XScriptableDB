using System;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SELECT‹å‚Ì—ñw’èB
    /// </summary>
    public class SelectColumn
    {
        public SqlExpression Expression { get; set; }
        public string Alias { get; set; }
        public bool IsWildcard { get; set; }

        public override string ToString()
        {
            if (IsWildcard)
                return "*";
            return string.IsNullOrEmpty(Alias) ? Expression.ToString() : $"{Expression} AS {Alias}";
        }
    }
}
