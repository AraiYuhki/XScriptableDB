using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Cache
{
    /// <summary>
    /// クエリキャッシュのキー。
    /// </summary>
    public readonly struct QueryCacheKey : IEquatable<QueryCacheKey>
    {
        public readonly Type TableType;
        public readonly string QueryType;
        public readonly object KeyValue;

        public QueryCacheKey(Type tableType, string queryType, object keyValue)
        {
            TableType = tableType;
            QueryType = queryType;
            KeyValue = keyValue;
        }

        public bool Equals(QueryCacheKey other)
        {
            return TableType == other.TableType &&
                   QueryType == other.QueryType &&
                   Equals(KeyValue, other.KeyValue);
        }

        public override bool Equals(object obj)
        {
            return obj is QueryCacheKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TableType, QueryType, KeyValue);
        }

        public override string ToString()
        {
            return $"{TableType.Name}.{QueryType}({KeyValue})";
        }
    }

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

        /// <summary>キャッシュの容量</summary>
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
        /// キャッシュから値を取得する。
        /// </summary>
        /// <typeparam name="T">値の型</typeparam>
        /// <param name="tableType">テーブルの型</param>
        /// <param name="queryType">クエリの種類</param>
        /// <param name="keyValue">キー値</param>
        /// <param name="value">取得した値</param>
        /// <returns>キャッシュにヒットした場合はtrue</returns>
        public bool TryGet<T>(Type tableType, string queryType, object keyValue, out T value)
        {
            var cacheKey = new QueryCacheKey(tableType, queryType, keyValue);

            lock (versionLock)
            {
                // バージョンチェック
                if (entryVersions.TryGetValue(cacheKey, out var entryVersion))
                {
                    if (tableVersions.TryGetValue(tableType, out var tableVersion))
                    {
                        if (entryVersion < tableVersion)
                        {
                            // テーブルが更新されているのでキャッシュは無効
                            cache.Remove(cacheKey);
                            entryVersions.Remove(cacheKey);
                            value = default;
                            return false;
                        }
                    }
                }
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
        /// キャッシュに値を設定する。
        /// </summary>
        /// <typeparam name="T">値の型</typeparam>
        /// <param name="tableType">テーブルの型</param>
        /// <param name="queryType">クエリの種類</param>
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
        /// キャッシュから取得、または生成して追加する。
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
        /// テーブルのデータが変更されたことを通知する。
        /// このテーブルに関連するキャッシュは無効化される。
        /// </summary>
        /// <param name="tableType">変更されたテーブルの型</param>
        public void InvalidateTable(Type tableType)
        {
            lock (versionLock)
            {
                if (tableVersions.TryGetValue(tableType, out var version))
                {
                    tableVersions[tableType] = version + 1;
                }
                else
                {
                    tableVersions[tableType] = 1;
                }
            }
        }

        /// <summary>
        /// 特定のクエリのキャッシュを削除する。
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
        /// キャッシュをクリアする。
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
        /// 統計をリセットする。
        /// </summary>
        public void ResetStatistics()
        {
            cache.ResetStatistics();
        }

        /// <summary>
        /// キャッシュ統計を取得する。
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
