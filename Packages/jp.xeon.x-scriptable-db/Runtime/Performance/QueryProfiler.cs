using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Xeon.XScriptableDB.Performance
{
    /// <summary>
    /// クエリプロファイラー。
    /// クエリの実行時間を計測します。
    /// </summary>
    public class QueryProfiler
    {
        private readonly List<QueryProfile> profiles = new();
        private readonly int maxProfiles;
        private readonly object syncLock = new();
        private bool isEnabled = true;

        /// <summary>プロファイリングが有効かどうか</summary>
        public bool IsEnabled
        {
            get => isEnabled;
            set => isEnabled = value;
        }

        /// <summary>記録されたプロファイルの数</summary>
        public int ProfileCount
        {
            get
            {
                lock (syncLock)
                {
                    return profiles.Count;
                }
            }
        }

        public QueryProfiler(int maxProfiles = 1000)
        {
            this.maxProfiles = maxProfiles;
        }

        /// <summary>
        /// クエリの実行を計測します。
        /// </summary>
        /// <typeparam name="T">結果の型</typeparam>
        /// <param name="queryName">クエリ名</param>
        /// <param name="tableType">テーブルの型</param>
        /// <param name="query">実行するクエリ</param>
        /// <returns>クエリ結果</returns>
        public T Profile<T>(string queryName, Type tableType, Func<T> query)
        {
            if (!isEnabled)
            {
                return query();
            }

            var sw = Stopwatch.StartNew();
            var result = query();
            sw.Stop();

            var profile = new QueryProfile
            {
                QueryName = queryName,
                TableType = tableType,
                ElapsedTicks = sw.ElapsedTicks,
                ElapsedMilliseconds = sw.Elapsed.TotalMilliseconds,
                ResultCount = GetResultCount(result),
                Timestamp = DateTime.Now,
                WasCached = false
            };

            AddProfile(profile);
            return result;
        }

        /// <summary>
        /// プロファイルを記録します。
        /// </summary>
        public void RecordProfile(QueryProfile profile)
        {
            if (!isEnabled) return;
            AddProfile(profile);
        }

        /// <summary>
        /// プロファイルエントリを追加します。
        /// </summary>
        private void AddProfile(QueryProfile profile)
        {
            lock (syncLock)
            {
                profiles.Add(profile);

                // 最大数を超えた場合は古いエントリを削除する
                while (profiles.Count > maxProfiles)
                {
                    profiles.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// 結果の数を返します。
        /// </summary>
        private int GetResultCount<T>(T result)
        {
            if (result == null) return 0;

            if (result is System.Collections.ICollection collection)
            {
                return collection.Count;
            }

            if (result is System.Collections.IEnumerable enumerable)
            {
                var count = 0;
                foreach (var _ in enumerable)
                {
                    count++;
                }
                return count;
            }

            return 1;
        }

        /// <summary>
        /// 記録されたすべてのプロファイルを返します。
        /// </summary>
        public IReadOnlyList<QueryProfile> GetProfiles()
        {
            lock (syncLock)
            {
                return profiles.ToArray();
            }
        }

        /// <summary>
        /// プロファイリング統計を返します。
        /// </summary>
        public ProfileStatistics GetStatistics()
        {
            lock (syncLock)
            {
                if (profiles.Count == 0)
                {
                    return new ProfileStatistics();
                }

                var totalMs = 0.0;
                var minMs = double.MaxValue;
                var maxMs = double.MinValue;
                var cachedCount = 0;

                foreach (var profile in profiles)
                {
                    totalMs += profile.ElapsedMilliseconds;
                    minMs = Math.Min(minMs, profile.ElapsedMilliseconds);
                    maxMs = Math.Max(maxMs, profile.ElapsedMilliseconds);
                    if (profile.WasCached) cachedCount++;
                }

                return new ProfileStatistics
                {
                    QueryCount = profiles.Count,
                    TotalMilliseconds = totalMs,
                    AverageMilliseconds = totalMs / profiles.Count,
                    MinMilliseconds = minMs,
                    MaxMilliseconds = maxMs,
                    CachedQueryCount = cachedCount
                };
            }
        }

        /// <summary>
        /// 閾値を超える遅いクエリを返します。
        /// </summary>
        /// <param name="thresholdMs">ミリ秒単位の閾値</param>
        public IReadOnlyList<QueryProfile> GetSlowQueries(double thresholdMs)
        {
            lock (syncLock)
            {
                var result = new List<QueryProfile>();

                foreach (var profile in profiles)
                {
                    if (profile.ElapsedMilliseconds >= thresholdMs)
                    {
                        result.Add(profile);
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// すべてのプロファイルをクリアします。
        /// </summary>
        public void Clear()
        {
            lock (syncLock)
            {
                profiles.Clear();
            }
        }
    }
}
