using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Xeon.XScriptableDB.Samples.QueryResultZeroGC
{
    /// <summary>
    /// QueryResult (Zero GC) サンプルのロジック部分。
    /// GUIから呼び出されることを想定。
    /// </summary>
    public class QueryResultZeroGCSample : MonoBehaviour
    {
        [SerializeField]
        private EnemyTable enemyTable;

        /// <summary>
        /// Zero GCパターンでレベル範囲検索を実行する。
        /// </summary>
        /// <param name="minLevel">最小レベル</param>
        /// <param name="maxLevel">最大レベル</param>
        /// <returns>検索結果の件数</returns>
        public int SearchByLevelRangeZeroGC(int minLevel, int maxLevel)
        {
            using var results = enemyTable.Where(e =>
                e.Level >= minLevel && e.Level <= maxLevel);

            // Zero GC: ref readonlyで値コピーを回避
            int count = 0;
            foreach (ref readonly var enemy in results)
            {
                count++;
                // 実際の処理をここに記述
            }

            return count;
        }

        /// <summary>
        /// 通常のLINQパターンでレベル範囲検索を実行する（比較用）。
        /// </summary>
        /// <param name="minLevel">最小レベル</param>
        /// <param name="maxLevel">最大レベル</param>
        /// <returns>検索結果の件数</returns>
        public int SearchByLevelRangeLinq(int minLevel, int maxLevel)
        {
            // GCアロケーションが発生するパターン
            var results = enemyTable.All
                .Where(e => e.Level >= minLevel && e.Level <= maxLevel)
                .ToList();

            return results.Count;
        }

        /// <summary>
        /// SecondaryKeyを使用したエリア検索（最速）。
        /// </summary>
        /// <param name="areaId">エリアID</param>
        /// <returns>検索結果の件数</returns>
        public int SearchByAreaSecondaryKey(int areaId)
        {
            var results = enemyTable.FindAllBySecondaryKey("areaId", areaId);
            return results.Count();
        }

        /// <summary>
        /// ボス敵のみを検索する（Zero GC）。
        /// </summary>
        /// <returns>ボス敵のリスト</returns>
        public List<EnemyRecord> GetBossEnemiesZeroGC()
        {
            // SecondaryKeyを利用
            var bosses = enemyTable.FindAllBySecondaryKey("isBoss", true);
            return bosses.ToList();
        }

        /// <summary>
        /// 複合条件検索（Zero GC）。
        /// </summary>
        /// <param name="areaId">エリアID</param>
        /// <param name="minLevel">最小レベル</param>
        /// <returns>検索結果の件数</returns>
        public int SearchComplexZeroGC(int areaId, int minLevel)
        {
            // SecondaryKeyで絞り込んでからWhere
            var areaEnemies = enemyTable.FindAllBySecondaryKey("areaId", areaId);

            using var results = areaEnemies.Where(e => e.Level >= minLevel);

            int count = 0;
            foreach (ref readonly var enemy in results)
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// パフォーマンスベンチマークを実行する。
        /// </summary>
        /// <param name="iterations">繰り返し回数</param>
        /// <returns>比較結果</returns>
        public ComparisonResult RunBenchmark(int iterations = 1000)
        {
            int minLevel = 10;
            int maxLevel = 50;

            return PerformanceProfiler.Compare(
                "Zero GC (QueryResult)",
                () => SearchByLevelRangeZeroGC(minLevel, maxLevel),
                "通常LINQ (ToList)",
                () => SearchByLevelRangeLinq(minLevel, maxLevel),
                iterations
            );
        }

        /// <summary>
        /// 3種類の検索方式を比較するベンチマークを実行する。
        /// </summary>
        /// <param name="iterations">繰り返し回数</param>
        /// <returns>各方式の計測結果</returns>
        public Dictionary<string, ProfileResult> RunFullBenchmark(int iterations = 1000)
        {
            int minLevel = 10;
            int maxLevel = 50;
            int areaId = 2;

            var results = new Dictionary<string, ProfileResult>
            {
                ["Zero GC (QueryResult)"] = PerformanceProfiler.Measure(
                    () => SearchByLevelRangeZeroGC(minLevel, maxLevel), iterations),

                ["通常LINQ (ToList)"] = PerformanceProfiler.Measure(
                    () => SearchByLevelRangeLinq(minLevel, maxLevel), iterations),

                ["SecondaryKey"] = PerformanceProfiler.Measure(
                    () => SearchByAreaSecondaryKey(areaId), iterations)
            };

            return results;
        }

        /// <summary>
        /// 検索結果のプレビューを取得する（先頭N件）。
        /// </summary>
        /// <param name="minLevel">最小レベル</param>
        /// <param name="maxLevel">最大レベル</param>
        /// <param name="limit">取得件数上限</param>
        /// <returns>プレビュー用のレコードリスト</returns>
        public List<EnemyRecord> GetPreview(int minLevel, int maxLevel, int limit = 10)
        {
            var preview = new List<EnemyRecord>(limit);

            using var results = enemyTable.Where(e =>
                e.Level >= minLevel && e.Level <= maxLevel);

            int count = 0;
            foreach (ref readonly var enemy in results)
            {
                if (count >= limit)
                    break;

                // プレビュー用にコピー（表示には必要）
                preview.Add(enemy);
                count++;
            }

            return preview;
        }

        /// <summary>
        /// 現在のテーブルのレコード数を取得する。
        /// </summary>
        public int RecordCount => enemyTable?.RecordCount ?? 0;

        /// <summary>
        /// 現在のGCメモリ使用量を取得する。
        /// </summary>
        public long CurrentMemoryUsage => GC.GetTotalMemory(false);

        /// <summary>
        /// GCを強制実行する。
        /// </summary>
        public void ForceGC()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
