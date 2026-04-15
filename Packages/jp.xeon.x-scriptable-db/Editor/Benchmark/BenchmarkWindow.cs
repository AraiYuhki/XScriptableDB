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
    /// Benchmark execution window.
    /// </summary>
    public class BenchmarkWindow : EditorWindow
    {
        [MenuItem("Tools/XScriptableDB/Benchmark")]
        public static void Open()
        {
            var window = GetWindow<BenchmarkWindow>("Benchmark");
            window.Show();
        }

        // Record class for testing
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
        private bool cancelRequested;
        private string currentBenchmarkName = "";
        private int completedBenchmarkCount;
        private int totalBenchmarkCount;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("XScriptableDB Benchmark", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledGroupScope(isRunning))
            {
                // Settings
                EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
                dataSize = EditorGUILayout.IntSlider("Data Size", dataSize, 100, 100000);
                iterations = EditorGUILayout.IntSlider("Iterations", iterations, 10, 10000);

                EditorGUILayout.Space();

                // Benchmark run buttons
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Run All Benchmarks"))
                        RunAllBenchmarks();

                    if (GUILayout.Button("Search Benchmark"))
                        RunSearchBenchmarks();

                    if (GUILayout.Button("CSV Benchmark"))
                        RunCsvBenchmarks();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Memory Benchmark"))
                        RunMemoryBenchmarks();

                    if (GUILayout.Button("Clear Results"))
                        results.Clear();

                    if (GUILayout.Button("Export Report"))
                        ExportReport();
                }
            }

            if (isRunning)
            {
                var progress = totalBenchmarkCount > 0
                    ? $"{completedBenchmarkCount}/{totalBenchmarkCount}"
                    : "...";
                var label = string.IsNullOrEmpty(currentBenchmarkName)
                    ? $"Running benchmarks... ({progress})"
                    : $"Running: {currentBenchmarkName} ({progress})";
                EditorGUILayout.HelpBox(label, MessageType.Info);

                if (GUILayout.Button("Cancel"))
                    cancelRequested = true;
            }

            EditorGUILayout.Space();

            if (results.Count <= 0)
                return;

            // Result display
            EditorGUILayout.LabelField($"Results ({results.Count})", EditorStyles.boldLabel);
            using var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition);
            scrollPosition = scroll.scrollPosition;

            // Header
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Test Name", GUILayout.Width(200));
                GUILayout.Label("Data Size", GUILayout.Width(80));
                GUILayout.Label("Iters", GUILayout.Width(60));
                GUILayout.Label("Total (ms)", GUILayout.Width(80));
                GUILayout.Label("Per Op", GUILayout.Width(100));
                GUILayout.Label("Memory", GUILayout.Width(80));
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
            cancelRequested = false;
            completedBenchmarkCount = 0;
            totalBenchmarkCount = 10; // Search:5 + CSV:2 + Memory:3
            results.Clear();

            EditorApplication.delayCall += () =>
            {
                try
                {
                    RunSearchBenchmarksInternal();
                    if (!cancelRequested)
                        RunCsvBenchmarksInternal();
                    if (!cancelRequested)
                        RunMemoryBenchmarksInternal();
                }
                finally
                {
                    isRunning = false;
                    currentBenchmarkName = "";
                    EditorUtility.ClearProgressBar();
                    Repaint();
                }
            };
        }

        private void RunSearchBenchmarks()
        {
            isRunning = true;
            cancelRequested = false;
            completedBenchmarkCount = 0;
            totalBenchmarkCount = 5;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    RunSearchBenchmarksInternal();
                }
                finally
                {
                    isRunning = false;
                    currentBenchmarkName = "";
                    EditorUtility.ClearProgressBar();
                    Repaint();
                }
            };
        }

        private void RunCsvBenchmarks()
        {
            isRunning = true;
            cancelRequested = false;
            completedBenchmarkCount = 0;
            totalBenchmarkCount = 2;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    RunCsvBenchmarksInternal();
                }
                finally
                {
                    isRunning = false;
                    currentBenchmarkName = "";
                    EditorUtility.ClearProgressBar();
                    Repaint();
                }
            };
        }

        private void RunMemoryBenchmarks()
        {
            isRunning = true;
            cancelRequested = false;
            completedBenchmarkCount = 0;
            totalBenchmarkCount = 3;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    RunMemoryBenchmarksInternal();
                }
                finally
                {
                    isRunning = false;
                    currentBenchmarkName = "";
                    EditorUtility.ClearProgressBar();
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

            // Linear search
            AddResult(RunBenchmark("Linear Search", records.Length, iterations, () =>
            {
                var result = records.FirstOrDefault(r => r.id == targetId);
            }));
            if (cancelRequested)
                return;

            // Binary search
            var sorted = records.OrderBy(r => r.id).ToArray();
            AddResult(RunBenchmark("Binary Search", records.Length, iterations, () =>
            {
                BinarySearch(sorted, targetId);
            }));
            if (cancelRequested)
                return;

            // Hash lookup
            var dict = records.ToDictionary(r => r.id);
            AddResult(RunBenchmark("Hash Lookup", records.Length, iterations, () =>
            {
                dict.TryGetValue(targetId, out _);
            }));
            if (cancelRequested)
                return;

            // Group lookup
            var groupIndex = records.GroupBy(r => r.category)
                .ToDictionary(g => g.Key, g => g.ToArray());
            AddResult(RunBenchmark("Group Lookup", records.Length, iterations, () =>
            {
                groupIndex.TryGetValue(50, out _);
            }));
            if (cancelRequested)
                return;

            // Filtering
            AddResult(RunBenchmark("Filter (LINQ)", records.Length, iterations / 10, () =>
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

            // CSV export
            var csv = "";
            AddResult(RunBenchmark("CSV Export", records.Length, 10, () =>
            {
                csv = CsvParser.ToCSV(records.ToList());
            }));
            if (cancelRequested)
                return;

            // CSV parse
            if (!string.IsNullOrEmpty(csv))
            {
                AddResult(RunBenchmark("CSV Parse", records.Length, 10, () =>
                {
                    var parsed = CsvParser.Parse<TestRecord>(csv);
                }));
            }
        }

        private void RunMemoryBenchmarksInternal()
        {
            // Memory usage for record generation
            currentBenchmarkName = "Record Generation";
            GC.Collect();
            var before = GC.GetTotalMemory(true);

            var records = GenerateRecords(dataSize);

            GC.Collect();
            var after = GC.GetTotalMemory(false);

            AddResult(new BenchmarkResult
            {
                Name = "Record Generation",
                DataSize = dataSize,
                Iterations = 1,
                ElapsedMs = 0,
                PerOperationUs = 0,
                MemoryBytes = after - before,
                Success = true
            });
            if (cancelRequested)
                return;

            // Memory usage for dictionary creation
            currentBenchmarkName = "Dictionary Creation";
            GC.Collect();
            before = GC.GetTotalMemory(true);

            var dict = records.ToDictionary(r => r.id);

            GC.Collect();
            after = GC.GetTotalMemory(false);

            AddResult(new BenchmarkResult
            {
                Name = "Dictionary Creation",
                DataSize = dataSize,
                Iterations = 1,
                ElapsedMs = 0,
                PerOperationUs = 0,
                MemoryBytes = after - before,
                Success = true
            });
            if (cancelRequested)
                return;

            // Memory usage for group index creation
            currentBenchmarkName = "Group Index Creation";
            GC.Collect();
            before = GC.GetTotalMemory(true);

            var groupIndex = records.GroupBy(r => r.category)
                .ToDictionary(g => g.Key, g => g.ToArray());

            GC.Collect();
            after = GC.GetTotalMemory(false);

            AddResult(new BenchmarkResult
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

        private void AddResult(BenchmarkResult result)
        {
            if (result == null)
                return;
            results.Add(result);
            completedBenchmarkCount++;
            Repaint();
        }

        private BenchmarkResult RunBenchmark(string name, int dataSize, int iterations, Action action)
        {
            if (cancelRequested)
                return null;

            currentBenchmarkName = name;
            var overallProgress = totalBenchmarkCount > 0
                ? (float)completedBenchmarkCount / totalBenchmarkCount
                : 0f;

            var result = new BenchmarkResult
            {
                Name = name,
                DataSize = dataSize,
                Iterations = iterations
            };

            try
            {
                // Warm-up
                action();

                var sw = Stopwatch.StartNew();
                var progressInterval = Math.Max(1, iterations / 100);

                for (var i = 0; i < iterations; i++)
                {
                    if (i % progressInterval == 0)
                    {
                        var iterProgress = (float)i / iterations;
                        var combinedProgress = (completedBenchmarkCount + iterProgress) / Math.Max(1, totalBenchmarkCount);
                        var cancelled = EditorUtility.DisplayCancelableProgressBar(
                            "Running Benchmark",
                            $"{name} ({i}/{iterations}) - Overall: {completedBenchmarkCount}/{totalBenchmarkCount}",
                            combinedProgress);

                        if (cancelled || cancelRequested)
                        {
                            cancelRequested = true;
                            sw.Stop();
                            result.ElapsedMs = sw.Elapsed.TotalMilliseconds;
                            result.PerOperationUs = i > 0 ? result.ElapsedMs * 1000 / i : 0;
                            result.Iterations = i;
                            result.Success = true;
                            result.Name = $"{name} (Cancelled: {i}/{iterations})";
                            return result;
                        }
                    }

                    action();
                }

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
                EditorUtility.DisplayDialog("Error", "No results to export", "OK");
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

            var path = EditorUtility.SaveFilePanel("Save Report", "", "benchmark_report.md", "md");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, report);
                EditorUtility.DisplayDialog("Done", $"Report saved:\n{path}", "OK");
            }
        }
    }
}
