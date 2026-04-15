using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// A group of aggregated results.
    /// </summary>
    public class AggregateGroup
    {
        /// <summary>Map from group key column name to value</summary>
        public Dictionary<string, object> GroupKeyValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<object> Records { get; set; } = new();
    }
}