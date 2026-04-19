using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Xeon.XScriptableDB.Cache;
using Xeon.XScriptableDB.Performance;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// パフォーマンスモニタリングウィンドウ。
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

        [MenuItem("Tools/XScriptableDB/Performance")]
        public static void ShowWindow()
        {
            var window = GetWindow<PerformanceWindow>();
            window.titleContent = new GUIContent("パフォーマンス");
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
            if (GUILayout.Toggle(currentTab == Tab.Cache, "キャッシュ", EditorStyles.toolbarButton))
                currentTab = Tab.Cache;
            if (GUILayout.Toggle(currentTab == Tab.Memory, "メモリ", EditorStyles.toolbarButton))
                currentTab = Tab.Memory;
            if (GUILayout.Toggle(currentTab == Tab.Queries, "クエリ", EditorStyles.toolbarButton))
                currentTab = Tab.Queries;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("更新", EditorStyles.toolbarButton))
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
            EditorGUILayout.LabelField("クエリキャッシュ", headerStyle);
            EditorGUILayout.Space(5);

            // キャッシュ設定
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("キャッシュ有効:");
            var newEnabled = EditorGUILayout.Toggle(CacheManager.IsEnabled);
            if (newEnabled != CacheManager.IsEnabled)
            {
                CacheManager.IsEnabled = newEnabled;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 統計情報
            var stats = CacheManager.GetStatistics();

            DrawStatRow("容量:", stats.Capacity.ToString());
            DrawStatRow("エントリ数:", stats.Count.ToString());
            DrawStatRow("ヒット数:", stats.HitCount.ToString());
            DrawStatRow("ミス数:", stats.MissCount.ToString());
            DrawStatRow("ヒット率:", $"{stats.HitRate:P1}");

            EditorGUILayout.Space(10);

            // アクション
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("キャッシュをクリア"))
            {
                CacheManager.Clear();
            }
            if (GUILayout.Button("統計をリセット"))
            {
                CacheManager.ResetStatistics();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMemoryTab()
        {
            EditorGUILayout.LabelField("メモリ使用量", headerStyle);
            EditorGUILayout.Space(5);

            // システムメモリ
            var snapshot = MemoryProfiler.TakeSnapshot();
            DrawStatRow("合計メモリ:", FormatBytes(snapshot.TotalMemory));
            DrawStatRow("GC Gen 0:", snapshot.GCCollectionCount0.ToString());
            DrawStatRow("GC Gen 1:", snapshot.GCCollectionCount1.ToString());
            DrawStatRow("GC Gen 2:", snapshot.GCCollectionCount2.ToString());

            EditorGUILayout.Space(10);

            // テーブルごとのメモリ
            EditorGUILayout.LabelField("テーブルメモリ (推定)", EditorStyles.boldLabel);

            if (memoryInfos.Count == 0)
            {
                EditorGUILayout.HelpBox("テーブルが見つかりません。『更新』をクリックしてスキャンしてください。", MessageType.Info);
            }
            else
            {
                long totalSize = 0;

                foreach (var info in memoryInfos)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(info.TableName);
                    EditorGUILayout.LabelField($"{info.RecordCount} 件のレコード", GUILayout.Width(100));
                    EditorGUILayout.LabelField(FormatBytes(info.EstimatedTotalSize), valueStyle, GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();

                    totalSize += info.EstimatedTotalSize;
                }

                EditorGUILayout.Space(5);
                DrawStatRow("合計推定サイズ:", FormatBytes(totalSize));
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("GCを強制実行"))
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
            }
        }

        private void DrawQueriesTab()
        {
            EditorGUILayout.LabelField("クエリプロファイラ", headerStyle);
            EditorGUILayout.Space(5);

            // 統計情報
            var stats = profiler.GetStatistics();
            DrawStatRow("クエリ数:", stats.QueryCount.ToString());
            DrawStatRow("合計時間:", $"{stats.TotalMilliseconds:F2} ms");
            DrawStatRow("平均時間:", $"{stats.AverageMilliseconds:F3} ms");
            DrawStatRow("最小時間:", $"{stats.MinMilliseconds:F3} ms");
            DrawStatRow("最大時間:", $"{stats.MaxMilliseconds:F3} ms");
            DrawStatRow("キャッシュヒット:", stats.CachedQueryCount.ToString());

            EditorGUILayout.Space(10);

            // クエリ履歴
            EditorGUILayout.LabelField("最近のクエリ", EditorStyles.boldLabel);

            var profiles = profiler.GetProfiles();
            if (profiles.Count == 0)
            {
                EditorGUILayout.HelpBox("クエリ履歴がありません。", MessageType.Info);
            }
            else
            {
                // 最新10件を表示します
                var recent = profiles.Reverse().Take(10);
                foreach (var profile in recent)
                {
                    var cached = profile.WasCached ? " [キャッシュ済み]" : "";
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"{profile.QueryName}{cached}");
                    EditorGUILayout.LabelField($"{profile.ResultCount} 件の結果", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{profile.ElapsedMilliseconds:F3} ms", valueStyle, GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("履歴をクリア"))
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
