using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// テストデータ生成ウィンドウ。
    /// </summary>
    public class DataGeneratorWindow : EditorWindow
    {
        private List<ITableAsset> availableTables = new();
        private string[] tableNames = Array.Empty<string>();
        private int selectedTableIndex = -1;
        private ITableAsset selectedTable;

        private int generateCount = 100;
        private TableGeneratorConfig config;
        private Vector2 scrollPosition;
        private bool showAdvancedSettings;

        [MenuItem("Tools/XScriptableDB/Test Data Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<DataGeneratorWindow>("Test Data Generator");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshTableList();
        }

        private void RefreshTableList()
        {
            availableTables.Clear();

            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset is ITableAsset tableAsset)
                    availableTables.Add(tableAsset);
            }

            tableNames = availableTables.Select(t => t.GetType().Name).ToArray();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            DrawHeader();
            DrawTableSelection();

            if (selectedTable != null)
            {
                DrawGenerationSettings();
                DrawFieldConfigs();
                DrawActions();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("テーブル更新", EditorStyles.toolbarButton, GUILayout.Width(100)))
                RefreshTableList();

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTableSelection()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("対象テーブル:", GUILayout.Width(100));

            EditorGUI.BeginChangeCheck();
            selectedTableIndex = EditorGUILayout.Popup(selectedTableIndex, tableNames);
            if (EditorGUI.EndChangeCheck() && selectedTableIndex >= 0 && selectedTableIndex < availableTables.Count)
            {
                selectedTable = availableTables[selectedTableIndex];
                config = TestDataGenerator.CreateDefaultConfig(selectedTable);
            }

            EditorGUILayout.EndHorizontal();

            if (selectedTable != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("", GUILayout.Width(100));
                EditorGUILayout.LabelField($"現在のレコード数: {selectedTable.Count}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawGenerationSettings()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("生成設定", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("生成件数:", GUILayout.Width(100));
            generateCount = EditorGUILayout.IntField(generateCount, GUILayout.Width(100));
            generateCount = Mathf.Clamp(generateCount, 1, 100000);

            // クイック選択ボタン
            if (GUILayout.Button("10", GUILayout.Width(40)))
                generateCount = 10;
            if (GUILayout.Button("100", GUILayout.Width(40)))
                generateCount = 100;
            if (GUILayout.Button("1000", GUILayout.Width(40)))
                generateCount = 1000;
            if (GUILayout.Button("10000", GUILayout.Width(50)))
                generateCount = 10000;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "詳細設定");
        }

        private void DrawFieldConfigs()
        {
            if (!showAdvancedSettings || config == null)
                return;

            EditorGUILayout.Space(5);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            foreach (var fieldConfig in config.FieldConfigs)
            {
                DrawFieldConfig(fieldConfig);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawFieldConfig(FieldGeneratorConfig fieldConfig)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            EditorGUILayout.LabelField(fieldConfig.FieldName, GUILayout.Width(120));

            fieldConfig.Rule = (GeneratorRule)EditorGUILayout.EnumPopup(fieldConfig.Rule, GUILayout.Width(100));

            switch (fieldConfig.Rule)
            {
                case GeneratorRule.Sequential:
                    EditorGUILayout.LabelField("開始値:", GUILayout.Width(50));
                    fieldConfig.StartValue = EditorGUILayout.IntField(fieldConfig.StartValue, GUILayout.Width(60));
                    break;

                case GeneratorRule.RandomRange:
                    EditorGUILayout.LabelField("Min:", GUILayout.Width(30));
                    fieldConfig.MinValue = EditorGUILayout.TextField(fieldConfig.MinValue, GUILayout.Width(50));
                    EditorGUILayout.LabelField("Max:", GUILayout.Width(30));
                    fieldConfig.MaxValue = EditorGUILayout.TextField(fieldConfig.MaxValue, GUILayout.Width(50));
                    break;

                case GeneratorRule.Pattern:
                    EditorGUILayout.LabelField("Pattern:", GUILayout.Width(50));
                    fieldConfig.Pattern = EditorGUILayout.TextField(fieldConfig.Pattern, GUILayout.Width(100));
                    break;

                case GeneratorRule.Fixed:
                    EditorGUILayout.LabelField("値:", GUILayout.Width(30));
                    fieldConfig.FixedValue = EditorGUILayout.TextField(fieldConfig.FixedValue, GUILayout.Width(100));
                    break;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.6f, 0.8f, 0.6f);
            if (GUILayout.Button($"{generateCount}件 生成", GUILayout.Width(120), GUILayout.Height(30)))
            {
                GenerateData();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);

            GUI.backgroundColor = new Color(1f, 0.8f, 0.6f);
            if (GUILayout.Button("テーブルクリア", GUILayout.Width(100), GUILayout.Height(30)))
            {
                ClearTable();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            // プレビュー情報
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"生成後のレコード数: {selectedTable.Count + generateCount}");
            EditorGUILayout.LabelField($"推定メモリ使用量: 約 {EstimateMemoryUsage()} KB");
            EditorGUILayout.EndVertical();
        }

        private void GenerateData()
        {
            if (selectedTable == null)
                return;

            config.RecordCount = generateCount;

            var startTime = DateTime.Now;

            EditorUtility.DisplayProgressBar("データ生成", "テストデータを生成しています...", 0);

            try
            {
                var generated = TestDataGenerator.Generate(selectedTable, generateCount, config);

                var elapsed = DateTime.Now - startTime;
                EditorUtility.ClearProgressBar();

                EditorUtility.DisplayDialog("完了",
                    $"{generated}件のテストデータを生成しました\n処理時間: {elapsed.TotalSeconds:F2}秒",
                    "OK");

                AssetDatabase.SaveAssets();
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("エラー", $"データ生成に失敗しました: {ex.Message}", "OK");
            }
        }

        private void ClearTable()
        {
            if (selectedTable == null)
                return;

            if (!EditorUtility.DisplayDialog("確認",
                $"テーブル '{selectedTable.GetType().Name}' の全レコード ({selectedTable.Count}件) を削除しますか？",
                "削除", "キャンセル"))
            {
                return;
            }

            TestDataGenerator.ClearTable(selectedTable);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("完了", "テーブルをクリアしました", "OK");
        }

        private string EstimateMemoryUsage()
        {
            if (selectedTable == null)
                return "0";

            var fields = ReflectionUtility.GetSerializableFields(selectedTable.RecordType);
            var estimatedRecordSize = 0;

            foreach (var field in fields)
            {
                estimatedRecordSize += GetEstimatedFieldSize(field.FieldType);
            }

            var totalBytes = (selectedTable.Count + generateCount) * estimatedRecordSize;
            return (totalBytes / 1024f).ToString("F1");
        }

        private int GetEstimatedFieldSize(Type type)
        {
            if (type == typeof(int) || type == typeof(float))
                return 4;
            if (type == typeof(long) || type == typeof(double))
                return 8;
            if (type == typeof(bool))
                return 1;
            if (type == typeof(string))
                return 50; // 平均的な文字列サイズを仮定
            if (type.IsEnum)
                return 4;
            return 8; // デフォルト
        }
    }
}
