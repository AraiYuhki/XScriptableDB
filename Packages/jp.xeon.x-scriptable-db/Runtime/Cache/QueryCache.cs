using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Cache
{
    /// <summary>
    /// Cache for query results.
    /// </summary>
    public class QueryCache
    {
        private readonly LruCache<QueryCacheKey, object> cache;
        private readonly Dictionary<Type, int> tableVersions = new();
        private readonly Dictionary<QueryCacheKey, int> entryVersions = new();
        private readonly object versionLock = new();

        /// <summary>Default cache capacity</summary>
        public const int DefaultCapacity = 1000;

        /// <summary>Cache capacity</summary>
        public int Capacity => cache.Capacity;

        /// <summary>Current number of entries</summary>
        public int Count => cache.Count;

        /// <summary>Hit rate</summary>
        public double HitRate => cache.HitRate;

        /// <summary>Hit count</summary>
        public long HitCount => cache.HitCount;

        /// <summary>Miss count</summary>
        public long MissCount => cache.MissCount;

        public QueryCache(int capacity = DefaultCapacity)
        {
            cache = new LruCache<QueryCacheKey, object>(capacity);
        }

        /// <summary>
        /// Checks whether a cache entry is current by comparing it against the table version.
        /// </summary>
        /// <param name="cacheKey">Cache key to validate</param>
        /// <param name="tableType">Type of the corresponding table</param>
        /// <returns>True if valid; false if invalid and removed</returns>
        private bool ValidateVersion(QueryCacheKey cacheKey, Type tableType)
        {
            lock (versionLock)
            {
                if (!entryVersions.TryGetValue(cacheKey, out var entryVersion))
                    return true;

                if (!tableVersions.TryGetValue(tableType, out var tableVersion))
                    return true;

                if (entryVersion >= tableVersion)
                    return true;

                // The table has been updated, so the cache entry is invalid
                cache.Remove(cacheKey);
                entryVersions.Remove(cacheKey);
                return false;
            }
        }

        /// <summary>
        /// Retrieves a value from the cache.
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="tableType">Type of the table</param>
        /// <param name="queryType">Type of the query</param>
        /// <param name="keyValue">Key value</param>
        /// <param name="value">Retrieved value</param>
        /// <returns>True if the cache was hit</returns>
        public bool TryGet<T>(Type tableType, string queryType, object keyValue, out T value)
        {
            var cacheKey = new QueryCacheKey(tableType, queryType, keyValue);

            // Version check
            if (!ValidateVersion(cacheKey, tableType))
            {
                value = default;
                return false;
            }

            if (cache.TryGet(cacheKey, out var obj))
            {
                value = (T)obj;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Sets a value in the cache.
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="tableType">Type of the table</param>
        /// <param name="queryType">Type of the query</param>
        /// <param name="keyValue">Key value</param>
        /// <param name="value">Value to cache</param>
        public void Set<T>(Type tableType, string queryType, object keyValue, T value)
        {
            var cacheKey = new QueryCacheKey(tableType, queryType, keyValue);

            lock (versionLock)
            {
                var currentVersion = tableVersions.TryGetValue(tableType, out var v) ? v : 0;
                entryVersions[cacheKey] = currentVersion;
            }

            cache.Set(cacheKey, value);
        }

        /// <summary>
        /// Retrieves a value from the cache, or generates and adds it if not present.
        /// </summary>
        public T GetOrAdd<T>(Type tableType, string queryType, object keyValue, Func<T> factory)
        {
            if (TryGet<T>(tableType, queryType, keyValue, out var value))
                return value;

            value = factory();
            Set(tableType, queryType, keyValue, value);
            return value;
        }

        /// <summary>
        /// Notifies that the data in the table has changed.
        /// All cache entries related to this table will be invalidated.
        /// </summary>
        /// <param name="tableType">Type of the changed table</param>
        public void InvalidateTable(Type tableType)
        {
            lock (versionLock)
            {
                tableVersions[tableType] = tableVersions.GetValueOrDefault(tableType, 0) + 1;
            }
        }

        /// <summary>
        /// Removes the cache entry for a specific query.
        /// </summary>
        public void Invalidate(Type tableType, string queryType, object keyValue)
        {
            var cacheKey = new QueryCacheKey(tableType, queryType, keyValue);
            cache.Remove(cacheKey);

            lock (versionLock)
            {
                entryVersions.Remove(cacheKey);
            }
        }

        /// <summary>
        /// Clears the cache.
        /// </summary>
        public void Clear()
        {
            cache.Clear();

            lock (versionLock)
            {
                entryVersions.Clear();
            }
        }

        /// <summary>
        /// Resets statistics.
        /// </summary>
        public void ResetStatistics()
        {
            cache.ResetStatistics();
        }

        /// <summary>
        /// Retrieves cache statistics.
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                Capacity = Capacity,
                Count = Count,
                HitCount = HitCount,
                MissCount = MissCount,
                HitRate = HitRate
            };
        }
    }
}
