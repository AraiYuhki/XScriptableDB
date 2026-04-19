using System;

namespace Xeon.XScriptableDB.Cache
{
    /// <summary>
    /// キャッシュマネージャー。
    /// グローバルキャッシュインスタンスを管理します。
    /// </summary>
    public static class CacheManager
    {
        private static QueryCache queryCache;
        private static bool isEnabled = true;

        /// <summary>キャッシュが有効かどうか</summary>
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

        /// <summary>デフォルトのキャッシュ容量</summary>
        public static int DefaultCapacity { get; set; } = 1000;

        /// <summary>
        /// クエリキャッシュを返します。
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
        /// キャッシュを初期化します。
        /// </summary>
        /// <param name="capacity">キャッシュ容量</param>
        public static void Initialize(int capacity)
        {
            queryCache = new QueryCache(capacity);
        }

        /// <summary>
        /// テーブルのキャッシュを無効にします。
        /// </summary>
        /// <typeparam name="T">テーブルの型</typeparam>
        public static void InvalidateTable<T>() where T : ITableAsset
        {
            QueryCache.InvalidateTable(typeof(T));
        }

        /// <summary>
        /// テーブルのキャッシュを無効にします。
        /// </summary>
        /// <param name="tableType">テーブルの型</param>
        public static void InvalidateTable(Type tableType)
        {
            QueryCache.InvalidateTable(tableType);
        }

        /// <summary>
        /// すべてのキャッシュをクリアします。
        /// </summary>
        public static void Clear()
        {
            queryCache?.Clear();
        }

        /// <summary>
        /// 統計をリセットします。
        /// </summary>
        public static void ResetStatistics()
        {
            queryCache?.ResetStatistics();
        }

        /// <summary>
        /// キャッシュの統計を返します。
        /// </summary>
        public static CacheStatistics GetStatistics()
        {
            return queryCache?.GetStatistics() ?? default;
        }
    }
}
