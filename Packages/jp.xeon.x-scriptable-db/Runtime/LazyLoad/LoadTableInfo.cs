using System;

namespace Xeon.XScriptableDB.LazyLoad
{
    /// <summary>
    /// ロードされたテーブルに関する情報。
    /// </summary>
    public struct LoadedTableInfo
    {
        public Type TableType;
        public int ReferenceCount;
        public int RecordCount;

        public override string ToString()
        {
            return $"{TableType.Name}: {RecordCount} records, {ReferenceCount} refs";
        }
    }
}