using System;

namespace Xeon.XScriptableDB.LazyLoad
{
    /// <summary>
    /// Information about a loaded table.
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