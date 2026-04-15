using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Row data from a SELECT result.
    /// </summary>
    public class ResultRow
    {
        public object SourceRecord { get; set; }
        public Dictionary<string, object> Values { get; set; } = new();

        public object this[string columnName] => Values.GetValueOrDefault(columnName, null);
    }
}