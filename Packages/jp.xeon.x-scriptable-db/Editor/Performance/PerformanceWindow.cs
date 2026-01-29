using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Xeon.XScriptableDB.Cache;
using Xeon.XScriptableDB.Performance;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// パフォーマンス監視ウィンドウ。
    /// </summary>
    public class PerformanceWindow : EditorWindow
    {
        private enum Tab
        {
            Cache,
            Memory,
            Queries
        }

        private Tab currentTab = Tab.Cache;
        private Vector2 scrollPosition;
        private QueryProfiler profiler;
        private List<TableMemoryInfo> memoryInfos = new();

        private GUIStyle headerStyle;
        private GUIStyle valueStyle;
        private bool stylesInitialized;

        [MenuItem("Window/XScriptableDB/Performance")]
        public static void ShowWindow()
        {
            var window = GetWindow<PerformanceWindow>();
            window.titleContent = new GUIContent("Performance");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            profiler = new QueryProfiler();
            RefreshMemoryInfo();
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14
            };

            valueStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            // タブ
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Toggle(currentTab == Tab.Cache, "Cache", EditorStyles.toolbarButton))
                currentTab = Tab.Cache;
            if (GUILayout.Toggle(currentTab == Tab.Memory, "Memory", EditorStyles.toolbarButton))
                currentTab = Tab.Memory;
            if (GUILayout.Toggle(currentTab == Tab.Queries, "Queries", EditorStyles.toolbarButton))
                currentTab = Tab.Queries;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                Refresh();
            }
            EditorGUILayout.EndHorizontal();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (currentTab)
            {
                case Tab.Cache:
                    DrawCacheTab();
                    break;
                case Tab.Memory:
                    DrawMemoryTab();
                    break;
                case Tab.Queries:
                    DrawQueriesTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCacheTab()
        {
            EditorGUILayout.LabelField("Query Cache", headerStyle);
            EditorGUILayout.Space(5);

            // キャッシュ設定
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cache Enabled:");
            var newEnabled = EditorGUILayout.Toggle(CacheManager.IsEnabled);
            if (newEnabled != CacheManager.IsEnabled)
            {
                CacheManager.IsEnabled = newEnabled;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 統計
            var stats = CacheManager.GetStatistics();

            DrawStatRow("Capacity:", stats.Capacity.ToString());
            DrawStatRow("Entries:", stats.Count.ToString());
            DrawStatRow("Hit Count:", stats.HitCount.ToString());
            DrawStatRow("Miss Count:", stats.MissCount.ToString());
            DrawStatRow("Hit Rate:", $"{stats.HitRate:P1}");

            EditorGUILayout.Space(10);

            // アクション
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Cache"))
            {
                CacheManager.Clear();
            }
            if (GUILayout.Button("Reset Statistics"))
            {
                CacheManager.ResetStatistics();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMemoryTab()
        {
            EditorGUILayout.LabelField("Memory Usage", headerStyle);
            EditorGUILayout.Space(5);

            // システムメモリ
            var snapshot = MemoryProfiler.TakeSnapshot();
            DrawStatRow("Total Memory:", FormatBytes(snapshot.TotalMemory));
            DrawStatRow("GC Gen 0:", snapshot.GCCollectionCount0.ToString());
            DrawStatRow("GC Gen 1:", snapshot.GCCollectionCount1.ToString());
            DrawStatRow("GC Gen 2:", snapshot.GCCollectionCount2.ToString());

            EditorGUILayout.Space(10);

            // テーブル別メモリ
            EditorGUILayout.LabelField("Table Memory (Estimated)", EditorStyles.boldLabel);

            if (memoryInfos.Count == 0)
            {
                EditorGUILayout.HelpBox("No tables found. Click 'Refresh' to scan.", MessageType.Info);
            }
            else
            {
                long totalSize = 0;

                foreach (var info in memoryInfos)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(info.TableName);
                    EditorGUILayout.LabelField($"{info.RecordCount} records", GUILayout.Width(100));
                    EditorGUILayout.LabelField(FormatBytes(info.EstimatedTotalSize), valueStyle, GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();

                    totalSize += info.EstimatedTotalSize;
                }

                EditorGUILayout.Space(5);
                DrawStatRow("Total Estimated:", FormatBytes(totalSize));
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Force GC"))
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
            }
        }

        private void DrawQueriesTab()
        {
            EditorGUILayout.LabelField("Query Profiler", headerStyle);
            EditorGUILayout.Space(5);

            // 統計
            var stats = profiler.GetStatistics();
            DrawStatRow("Query Count:", stats.QueryCount.ToString());
            DrawStatRow("Total Time:", $"{stats.TotalMilliseconds:F2} ms");
            DrawStatRow("Average Time:", $"{stats.AverageMilliseconds:F3} ms");
            DrawStatRow("Min Time:", $"{stats.MinMilliseconds:F3} ms");
            DrawStatRow("Max Time:", $"{stats.MaxMilliseconds:F3} ms");
            DrawStatRow("Cached Queries:", stats.CachedQueryCount.ToString());

            EditorGUILayout.Space(10);

            // クエリ履歴
            EditorGUILayout.LabelField("Recent Queries", EditorStyles.boldLabel);

            var profiles = profiler.GetProfiles();
            if (profiles.Count == 0)
            {
                EditorGUILayout.HelpBox("No queries recorded yet.", MessageType.Info);
            }
            else
            {
                // 最新の10件を表示
                var recent = profiles.Reverse().Take(10);
                foreach (var profile in recent)
                {
                    var cached = profile.WasCached ? " [cached]" : "";
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"{profile.QueryName}{cached}");
                    EditorGUILayout.LabelField($"{profile.ResultCount} results", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{profile.ElapsedMilliseconds:F3} ms", valueStyle, GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Clear History"))
            {
                profiler.Clear();
            }
        }

        private void DrawStatRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(120));
            EditorGUILayout.LabelField(value, valueStyle);
            EditorGUILayout.EndHorizontal();
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        }

        private void Refresh()
        {
            RefreshMemoryInfo();
            Repaint();
        }

        private void RefreshMemoryInfo()
        {
            memoryInfos.Clear();

            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (asset is ITableAsset tableAsset)
                {
                    var info = MemoryProfiler.EstimateMemoryUsage(tableAsset);
                    memoryInfos.Add(info);
                }
            }

            memoryInfos = memoryInfos.OrderByDescending(m => m.EstimatedTotalSize).ToList();
        }
    }
}
