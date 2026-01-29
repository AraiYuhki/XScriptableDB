using System;

namespace Xeon.XScriptableDB.Cache
{
    /// <summary>
    /// キャッシュマネージャー。
    /// グローバルなキャッシュインスタンスを管理する。
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
        /// クエリキャッシュを取得する。
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
        /// キャッシュを初期化する。
        /// </summary>
        /// <param name="capacity">キャッシュ容量</param>
        public static void Initialize(int capacity)
        {
            queryCache = new QueryCache(capacity);
        }

        /// <summary>
        /// テーブルのキャッシュを無効化する。
        /// </summary>
        /// <typeparam name="T">テーブルの型</typeparam>
        public static void InvalidateTable<T>() where T : ITableAsset
        {
            QueryCache.InvalidateTable(typeof(T));
        }

        /// <summary>
        /// テーブルのキャッシュを無効化する。
        /// </summary>
        /// <param name="tableType">テーブルの型</param>
        public static void InvalidateTable(Type tableType)
        {
            QueryCache.InvalidateTable(tableType);
        }

        /// <summary>
        /// 全てのキャッシュをクリアする。
        /// </summary>
        public static void Clear()
        {
            queryCache?.Clear();
        }

        /// <summary>
        /// 統計をリセットする。
        /// </summary>
        public static void ResetStatistics()
        {
            queryCache?.ResetStatistics();
        }

        /// <summary>
        /// キャッシュ統計を取得する。
        /// </summary>
        public static CacheStatistics GetStatistics()
        {
            return queryCache?.GetStatistics() ?? default;
        }
    }
}
