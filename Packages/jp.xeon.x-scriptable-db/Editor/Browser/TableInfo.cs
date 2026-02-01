using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// テーブル情報。
    /// </summary>
    public class TableInfo
    {
        public string Name { get; set; }
        public string AssetPath { get; set; }
        public ScriptableObject Asset { get; set; }
        public ITableAsset TableAsset { get; set; }
        public Type RecordType { get; set; }
        public int RecordCount { get; set; }
        public List<ColumnInfo> Columns { get; set; } = new();
    }
}
