using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// JOIN操作の結果のレコード。
    /// </summary>
    public class JoinedRecord
    {
        /// <summary>テーブル名/エイリアスからレコードへのマップ</summary>
        public Dictionary<string, object> TableRecords { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>テーブル名/エイリアスからレコード型へのマップ</summary>
        public Dictionary<string, Type> TableTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public object GetRecord(string tableAlias) => TableRecords.GetValueOrDefault(tableAlias, null);

        public Type GetRecordType(string tableAlias) => TableTypes.GetValueOrDefault(tableAlias, null);
    }
}