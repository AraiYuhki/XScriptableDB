using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// 集約結果のグループ。
    /// </summary>
    public class AggregateGroup
    {
        /// <summary>グループキーの列名から値へのマップ</summary>
        public Dictionary<string, object> GroupKeyValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<object> Records { get; set; } = new();
    }
}