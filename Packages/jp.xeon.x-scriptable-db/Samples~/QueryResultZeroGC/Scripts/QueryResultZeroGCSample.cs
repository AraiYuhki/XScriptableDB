using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Xeon.XScriptableDB.Samples.QueryResultZeroGC
{
    /// <summary>
    /// QueryResult (Zero GC) サンプルのロジック。
    /// GUIから呼び出されることを想定しています。
    /// </summary>
    public class QueryResultZeroGCSample : MonoBehaviour
    {
        [SerializeField]
        private EnemyTable enemyTable;

        /// <summary>
        /// Zero GCパターンを使用してレベル範囲検索を実行します。
        /// </summary>
        /// <param name="minLevel">最小レベル</param>
        /// <param name="maxLevel">最大レベル</param>
        /// <returns>結果の数</returns>
        public int SearchByLevelRangeZeroGC(int minLevel, int maxLevel)
        {
            // QueryBySecondaryKeyでGCアロケーションなしのQueryResultを取得します
            // 注：レベル範囲検索にはWhereを使用します（IEnumerable<T>を返します）
            int count = 0;
            foreach (var enemy in enemyTable.Where(e =>
                e.Level >= minLevel && e.Level <= maxLevel))
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// 通常のLINQパターンを使用してレベル範囲検索を実行します（比較用）。
        /// </summary>
        /// <param name="minLevel">最小レベル</param>
        /// <param name="maxLevel">最大レベル</param>
        /// <returns>結果の数</returns>
        public int SearchByLevelRangeLinq(int minLevel, int maxLevel)
        {
            // GCアロケーションを発生させるパターン
            var results = enemyTable.All
                .Where(e => e.Level >= minLevel && e.Level <= maxLevel)
                .ToList();

            return results.Count;
        }

        /// <summary>
        /// SecondaryKeyを使用したエリア検索（最速）。
        /// </summary>
        /// <param name="areaId">エリアID</param>
        /// <returns>結果の数</returns>
        public int SearchByAreaSecondaryKey(int areaId)
        {
            var results = enemyTable.FindAllBySecondaryKeyAsArray("areaId", areaId);
            return results.Length;
        }

        /// <summary>
        /// ボスエネミーのみを検索します。
        /// </summary>
        /// <returns>ボスエネミーのリスト</returns>
        public List<EnemyRecord> GetBossEnemiesZeroGC()
        {
            // SecondaryKeyを使用
            var bosses = enemyTable.FindAllBySecondaryKey("isBoss", true);
            return bosses.ToList();
        }

        /// <summary>
        /// 複数条件検索。
        /// </summary>
        /// <param name="areaId">エリアID</param>
        /// <param name="minLevel">最小レベル</param>
        /// <returns>結果の数</returns>
        public int SearchComplexZeroGC(int areaId, int minLevel)
        {
            // SecondaryKeyで絞り込んだ後、Whereで追加のフィルターを適用します
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
        /// パフォーマンスベンチマークを実行します。
        /// </summary>
        /// <param name="iterations">試行回数</param>
        /// <returns>比較結果</returns>
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
        /// 3つの検索方法を比較するベンチマークを実行します。
        /// </summary>
        /// <param name="iterations">試行回数</param>
        /// <returns>各メソッドの測定結果</returns>
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
        /// 検索結果のプレビューを取得します（最初のNレコード）。
        /// </summary>
        /// <param name="minLevel">最小レベル</param>
        /// <param name="maxLevel">最大レベル</param>
        /// <param name="limit">取得する最大レコード数</param>
        /// <returns>プレビューレコードのリスト</returns>
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
        /// 現在のテーブルのレコード数を取得します。
        /// </summary>
        public int RecordCount => enemyTable?.Count ?? 0;

        /// <summary>
        /// 現在のGCメモリ使用量を取得します。
        /// </summary>
        public long CurrentMemoryUsage => GC.GetTotalMemory(false);

        /// <summary>
        /// GCコレクションを強制的に実行します。
        /// </summary>
        public void ForceGC()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
