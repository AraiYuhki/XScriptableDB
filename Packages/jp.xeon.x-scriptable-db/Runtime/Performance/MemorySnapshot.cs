using System;

namespace Xeon.XScriptableDB.Performance
{
    /// <summary>
    /// Memory snapshot.
    /// </summary>
    public struct MemorySnapshot
    {
        public DateTime Timestamp;
        public long TotalMemory;
        public int GCCollectionCount0;
        public int GCCollectionCount1;
        public int GCCollectionCount2;

        public override string ToString()
        {
            return $"Memory: {TotalMemory / 1024.0 / 1024.0:F2} MB, " +
                   $"GC: [{GCCollectionCount0}, {GCCollectionCount1}, {GCCollectionCount2}]";
        }
    }
}