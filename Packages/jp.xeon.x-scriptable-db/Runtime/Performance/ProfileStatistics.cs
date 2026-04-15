namespace Xeon.XScriptableDB.Performance
{
    /// <summary>
    /// Profile statistics.
    /// </summary>
    public struct ProfileStatistics
    {
        public int QueryCount;
        public double TotalMilliseconds;
        public double AverageMilliseconds;
        public double MinMilliseconds;
        public double MaxMilliseconds;
        public int CachedQueryCount;

        public override string ToString()
        {
            return $"Queries: {QueryCount}, " +
                   $"Total: {TotalMilliseconds:F2}ms, " +
                   $"Avg: {AverageMilliseconds:F3}ms, " +
                   $"Min: {MinMilliseconds:F3}ms, " +
                   $"Max: {MaxMilliseconds:F3}ms, " +
                   $"Cached: {CachedQueryCount}";
        }
    }
}