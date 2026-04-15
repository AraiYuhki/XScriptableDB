using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Record resulting from a JOIN operation.
    /// </summary>
    public class JoinedRecord
    {
        /// <summary>Map from table name/alias to record</summary>
        public Dictionary<string, object> TableRecords { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Map from table name/alias to record type</summary>
        public Dictionary<string, Type> TableTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public object GetRecord(string tableAlias) => TableRecords.GetValueOrDefault(tableAlias, null);

        public Type GetRecordType(string tableAlias) => TableTypes.GetValueOrDefault(tableAlias, null);
    }
}