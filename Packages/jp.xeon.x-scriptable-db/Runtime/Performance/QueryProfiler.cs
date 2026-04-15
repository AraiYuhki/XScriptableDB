using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Xeon.XScriptableDB.Performance
{
    /// <summary>
    /// Query profiler.
    /// Measures the execution time of queries.
    /// </summary>
    public class QueryProfiler
    {
        private readonly List<QueryProfile> profiles = new();
        private readonly int maxProfiles;
        private readonly object syncLock = new();
        private bool isEnabled = true;

        /// <summary>Whether profiling is enabled</summary>
        public bool IsEnabled
        {
            get => isEnabled;
            set => isEnabled = value;
        }

        /// <summary>Number of recorded profiles</summary>
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
        /// Measures the execution of a query.
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="queryName">Query name</param>
        /// <param name="tableType">Type of the table</param>
        /// <param name="query">Query to execute</param>
        /// <returns>Query result</returns>
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
        /// Records a profile.
        /// </summary>
        public void RecordProfile(QueryProfile profile)
        {
            if (!isEnabled) return;
            AddProfile(profile);
        }

        /// <summary>
        /// Adds a profile entry.
        /// </summary>
        private void AddProfile(QueryProfile profile)
        {
            lock (syncLock)
            {
                profiles.Add(profile);

                // Remove old entries when the maximum count is exceeded
                while (profiles.Count > maxProfiles)
                {
                    profiles.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Returns the count of results.
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
        /// Returns all recorded profiles.
        /// </summary>
        public IReadOnlyList<QueryProfile> GetProfiles()
        {
            lock (syncLock)
            {
                return profiles.ToArray();
            }
        }

        /// <summary>
        /// Returns profiling statistics.
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
        /// Returns slow queries above the threshold.
        /// </summary>
        /// <param name="thresholdMs">Threshold in milliseconds</param>
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
        /// Clears all profiles.
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
