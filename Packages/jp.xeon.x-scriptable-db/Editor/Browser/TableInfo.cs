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
        public string Name { get; }
        public string AssetPath { get; }
        public ScriptableObject Asset { get; }
        public ITableAsset TableAsset { get; }
        public Type RecordType { get; }
        public int RecordCount { get; }
        public List<ColumnInfo> Columns { get; }

        public TableInfo(string name, string assetPath, ScriptableObject asset, ITableAsset tableAsset, Type recordType, int recordCount, List<ColumnInfo> columns)
        {
            Name = name;
            AssetPath = assetPath;
            Asset = asset;
            TableAsset = tableAsset;
            RecordType = recordType;
            RecordCount = recordCount;
            Columns = columns;
        }
    }
}
