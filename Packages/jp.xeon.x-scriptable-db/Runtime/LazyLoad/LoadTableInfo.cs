using System;

namespace Xeon.XScriptableDB.LazyLoad
{
    /// <summary>
    /// ロード済みテーブルの情報。
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