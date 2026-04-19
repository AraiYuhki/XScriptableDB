using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SELECT結果からの行データ。
    /// </summary>
    public class ResultRow
    {
        public object SourceRecord { get; set; }
        public Dictionary<string, object> Values { get; set; } = new();

        public object this[string columnName] => Values.GetValueOrDefault(columnName, null);
    }
}