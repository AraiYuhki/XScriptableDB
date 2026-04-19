using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Cache
{
    /// <summary>
    /// クエリ結果のキャッシュ。
    /// </summary>
    public class QueryCache
    {
        private readonly LruCache<QueryCacheKey, object> cache;
        private readonly Dictionary<Type, int> tableVersions = new();
        private readonly Dictionary<QueryCacheKey, int> entryVersions = new();
        private readonly object versionLock = new();

        /// <summary>デフォルトのキャッシュ容量</summary>
        public const int DefaultCapacity = 1000;

        /// <summary>キャッシュ容量</summary>
        public int Capacity => cache.Capacity;

        /// <summary>現在のエントリ数</summary>
        public int Count => cache.Count;

        /// <summary>ヒット率</summary>
        public double HitRate => cache.HitRate;

        /// <summary>ヒット数</summary>
        public long HitCount => cache.HitCount;

        /// <summary>ミス数</summary>
        public long MissCount => cache.MissCount;

        public QueryCache(int capacity = DefaultCapacity)
        {
            cache = new LruCache<QueryCacheKey, object>(capacity);
        }

        /// <summary>
        /// キャッシュエントリがテーブルのバージョンと比較して最新かどうかを確認します。
        /// </summary>
        /// <param name="cacheKey">検証するキャッシュキー</param>
        /// <param name="tableType">対応するテーブルの型</param>
        /// <returns>有効な場合はtrue、無効で削除された場合はfalse</returns>
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

                // テーブルが更新されているため、キャッシュエントリは無効です
                cache.Remove(cacheKey);
                entryVersions.Remove(cacheKey);
                return false;
            }
        }

        /// <summary>
        /// キャッシュから値を取得します。
        /// </summary>
        /// <typeparam name="T">値の型</typeparam>
        /// <param name="tableType">テーブルの型</param>
        /// <param name="queryType">クエリの型</param>
        /// <param name="keyValue">キー値</param>
        /// <param name="value">取得された値</param>
        /// <returns>キャッシュにヒットした場合はtrue</returns>
        public bool TryGet<T>(Type tableType, string queryType, object keyValue, out T value)
        {
            var cacheKey = new QueryCacheKey(tableType, queryType, keyValue);

            // バージョンチェック
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
        /// キャッシュに値を設定します。
        /// </summary>
        /// <typeparam name="T">値の型</typeparam>
        /// <param name="tableType">テーブルの型</param>
        /// <param name="queryType">クエリの型</param>
        /// <param name="keyValue">キー値</param>
        /// <param name="value">キャッシュする値</param>
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
        /// キャッシュから値を取得します。存在しない場合は生成して追加します。
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
        /// テーブルのデータが変更されたことを通知します。
        /// このテーブルに関連するすべてのキャッシュエントリが無効になります。
        /// </summary>
        /// <param name="tableType">変更されたテーブルの型</param>
        public void InvalidateTable(Type tableType)
        {
            lock (versionLock)
            {
                tableVersions[tableType] = tableVersions.GetValueOrDefault(tableType, 0) + 1;
            }
        }

        /// <summary>
        /// 特定のクエリのキャッシュエントリを削除します。
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
        /// キャッシュをクリアします。
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
        /// 統計をリセットします。
        /// </summary>
        public void ResetStatistics()
        {
            cache.ResetStatistics();
        }

        /// <summary>
        /// キャッシュの統計を取得します。
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
