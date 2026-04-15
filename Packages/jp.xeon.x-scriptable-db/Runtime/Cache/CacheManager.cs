using System;

namespace Xeon.XScriptableDB.Cache
{
    /// <summary>
    /// Cache manager.
    /// Manages the global cache instance.
    /// </summary>
    public static class CacheManager
    {
        private static QueryCache queryCache;
        private static bool isEnabled = true;

        /// <summary>Whether the cache is enabled</summary>
        public static bool IsEnabled
        {
            get => isEnabled;
            set
            {
                isEnabled = value;
                if (!value)
                {
                    Clear();
                }
            }
        }

        /// <summary>Default cache capacity</summary>
        public static int DefaultCapacity { get; set; } = 1000;

        /// <summary>
        /// Returns the query cache.
        /// </summary>
        public static QueryCache QueryCache
        {
            get
            {
                queryCache ??= new QueryCache(DefaultCapacity);
                return queryCache;
            }
        }

        /// <summary>
        /// Initializes the cache.
        /// </summary>
        /// <param name="capacity">Cache capacity</param>
        public static void Initialize(int capacity)
        {
            queryCache = new QueryCache(capacity);
        }

        /// <summary>
        /// Invalidates the cache for a table.
        /// </summary>
        /// <typeparam name="T">Type of the table</typeparam>
        public static void InvalidateTable<T>() where T : ITableAsset
        {
            QueryCache.InvalidateTable(typeof(T));
        }

        /// <summary>
        /// Invalidates the cache for a table.
        /// </summary>
        /// <param name="tableType">Type of the table</param>
        public static void InvalidateTable(Type tableType)
        {
            QueryCache.InvalidateTable(tableType);
        }

        /// <summary>
        /// Clears all caches.
        /// </summary>
        public static void Clear()
        {
            queryCache?.Clear();
        }

        /// <summary>
        /// Resets the statistics.
        /// </summary>
        public static void ResetStatistics()
        {
            queryCache?.ResetStatistics();
        }

        /// <summary>
        /// Returns the cache statistics.
        /// </summary>
        public static CacheStatistics GetStatistics()
        {
            return queryCache?.GetStatistics() ?? default;
        }
    }
}
