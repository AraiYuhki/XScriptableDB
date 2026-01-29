using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// XTableAssetを編集するためのEditorWindow。
    /// </summary>
    public class XTableEditorWindow : EditorWindow
    {
        [MenuItem("Tools/XScriptableDB/テーブルエディタ")]
        public static void Open()
        {
            var window = GetWindow<XTableEditorWindow>();
            window.titleContent = new GUIContent("XTable Editor");
            window.Show();
        }

        private ScriptableObject selectedTable;
        private SerializedObject serializedTable;
        private SerializedProperty recordsProperty;

        private Vector2 tableListScrollPosition;
        private Vector2 recordListScrollPosition;
        private int selectedRecordIndex = -1;

        private List<ScriptableObject> allTables = new();
        private string searchFilter = "";

        private GUIStyle headerStyle;
        private GUIStyle warningStyle;

        private void OnEnable()
        {
            RefreshTableList();
        }

        private void OnGUI()
        {
            InitializeStyles();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawTableListPanel();
                DrawRecordListPanel();
            }
        }

        private void InitializeStyles()
        {
            headerStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(4, 4, 8, 8)
            };

            warningStyle ??= new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.yellow }
            };
        }

        private void DrawTableListPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(250)))
            {
                EditorGUILayout.LabelField("テーブル一覧", headerStyle);

                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField);
                    if (GUILayout.Button("更新", EditorStyles.toolbarButton, GUILayout.Width(40)))
                        RefreshTableList();
                }

                using (var scroll = new EditorGUILayout.ScrollViewScope(tableListScrollPosition))
                {
                    tableListScrollPosition = scroll.scrollPosition;
                    DrawTableList();
                }
            }
        }

        private void DrawTableList()
        {
            var filteredTables = string.IsNullOrEmpty(searchFilter)
                ? allTables
                : allTables.Where(t => t.name.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            foreach (var table in filteredTables)
            {
                var isSelected = table == selectedTable;
                var style = isSelected ? EditorStyles.selectionRect : EditorStyles.label;

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(table.name, style))
                        SelectTable(table);

                    if (GUILayout.Button("選択", GUILayout.Width(40)))
                    {
                        Selection.activeObject = table;
                        EditorGUIUtility.PingObject(table);
                    }
                }
            }

            if (filteredTables.Count == 0)
                EditorGUILayout.HelpBox("テーブルが見つかりません", MessageType.Info);
        }

        private void DrawRecordListPanel()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (selectedTable == null)
                {
                    EditorGUILayout.HelpBox("左のリストからテーブルを選択してください", MessageType.Info);
                    return;
                }

                DrawRecordHeader();
                DrawDuplicateKeyWarning();
                DrawRecordToolbar();
                DrawRecordList();
            }
        }

        private void DrawRecordHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{selectedTable.name}", headerStyle);

                var xTable = selectedTable as IXTableAsset;
                if (xTable != null)
                {
                    EditorGUILayout.LabelField($"レコード数: {xTable.Count}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"Key: {xTable.KeyType.Name}", GUILayout.Width(100));
                }
            }
        }

        private void DrawDuplicateKeyWarning()
        {
            var xTable = selectedTable as IXTableAsset;
            if (xTable == null)
                return;

            var duplicates = xTable.FindDuplicateKeysAsObjects();
            if (duplicates == null || duplicates.Count == 0)
                return;

            var keysString = string.Join(", ", duplicates.Cast<object>().Take(5));
            if (duplicates.Count > 5)
                keysString += $"... 他{duplicates.Count - 5}件";

            EditorGUILayout.HelpBox($"重複したPrimaryKeyがあります: {keysString}", MessageType.Warning);
        }

        private void DrawRecordToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("追加", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    AddNewRecord();

                EditorGUI.BeginDisabledGroup(selectedRecordIndex < 0);
                if (GUILayout.Button("削除", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    DeleteSelectedRecord();
                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("ソート", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    SortRecords();

                if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    SaveTable();
            }
        }

        private void DrawRecordList()
        {
            if (serializedTable == null || recordsProperty == null)
                return;

            serializedTable.Update();

            using (var scroll = new EditorGUILayout.ScrollViewScope(recordListScrollPosition))
            {
                recordListScrollPosition = scroll.scrollPosition;

                for (var i = 0; i < recordsProperty.arraySize; i++)
                {
                    var isSelected = i == selectedRecordIndex;
                    var element = recordsProperty.GetArrayElementAtIndex(i);

                    using (new EditorGUILayout.VerticalScope(isSelected ? "SelectionRect" : "Box"))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(40));
                            if (GUILayout.Button(isSelected ? "▼" : "▶", GUILayout.Width(25)))
                                selectedRecordIndex = isSelected ? -1 : i;

                            DrawRecordSummary(element);
                        }

                        if (isSelected)
                            EditorGUILayout.PropertyField(element, GUIContent.none, true);
                    }
                }
            }

            serializedTable.ApplyModifiedProperties();
        }

        private void DrawRecordSummary(SerializedProperty element)
        {
            var iterator = element.Copy();
            var enterChildren = true;
            var depth = iterator.depth;
            var count = 0;
            const int maxFields = 3;

            while (iterator.NextVisible(enterChildren) && iterator.depth > depth && count < maxFields)
            {
                enterChildren = false;
                var value = GetPropertyValueString(iterator);
                EditorGUILayout.LabelField($"{iterator.name}: {value}", GUILayout.Width(150));
                count++;
            }
        }

        private string GetPropertyValueString(SerializedProperty property)
        {
            return property.propertyType switch
            {
                SerializedPropertyType.Integer => property.intValue.ToString(),
                SerializedPropertyType.Float => property.floatValue.ToString("F2"),
                SerializedPropertyType.String => property.stringValue.Length > 15
                    ? property.stringValue.Substring(0, 15) + "..."
                    : property.stringValue,
                SerializedPropertyType.Boolean => property.boolValue.ToString(),
                SerializedPropertyType.Enum => property.enumDisplayNames[property.enumValueIndex],
                _ => "..."
            };
        }

        private void SelectTable(ScriptableObject table)
        {
            selectedTable = table;
            selectedRecordIndex = -1;

            if (table != null)
            {
                serializedTable = new SerializedObject(table);
                recordsProperty = serializedTable.FindProperty("records")
                    ?? serializedTable.FindProperty("data");
            }
            else
            {
                serializedTable = null;
                recordsProperty = null;
            }
        }

        private void RefreshTableList()
        {
            allTables.Clear();

            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset is IXTableAsset)
                    allTables.Add(asset);
            }

            allTables.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        }

        private void AddNewRecord()
        {
            var xTable = selectedTable as IXTableAsset;
            if (xTable == null)
                return;

            var newRecord = xTable.CreateNewRecord();
            xTable.AddRecordObject(newRecord);

            serializedTable.Update();
            selectedRecordIndex = xTable.Count - 1;
            Repaint();
        }

        private void DeleteSelectedRecord()
        {
            if (selectedRecordIndex < 0)
                return;

            var xTable = selectedTable as IXTableAsset;
            if (xTable == null)
                return;

            if (!EditorUtility.DisplayDialog("確認", "選択したレコードを削除しますか？", "削除", "キャンセル"))
                return;

            xTable.RemoveRecordAt(selectedRecordIndex);
            serializedTable.Update();

            if (selectedRecordIndex >= xTable.Count)
                selectedRecordIndex = xTable.Count - 1;

            Repaint();
        }

        private void SortRecords()
        {
            if (selectedTable == null)
                return;

            var method = selectedTable.GetType().GetMethod("EnsureSorted");
            if (method != null)
            {
                method.Invoke(selectedTable, null);
                EditorUtility.SetDirty(selectedTable);
                serializedTable.Update();
                Repaint();
            }
        }

        private void SaveTable()
        {
            if (selectedTable == null)
                return;

            var xTable = selectedTable as IXTableAsset;
            if (xTable != null)
            {
                var duplicates = xTable.FindDuplicateKeysAsObjects();
                if (duplicates != null && duplicates.Count > 0)
                {
                    var keysString = string.Join(", ", duplicates.Cast<object>().Take(5));
                    if (!EditorUtility.DisplayDialog("警告",
                        $"重複したPrimaryKeyがあります: {keysString}\n保存しますか？",
                        "保存", "キャンセル"))
                        return;
                }
            }

            serializedTable.ApplyModifiedProperties();
            EditorUtility.SetDirty(selectedTable);
            AssetDatabase.SaveAssetIfDirty(selectedTable);
            EditorUtility.DisplayDialog("保存完了", $"{selectedTable.name}を保存しました", "OK");
        }

        private void OnDestroy()
        {
            if (selectedTable != null)
                AssetDatabase.SaveAssetIfDirty(selectedTable);
        }
    }
}
