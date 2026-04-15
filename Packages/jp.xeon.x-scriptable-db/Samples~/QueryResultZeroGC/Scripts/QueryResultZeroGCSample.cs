using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Xeon.XScriptableDB.Samples.QueryResultZeroGC
{
    /// <summary>
    /// Logic for the QueryResult (Zero GC) sample.
    /// Intended to be called from a GUI.
    /// </summary>
    public class QueryResultZeroGCSample : MonoBehaviour
    {
        [SerializeField]
        private EnemyTable enemyTable;

        /// <summary>
        /// Executes a level range search using the Zero GC pattern.
        /// </summary>
        /// <param name="minLevel">Minimum level</param>
        /// <param name="maxLevel">Maximum level</param>
        /// <returns>Number of results</returns>
        public int SearchByLevelRangeZeroGC(int minLevel, int maxLevel)
        {
            // Get a zero-GC-alloc QueryResult with QueryBySecondaryKey
            // Note: Where is used for level range search (returns IEnumerable<T>)
            int count = 0;
            foreach (var enemy in enemyTable.Where(e =>
                e.Level >= minLevel && e.Level <= maxLevel))
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// Executes a level range search using the normal LINQ pattern (for comparison).
        /// </summary>
        /// <param name="minLevel">Minimum level</param>
        /// <param name="maxLevel">Maximum level</param>
        /// <returns>Number of results</returns>
        public int SearchByLevelRangeLinq(int minLevel, int maxLevel)
        {
            // Pattern that causes GC allocation
            var results = enemyTable.All
                .Where(e => e.Level >= minLevel && e.Level <= maxLevel)
                .ToList();

            return results.Count;
        }

        /// <summary>
        /// Area search using SecondaryKey (fastest).
        /// </summary>
        /// <param name="areaId">Area ID</param>
        /// <returns>Number of results</returns>
        public int SearchByAreaSecondaryKey(int areaId)
        {
            var results = enemyTable.FindAllBySecondaryKeyAsArray("areaId", areaId);
            return results.Length;
        }

        /// <summary>
        /// Searches for boss enemies only.
        /// </summary>
        /// <returns>List of boss enemies</returns>
        public List<EnemyRecord> GetBossEnemiesZeroGC()
        {
            // Using SecondaryKey
            var bosses = enemyTable.FindAllBySecondaryKey("isBoss", true);
            return bosses.ToList();
        }

        /// <summary>
        /// Multi-condition search.
        /// </summary>
        /// <param name="areaId">Area ID</param>
        /// <param name="minLevel">Minimum level</param>
        /// <returns>Number of results</returns>
        public int SearchComplexZeroGC(int areaId, int minLevel)
        {
            // Narrow down by SecondaryKey then apply additional filter with Where
            var areaEnemies = enemyTable.FindAllBySecondaryKey("areaId", areaId);

            int count = 0;
            foreach (var enemy in areaEnemies)
            {
                if (enemy.Level >= minLevel)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Runs a performance benchmark.
        /// </summary>
        /// <param name="iterations">Number of iterations</param>
        /// <returns>Comparison result</returns>
        public ComparisonResult RunBenchmark(int iterations = 1000)
        {
            int minLevel = 10;
            int maxLevel = 50;

            return PerformanceProfiler.Compare(
                "Zero GC (QueryResult)",
                () => SearchByLevelRangeZeroGC(minLevel, maxLevel),
                "Normal LINQ (ToList)",
                () => SearchByLevelRangeLinq(minLevel, maxLevel),
                iterations
            );
        }

        /// <summary>
        /// Runs a benchmark comparing three search methods.
        /// </summary>
        /// <param name="iterations">Number of iterations</param>
        /// <returns>Measurement results for each method</returns>
        public Dictionary<string, ProfileResult> RunFullBenchmark(int iterations = 1000)
        {
            int minLevel = 10;
            int maxLevel = 50;
            int areaId = 2;

            var results = new Dictionary<string, ProfileResult>
            {
                ["Zero GC (QueryResult)"] = PerformanceProfiler.Measure(
                    () => SearchByLevelRangeZeroGC(minLevel, maxLevel), iterations),

                ["Normal LINQ (ToList)"] = PerformanceProfiler.Measure(
                    () => SearchByLevelRangeLinq(minLevel, maxLevel), iterations),

                ["SecondaryKey"] = PerformanceProfiler.Measure(
                    () => SearchByAreaSecondaryKey(areaId), iterations)
            };

            return results;
        }

        /// <summary>
        /// Gets a preview of search results (first N records).
        /// </summary>
        /// <param name="minLevel">Minimum level</param>
        /// <param name="maxLevel">Maximum level</param>
        /// <param name="limit">Maximum number of records to retrieve</param>
        /// <returns>List of preview records</returns>
        public List<EnemyRecord> GetPreview(int minLevel, int maxLevel, int limit = 10)
        {
            var preview = new List<EnemyRecord>(limit);

            foreach (var enemy in enemyTable.Where(e =>
                e.Level >= minLevel && e.Level <= maxLevel))
            {
                if (preview.Count >= limit)
                    break;
                preview.Add(enemy);
            }

            return preview;
        }

        /// <summary>
        /// Gets the number of records in the current table.
        /// </summary>
        public int RecordCount => enemyTable?.Count ?? 0;

        /// <summary>
        /// Gets the current GC memory usage.
        /// </summary>
        public long CurrentMemoryUsage => GC.GetTotalMemory(false);

        /// <summary>
        /// Forces a GC collection.
        /// </summary>
        public void ForceGC()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
