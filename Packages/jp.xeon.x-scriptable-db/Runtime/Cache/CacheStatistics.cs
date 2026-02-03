namespace Xeon.XScriptableDB.Cache
{
    /// <summary>
    /// キャッシュ統計。
    /// </summary>
    public struct CacheStatistics
    {
        public int Capacity;
        public int Count;
        public long HitCount;
        public long MissCount;
        public double HitRate;

        public override string ToString()
        {
            return $"Cache: {Count}/{Capacity}, Hit: {HitCount}, Miss: {MissCount}, Rate: {HitRate:P1}";
        }
    }
}