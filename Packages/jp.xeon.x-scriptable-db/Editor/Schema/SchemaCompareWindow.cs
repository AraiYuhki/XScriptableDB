using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// スキーマ比較ウィンドウ。
    /// </summary>
    public class SchemaCompareWindow : EditorWindow
    {
        private ITableAsset sourceTable;
        private ITableAsset targetTable;
        private SchemaComparisonResult comparisonResult;
        private Vector2 scrollPosition;
        private Vector2 sourceScrollPosition;
        private Vector2 targetScrollPosition;

        private List<ITableAsset> availableTables = new();
        private string[] tableNames = Array.Empty<string>();
        private int sourceTableIndex = -1;
        private int targetTableIndex = -1;

        private bool showAddedFields = true;
        private bool showRemovedFields = true;
        private bool showChangedFields = true;
        private bool showKeyChanges = true;

        [MenuItem("Tools/XScriptableDB/スキーマ比較")]
        public static void ShowWindow()
        {
            var window = GetWindow<SchemaCompareWindow>("スキーマ比較");
            window.minSize = new Vector2(800, 500);
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

            DrawToolbar();
            DrawTableSelection();

            if (comparisonResult != null)
            {
                DrawComparisonResult();
            }
            else
            {
                DrawSchemaViews();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("テーブル更新", EditorStyles.toolbarButton, GUILayout.Width(100)))
                RefreshTableList();

            GUILayout.FlexibleSpace();

            if (sourceTable != null && targetTable != null)
            {
                if (GUILayout.Button("比較実行", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    ExecuteComparison();

                if (comparisonResult != null)
                {
                    if (GUILayout.Button("結果クリア", EditorStyles.toolbarButton, GUILayout.Width(80)))
                        comparisonResult = null;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTableSelection()
        {
            EditorGUILayout.BeginHorizontal();

            // ソーステーブル選択
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 10));
            EditorGUILayout.LabelField("比較元 (Source)", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            sourceTableIndex = EditorGUILayout.Popup(sourceTableIndex, tableNames);
            if (EditorGUI.EndChangeCheck() && sourceTableIndex >= 0 && sourceTableIndex < availableTables.Count)
            {
                sourceTable = availableTables[sourceTableIndex];
                comparisonResult = null;
            }

            EditorGUILayout.EndVertical();

            // ターゲットテーブル選択
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 10));
            EditorGUILayout.LabelField("比較先 (Target)", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            targetTableIndex = EditorGUILayout.Popup(targetTableIndex, tableNames);
            if (EditorGUI.EndChangeCheck() && targetTableIndex >= 0 && targetTableIndex < availableTables.Count)
            {
                targetTable = availableTables[targetTableIndex];
                comparisonResult = null;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
        }

        private void DrawSchemaViews()
        {
            EditorGUILayout.BeginHorizontal();

            // ソーススキーマ表示
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 10));
            if (sourceTable != null)
            {
                EditorGUILayout.LabelField($"スキーマ: {sourceTable.RecordType.Name}", EditorStyles.boldLabel);
                sourceScrollPosition = EditorGUILayout.BeginScrollView(sourceScrollPosition, GUILayout.Height(300));
                DrawSchemaInfo(sourceTable.RecordType);
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("比較元テーブルを選択してください", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            // ターゲットスキーマ表示
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 10));
            if (targetTable != null)
            {
                EditorGUILayout.LabelField($"スキーマ: {targetTable.RecordType.Name}", EditorStyles.boldLabel);
                targetScrollPosition = EditorGUILayout.BeginScrollView(targetScrollPosition, GUILayout.Height(300));
                DrawSchemaInfo(targetTable.RecordType);
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("比較先テーブルを選択してください", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSchemaInfo(Type type)
        {
            var fields = SchemaComparer.GetFieldSchemas(type);

            foreach (var field in fields.Values.OrderBy(f => f.Name))
            {
                EditorGUILayout.BeginHorizontal();

                var icon = GetFieldIcon(field);
                GUILayout.Label(icon, GUILayout.Width(20));

                EditorGUILayout.LabelField(field.Name, GUILayout.Width(150));
                EditorGUILayout.LabelField(field.FieldType.Name, GUILayout.Width(100));

                var attrs = new List<string>();
                if (field.IsPrimaryKey)
                    attrs.Add("PK");
                if (field.IsSecondaryKey)
                    attrs.Add("SK");
                if (field.IsReadOnly)
                    attrs.Add("RO");

                EditorGUILayout.LabelField(string.Join(", ", attrs));

                EditorGUILayout.EndHorizontal();
            }
        }

        private GUIContent GetFieldIcon(FieldSchemaInfo field)
        {
            if (field.IsPrimaryKey)
                return EditorGUIUtility.IconContent("d_FilterByLabel");
            if (field.IsSecondaryKey)
                return EditorGUIUtility.IconContent("d_FilterByType");
            return EditorGUIUtility.IconContent("d_TextAsset Icon");
        }

        private void ExecuteComparison()
        {
            if (sourceTable == null || targetTable == null)
                return;

            comparisonResult = SchemaComparer.Compare(sourceTable, targetTable);
        }

        private void DrawComparisonResult()
        {
            EditorGUILayout.Space(10);

            // サマリー
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.BeginVertical();

            var statusIcon = comparisonResult.IsCompatible
                ? EditorGUIUtility.IconContent("d_greenLight")
                : EditorGUIUtility.IconContent("d_redLight");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(statusIcon, GUILayout.Width(20));
            EditorGUILayout.LabelField(
                comparisonResult.IsCompatible ? "互換性あり" : "互換性に問題あり",
                EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(comparisonResult.CompatibilityNote))
                EditorGUILayout.LabelField(comparisonResult.CompatibilityNote, EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"追加: {comparisonResult.AddedFieldCount}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"削除: {comparisonResult.RemovedFieldCount}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"変更: {comparisonResult.ChangedFieldCount}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"合計: {comparisonResult.Differences.Count}", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            // フィルター
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            showAddedFields = GUILayout.Toggle(showAddedFields, "追加", EditorStyles.toolbarButton);
            showRemovedFields = GUILayout.Toggle(showRemovedFields, "削除", EditorStyles.toolbarButton);
            showChangedFields = GUILayout.Toggle(showChangedFields, "変更", EditorStyles.toolbarButton);
            showKeyChanges = GUILayout.Toggle(showKeyChanges, "キー変更", EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // 差分リスト
            EditorGUILayout.Space(5);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var diff in comparisonResult.Differences)
            {
                if (!ShouldShowDifference(diff))
                    continue;

                DrawDifferenceItem(diff);
            }

            if (comparisonResult.Differences.Count == 0)
            {
                EditorGUILayout.HelpBox("差分はありません", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private bool ShouldShowDifference(SchemaDifference diff)
        {
            return diff.Type switch
            {
                SchemaDifferenceType.FieldAdded => showAddedFields,
                SchemaDifferenceType.FieldRemoved => showRemovedFields,
                SchemaDifferenceType.FieldTypeChanged => showChangedFields,
                SchemaDifferenceType.FieldAttributeChanged => showChangedFields,
                SchemaDifferenceType.PrimaryKeyChanged => showKeyChanges,
                SchemaDifferenceType.SecondaryKeyAdded => showKeyChanges,
                SchemaDifferenceType.SecondaryKeyRemoved => showKeyChanges,
                _ => true
            };
        }

        private void DrawDifferenceItem(SchemaDifference diff)
        {
            var bgColor = diff.Type switch
            {
                SchemaDifferenceType.FieldAdded => new Color(0.2f, 0.6f, 0.2f, 0.3f),
                SchemaDifferenceType.FieldRemoved => new Color(0.6f, 0.2f, 0.2f, 0.3f),
                SchemaDifferenceType.FieldTypeChanged => new Color(0.6f, 0.6f, 0.2f, 0.3f),
                _ => new Color(0.4f, 0.4f, 0.6f, 0.3f)
            };

            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUI.backgroundColor = oldColor;

            var icon = diff.Type switch
            {
                SchemaDifferenceType.FieldAdded => EditorGUIUtility.IconContent("d_Toolbar Plus"),
                SchemaDifferenceType.FieldRemoved => EditorGUIUtility.IconContent("d_Toolbar Minus"),
                _ => EditorGUIUtility.IconContent("d_console.warnicon.sml")
            };

            GUILayout.Label(icon, GUILayout.Width(20));
            EditorGUILayout.LabelField(diff.ToString());

            EditorGUILayout.EndHorizontal();
        }
    }
}
