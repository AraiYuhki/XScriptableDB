using System;

namespace Xeon.XScriptableDB.Performance
{
    /// <summary>
    /// クエリプロファイル結果。
    /// </summary>
    public struct QueryProfile
    {
        public string QueryName;
        public Type TableType;
        public long ElapsedTicks;
        public double ElapsedMilliseconds;
        public int ResultCount;
        public DateTime Timestamp;
        public bool WasCached;

        public override string ToString()
        {
            var cached = WasCached ? " (cached)" : "";
            return $"[{TableType?.Name}] {QueryName}: {ElapsedMilliseconds:F3}ms, {ResultCount} results{cached}";
        }
    }
}