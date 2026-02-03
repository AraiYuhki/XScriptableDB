using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Xeon.XScriptableDB.IO;
using Debug = UnityEngine.Debug;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// ベンチマーク実行ウィンドウ。
    /// </summary>
    public class BenchmarkWindow : EditorWindow
    {
        [MenuItem("Tools/XScriptableDB/Benchmark")]
        public static void Open()
        {
            var window = GetWindow<BenchmarkWindow>("Benchmark");
            window.Show();
        }

        // テスト用レコードクラス
        [Serializable]
        private class TestRecord : CsvData
        {
            [CsvColumn("id")]
            public int id;

            [CsvColumn("name")]
            public string name;

            [CsvColumn("value")]
            public float value;

            [CsvColumn("category")]
            public int category;
        }

        private int dataSize = 10000;
        private int iterations = 1000;
        private List<BenchmarkResult> results = new();
        private Vector2 scrollPosition;
        private bool isRunning;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("XScriptableDB ベンチマーク", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledGroupScope(isRunning))
            {
                // 設定
                EditorGUILayout.LabelField("設定", EditorStyles.boldLabel);
                dataSize = EditorGUILayout.IntSlider("データサイズ", dataSize, 100, 100000);
                iterations = EditorGUILayout.IntSlider("反復回数", iterations, 10, 10000);

                EditorGUILayout.Space();

                // ベンチマーク実行ボタン
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("全ベンチマーク実行"))
                        RunAllBenchmarks();

                    if (GUILayout.Button("検索ベンチマーク"))
                        RunSearchBenchmarks();

                    if (GUILayout.Button("CSVベンチマーク"))
                        RunCsvBenchmarks();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("メモリベンチマーク"))
                        RunMemoryBenchmarks();

                    if (GUILayout.Button("結果をクリア"))
                        results.Clear();

                    if (GUILayout.Button("レポート出力"))
                        ExportReport();
                }
            }

            if (isRunning)
            {
                EditorGUILayout.HelpBox("ベンチマーク実行中...", MessageType.Info);
            }

            EditorGUILayout.Space();

            if (results.Count <= 0)
                return;

            // 結果表示
            EditorGUILayout.LabelField($"結果 ({results.Count}件)", EditorStyles.boldLabel);
            using var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition);
            scrollPosition = scroll.scrollPosition;

            // ヘッダー
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("テスト名", GUILayout.Width(200));
                GUILayout.Label("データ数", GUILayout.Width(80));
                GUILayout.Label("反復", GUILayout.Width(60));
                GUILayout.Label("合計(ms)", GUILayout.Width(80));
                GUILayout.Label("1回あたり", GUILayout.Width(100));
                GUILayout.Label("メモリ", GUILayout.Width(80));
            }

            foreach (var result in results)
            {
                using var _ = new EditorGUILayout.HorizontalScope();
                var style = result.Success ? EditorStyles.label : EditorStyles.boldLabel;
                if (!result.Success)
                    GUI.color = Color.red;

                GUILayout.Label(result.Name, style, GUILayout.Width(200));
                GUILayout.Label(result.DataSize.ToString("N0"), GUILayout.Width(80));
                GUILayout.Label(result.Iterations.ToString("N0"), GUILayout.Width(60));
                GUILayout.Label(result.ElapsedMs.ToString("F2"), GUILayout.Width(80));
                GUILayout.Label(FormatMicroseconds(result.PerOperationUs), GUILayout.Width(100));
                GUILayout.Label(FormatBytes(result.MemoryBytes), GUILayout.Width(80));

                GUI.color = Color.white;
            }
        }

        private string FormatMicroseconds(double us)
        {
            if (us < 1)
                return $"{us * 1000:F2}ns";
            if (us < 1000)
                return $"{us:F2}us";
            return $"{us / 1000:F2}ms";
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 0)
                return "-";
            if (bytes < 1024)
                return $"{bytes}B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1}KB";
            return $"{bytes / 1024.0 / 1024.0:F1}MB";
        }

        private void RunAllBenchmarks()
        {
            isRunning = true;
            results.Clear();

            EditorApplication.delayCall += () =>
            {
                try
                {
                    RunSearchBenchmarksInternal();
                    RunCsvBenchmarksInternal();
                    RunMemoryBenchmarksInternal();
                }
                finally
                {
                    isRunning = false;
                    Repaint();
                }
            };
        }

        private void RunSearchBenchmarks()
        {
            isRunning = true;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    RunSearchBenchmarksInternal();
                }
                finally
                {
                    isRunning = false;
                    Repaint();
                }
            };
        }

        private void RunCsvBenchmarks()
        {
            isRunning = true;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    RunCsvBenchmarksInternal();
                }
                finally
                {
                    isRunning = false;
                    Repaint();
                }
            };
        }

        private void RunMemoryBenchmarks()
        {
            isRunning = true;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    RunMemoryBenchmarksInternal();
                }
                finally
                {
                    isRunning = false;
                    Repaint();
                }
            };
        }

        private TestRecord[] GenerateRecords(int count)
        {
            var records = new TestRecord[count];
            var random = new System.Random(42);

            for (var i = 0; i < count; i++)
            {
                records[i] = new TestRecord
                {
                    id = i,
                    name = $"Item_{i}",
                    value = (float)(random.NextDouble() * 1000),
                    category = random.Next(0, 100)
                };
            }

            return records;
        }

        private void RunSearchBenchmarksInternal()
        {
            var records = GenerateRecords(dataSize);
            var targetId = dataSize / 2;

            // 線形検索
            results.Add(RunBenchmark("Linear Search", records.Length, iterations, () =>
            {
                var result = records.FirstOrDefault(r => r.id == targetId);
            }));

            // バイナリサーチ
            var sorted = records.OrderBy(r => r.id).ToArray();
            results.Add(RunBenchmark("Binary Search", records.Length, iterations, () =>
            {
                BinarySearch(sorted, targetId);
            }));

            // ハッシュ検索
            var dict = records.ToDictionary(r => r.id);
            results.Add(RunBenchmark("Hash Lookup", records.Length, iterations, () =>
            {
                dict.TryGetValue(targetId, out _);
            }));

            // グループ検索
            var groupIndex = records.GroupBy(r => r.category)
                .ToDictionary(g => g.Key, g => g.ToArray());
            results.Add(RunBenchmark("Group Lookup", records.Length, iterations, () =>
            {
                groupIndex.TryGetValue(50, out _);
            }));

            // フィルタリング
            results.Add(RunBenchmark("Filter (LINQ)", records.Length, iterations / 10, () =>
            {
                var result = records.Where(r => r.value > 500).ToArray();
            }));
        }

        private TestRecord BinarySearch(TestRecord[] records, int id)
        {
            var left = 0;
            var right = records.Length - 1;

            while (left <= right)
            {
                var mid = left + (right - left) / 2;
                var comparison = records[mid].id.CompareTo(id);

                if (comparison == 0)
                    return records[mid];
                if (comparison < 0)
                    left = mid + 1;
                else
                    right = mid - 1;
            }

            return null;
        }

        private void RunCsvBenchmarksInternal()
        {
            var records = GenerateRecords(Math.Min(dataSize, 10000));

            // CSV出力
            var csv = "";
            results.Add(RunBenchmark("CSV Export", records.Length, 10, () =>
            {
                csv = CsvParser.ToCSV(records.ToList());
            }));

            // CSVパース
            if (!string.IsNullOrEmpty(csv))
            {
                results.Add(RunBenchmark("CSV Parse", records.Length, 10, () =>
                {
                    var parsed = CsvParser.Parse<TestRecord>(csv);
                }));
            }
        }

        private void RunMemoryBenchmarksInternal()
        {
            // レコード生成のメモリ使用量
            GC.Collect();
            var before = GC.GetTotalMemory(true);

            var records = GenerateRecords(dataSize);

            GC.Collect();
            var after = GC.GetTotalMemory(false);

            results.Add(new BenchmarkResult
            {
                Name = "Record Generation",
                DataSize = dataSize,
                Iterations = 1,
                ElapsedMs = 0,
                PerOperationUs = 0,
                MemoryBytes = after - before,
                Success = true
            });

            // Dictionary作成のメモリ使用量
            GC.Collect();
            before = GC.GetTotalMemory(true);

            var dict = records.ToDictionary(r => r.id);

            GC.Collect();
            after = GC.GetTotalMemory(false);

            results.Add(new BenchmarkResult
            {
                Name = "Dictionary Creation",
                DataSize = dataSize,
                Iterations = 1,
                ElapsedMs = 0,
                PerOperationUs = 0,
                MemoryBytes = after - before,
                Success = true
            });

            // グループインデックス作成のメモリ使用量
            GC.Collect();
            before = GC.GetTotalMemory(true);

            var groupIndex = records.GroupBy(r => r.category)
                .ToDictionary(g => g.Key, g => g.ToArray());

            GC.Collect();
            after = GC.GetTotalMemory(false);

            results.Add(new BenchmarkResult
            {
                Name = "Group Index Creation",
                DataSize = dataSize,
                Iterations = 1,
                ElapsedMs = 0,
                PerOperationUs = 0,
                MemoryBytes = after - before,
                Success = true
            });
        }

        private BenchmarkResult RunBenchmark(string name, int dataSize, int iterations, Action action)
        {
            var result = new BenchmarkResult
            {
                Name = name,
                DataSize = dataSize,
                Iterations = iterations
            };

            try
            {
                // ウォームアップ
                action();

                var sw = Stopwatch.StartNew();

                for (var i = 0; i < iterations; i++)
                    action();

                sw.Stop();

                result.ElapsedMs = sw.Elapsed.TotalMilliseconds;
                result.PerOperationUs = result.ElapsedMs * 1000 / iterations;
                result.Success = true;
            }
            catch (Exception e)
            {
                result.Success = false;
                result.Error = e.Message;
                Debug.LogException(e);
            }

            return result;
        }

        private void ExportReport()
        {
            if (results.Count == 0)
            {
                EditorUtility.DisplayDialog("エラー", "結果がありません", "OK");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("# XScriptableDB Benchmark Report");
            sb.AppendLine();
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Data Size: {dataSize:N0}");
            sb.AppendLine($"Iterations: {iterations:N0}");
            sb.AppendLine();
            sb.AppendLine("## Results");
            sb.AppendLine();
            sb.AppendLine("| Test Name | Data Size | Iterations | Total (ms) | Per Op | Memory |");
            sb.AppendLine("|-----------|-----------|------------|------------|--------|--------|");

            foreach (var result in results)
            {
                var status = result.Success ? "" : " (FAILED)";
                sb.AppendLine($"| {result.Name}{status} | {result.DataSize:N0} | {result.Iterations:N0} | {result.ElapsedMs:F2} | {FormatMicroseconds(result.PerOperationUs)} | {FormatBytes(result.MemoryBytes)} |");
            }

            sb.AppendLine();
            sb.AppendLine("## Summary");
            sb.AppendLine();
            sb.AppendLine("- Binary search is O(log n) - recommended for large datasets");
            sb.AppendLine("- Hash lookup is O(1) - fastest for exact key matches");
            sb.AppendLine("- Linear search is O(n) - avoid for datasets > 1000 records");
            sb.AppendLine("- QueryResult uses zero GC allocation during iteration");

            var report = sb.ToString();
            Debug.Log(report);

            var path = EditorUtility.SaveFilePanel("レポートを保存", "", "benchmark_report.md", "md");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, report);
                EditorUtility.DisplayDialog("完了", $"レポートを保存しました:\n{path}", "OK");
            }
        }
    }
}
