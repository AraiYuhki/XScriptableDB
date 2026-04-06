using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// テーブル差分を表示・適用するためのEditorWindow。
    /// </summary>
    public class DiffViewerWindow : EditorWindow
    {
        /// <summary>
        /// 差分結果を指定してウィンドウを開く。
        /// </summary>
        public static DiffViewerWindow Open(TableDiffResult diffResult, ScriptableObject targetTable,
            object[] importedRecords)
        {
            return Open(diffResult, targetTable, importedRecords, null);
        }

        /// <summary>
        /// 差分結果を指定してウィンドウを開く（適用時コールバック付き）。
        /// </summary>
        public static DiffViewerWindow Open(TableDiffResult diffResult, ScriptableObject targetTable,
            object[] importedRecords, Action onApplied)
        {
            var window = GetWindow<DiffViewerWindow>();
            window.titleContent = new GUIContent("Diff Viewer");
            window.SetDiffResult(diffResult, targetTable, importedRecords, onApplied);
            window.Show();
            return window;
        }

        private TableDiffResult diffResult;
        private ScriptableObject targetTable;
        private object[] importedRecords;
        private Action onAppliedCallback;

        private Vector2 scrollPosition;
        private HashSet<object> selectedKeys = new();
        private bool showUnchanged = false;
        private DiffType? filterType = null;

        private GUIStyle addedStyle;
        private GUIStyle removedStyle;
        private GUIStyle modifiedStyle;
        private GUIStyle unchangedStyle;
        private GUIStyle headerStyle;

        private bool stylesInitialized = false;

        public void SetDiffResult(TableDiffResult result, ScriptableObject table, object[] records,
            Action onApplied = null)
        {
            diffResult = result;
            targetTable = table;
            importedRecords = records;
            onAppliedCallback = onApplied;
            selectedKeys.Clear();

            // デフォルトで全ての変更を選択
            if (result != null)
            {
                foreach (var diff in result.Diffs)
                {
                    if (diff.DiffType != DiffType.Unchanged)
                        selectedKeys.Add(diff.PrimaryKey);
                }
            }
        }

        private void InitializeStyles()
        {
            if (stylesInitialized)
                return;

            addedStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) }
            };

            removedStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.9f, 0.3f, 0.3f) }
            };

            modifiedStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.9f, 0.7f, 0.2f) }
            };

            unchangedStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.gray }
            };

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(4, 4, 8, 8)
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitializeStyles();

            if (diffResult == null)
            {
                DrawNoDiffState();
                return;
            }

            DrawHeader();
            DrawToolbar();
            DrawDiffList();
            DrawFooter();
        }

        private void DrawNoDiffState()
        {
            EditorGUILayout.HelpBox("No diff data available.\nImport a CSV from the table editor or select data to compare.", MessageType.Info);

            EditorGUILayout.Space(20);

            if (GUILayout.Button("Open Table Editor", GUILayout.Height(30)))
            {
                DataEditorWindow.Open();
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField($"Diff Comparison: {diffResult.TableName}", headerStyle);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawStatBadge("+", diffResult.AddedCount, addedStyle);
                DrawStatBadge("-", diffResult.RemovedCount, removedStyle);
                DrawStatBadge("*", diffResult.ModifiedCount, modifiedStyle);
                if (showUnchanged)
                    DrawStatBadge("=", diffResult.UnchangedCount, unchangedStyle);
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(4);
        }

        private void DrawStatBadge(string prefix, int count, GUIStyle style)
        {
            EditorGUILayout.LabelField($"{prefix}{count}", style, GUILayout.Width(50));
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // フィルター
                if (GUILayout.Toggle(filterType == null, "All", EditorStyles.toolbarButton, GUILayout.Width(50)))
                    filterType = null;
                if (GUILayout.Toggle(filterType == DiffType.Added, "Added", EditorStyles.toolbarButton,
                        GUILayout.Width(50)))
                    filterType = filterType == DiffType.Added ? null : DiffType.Added;
                if (GUILayout.Toggle(filterType == DiffType.Removed, "Removed", EditorStyles.toolbarButton,
                        GUILayout.Width(60)))
                    filterType = filterType == DiffType.Removed ? null : DiffType.Removed;
                if (GUILayout.Toggle(filterType == DiffType.Modified, "Modified", EditorStyles.toolbarButton,
                        GUILayout.Width(60)))
                    filterType = filterType == DiffType.Modified ? null : DiffType.Modified;

                GUILayout.Space(10);
                showUnchanged = GUILayout.Toggle(showUnchanged, "Show Unchanged", EditorStyles.toolbarButton);

                GUILayout.FlexibleSpace();

                // 選択操作
                if (GUILayout.Button("Select All", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    SelectAll();
                if (GUILayout.Button("Deselect All", EditorStyles.toolbarButton, GUILayout.Width(75)))
                    selectedKeys.Clear();
            }
        }

        private void DrawDiffList()
        {
            using var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition);
            scrollPosition = scroll.scrollPosition;

            foreach (var diff in diffResult.Diffs)
            {
                if (!ShouldShowDiff(diff))
                    continue;

                DrawDiffEntry(diff);
            }
        }

        private bool ShouldShowDiff(RecordDiff diff)
        {
            if (!showUnchanged && diff.DiffType == DiffType.Unchanged)
                return false;

            if (filterType.HasValue && diff.DiffType != filterType.Value)
                return false;

            return true;
        }

        private void DrawDiffEntry(RecordDiff diff)
        {
            var style = GetStyleForDiffType(diff.DiffType);
            var isSelected = selectedKeys.Contains(diff.PrimaryKey);
            var canSelect = diff.DiffType != DiffType.Unchanged;

            using var _ = new EditorGUILayout.VerticalScope("Box");
            using (new EditorGUILayout.HorizontalScope())
            {
                // チェックボックス
                EditorGUI.BeginDisabledGroup(!canSelect);
                var newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                if (newSelected != isSelected && canSelect)
                {
                    if (newSelected)
                        selectedKeys.Add(diff.PrimaryKey);
                    else
                        selectedKeys.Remove(diff.PrimaryKey);
                }

                EditorGUI.EndDisabledGroup();

                // 差分タイプアイコン
                var icon = diff.DiffType switch
                {
                    DiffType.Added => "[+]",
                    DiffType.Removed => "[-]",
                    DiffType.Modified => "[*]",
                    _ => "[=]"
                };
                EditorGUILayout.LabelField(icon, style, GUILayout.Width(30));

                // PrimaryKey
                EditorGUILayout.LabelField($"Key: {diff.PrimaryKey}", style, GUILayout.Width(150));

                // 変更フィールド数
                if (diff.DiffType == DiffType.Modified)
                {
                    EditorGUILayout.LabelField($"({diff.ChangedFieldCount} fields changed)", GUILayout.Width(120));
                }

                GUILayout.FlexibleSpace();
            }

            // フィールド詳細
            if (diff.DiffType == DiffType.Modified && diff.FieldDiffs.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var fieldDiff in diff.FieldDiffs)
                {
                    if (!fieldDiff.HasChanged)
                        continue;

                    DrawFieldDiff(fieldDiff);
                }

                EditorGUI.indentLevel--;
            }
            else if (diff.DiffType == DiffType.Added && diff.NewRecord != null)
            {
                EditorGUI.indentLevel++;
                DrawRecordSummary(diff.NewRecord, addedStyle);
                EditorGUI.indentLevel--;
            }
            else if (diff.DiffType == DiffType.Removed && diff.OldRecord != null)
            {
                EditorGUI.indentLevel++;
                DrawRecordSummary(diff.OldRecord, removedStyle);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawFieldDiff(FieldDiff fieldDiff)
        {
            using var _ = new EditorGUILayout.HorizontalScope();
            EditorGUILayout.LabelField(fieldDiff.FieldName, GUILayout.Width(120));

            var oldStr = FormatValue(fieldDiff.OldValue);
            var newStr = FormatValue(fieldDiff.NewValue);

            EditorGUILayout.LabelField(oldStr, removedStyle, GUILayout.Width(150));
            EditorGUILayout.LabelField("→", GUILayout.Width(20));
            EditorGUILayout.LabelField(newStr, addedStyle, GUILayout.Width(150));
        }

        private void DrawRecordSummary(object record, GUIStyle style)
        {
            if (record == null)
                return;

            var type = record.GetType();
            var fields = ReflectionUtility.GetSerializableFields(type);
            var count = 0;

            using var _ = new EditorGUILayout.HorizontalScope();
            foreach (var field in fields)
            {
                if (count >= 4)
                {
                    EditorGUILayout.LabelField("...", style, GUILayout.Width(30));
                    break;
                }

                var value = field.GetValue(record);
                var str = FormatValue(value);
                EditorGUILayout.LabelField($"{field.Name}: {str}", style, GUILayout.Width(150));
                count++;
            }
        }

        private string FormatValue(object value)
        {
            if (value == null)
                return "(null)";

            if (value is DateTime dt)
                return DateTimeEditorUtility.FormatDateTime(dt);

            if (value is SerializableDateTime sdt)
                return DateTimeEditorUtility.FormatDateTime(sdt.DateTime);

            var str = value.ToString();
            if (str.Length > 20)
                str = str.Substring(0, 17) + "...";

            return str;
        }

        private GUIStyle GetStyleForDiffType(DiffType type)
        {
            return type switch
            {
                DiffType.Added => addedStyle,
                DiffType.Removed => removedStyle,
                DiffType.Modified => modifiedStyle,
                _ => unchangedStyle
            };
        }

        private void SelectAll()
        {
            selectedKeys.Clear();
            foreach (var diff in diffResult.Diffs)
            {
                if (diff.DiffType != DiffType.Unchanged)
                {
                    if (!filterType.HasValue || diff.DiffType == filterType.Value)
                        selectedKeys.Add(diff.PrimaryKey);
                }
            }
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space(10);

            using var _ = new EditorGUILayout.HorizontalScope();
            var selectedCount = selectedKeys.Count;
            EditorGUILayout.LabelField($"Selected: {selectedCount}");

            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(selectedCount == 0 || targetTable == null);
            if (GUILayout.Button("Apply Selected Changes", GUILayout.Width(160), GUILayout.Height(30)))
            {
                ApplySelectedChanges();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(targetTable == null || importedRecords == null);
            if (GUILayout.Button("Apply All Changes", GUILayout.Width(150), GUILayout.Height(30)))
            {
                ApplyAllChanges();
            }

            EditorGUI.EndDisabledGroup();
        }

        private void ApplySelectedChanges()
        {
            if (targetTable == null || diffResult == null)
                return;

            var tableAsset = targetTable as ITableAsset;
            if (tableAsset == null)
            {
                EditorUtility.DisplayDialog("Error", "Table asset is invalid", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Confirm",
                    $"Apply {selectedKeys.Count} change(s)?",
                    "Apply", "Cancel"))
                return;

            try
            {
                ApplyChangesToTable(tableAsset, selectedKeys);
                EditorUtility.SetDirty(targetTable);
                EditorUtility.DisplayDialog("Complete", "Changes have been applied", "OK");

                // コールバック呼び出し
                onAppliedCallback?.Invoke();

                // 差分を再計算
                RefreshDiff();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"An error occurred while applying changes:\n{e.Message}", "OK");
                Debug.LogException(e);
            }
        }

        private void ApplyAllChanges()
        {
            if (targetTable == null || importedRecords == null)
                return;

            if (!EditorUtility.DisplayDialog("Confirm",
                    "Apply all changes?\nThis will overwrite the current table data.",
                    "Apply", "Cancel"))
                return;

            try
            {
                // SetRecordsメソッドを呼び出す
                var setRecordsMethod = targetTable.GetType().GetMethod("SetRecords");
                if (setRecordsMethod != null)
                {
                    var recordType = (targetTable as ITableAsset)?.RecordType;
                    if (recordType != null)
                    {
                        var typedArray = Array.CreateInstance(recordType, importedRecords.Length);
                        Array.Copy(importedRecords, typedArray, importedRecords.Length);
                        setRecordsMethod.Invoke(targetTable, new object[] { typedArray });
                    }
                }

                EditorUtility.SetDirty(targetTable);

                // コールバックがない場合のみ自動保存
                if (onAppliedCallback == null)
                    AssetDatabase.SaveAssetIfDirty(targetTable);

                EditorUtility.DisplayDialog("Complete", "All changes have been applied", "OK");

                // コールバック呼び出し
                onAppliedCallback?.Invoke();

                // ウィンドウを閉じる
                Close();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"An error occurred while applying changes:\n{e.Message}", "OK");
                Debug.LogException(e);
            }
        }

        private void ApplyChangesToTable(ITableAsset tableAsset, HashSet<object> keysToApply)
        {
            // 現在のレコードをリストにコピー
            var currentRecords = new List<object>();
            foreach (var record in tableAsset.Records)
                currentRecords.Add(record);

            var recordType = tableAsset.RecordType;

            // 変更を適用
            foreach (var diff in diffResult.Diffs)
            {
                if (!keysToApply.Contains(diff.PrimaryKey))
                    continue;

                switch (diff.DiffType)
                {
                    case DiffType.Added:
                        if (diff.NewRecord != null)
                            currentRecords.Add(diff.NewRecord);
                        break;

                    case DiffType.Removed:
                        if (diff.OldIndex >= 0 && diff.OldIndex < currentRecords.Count)
                        {
                            // インデックスで削除すると順序が変わるので、nullにしてあとで除去
                            currentRecords[diff.OldIndex] = null;
                        }

                        break;

                    case DiffType.Modified:
                        if (diff.OldIndex >= 0 && diff.OldIndex < currentRecords.Count && diff.NewRecord != null)
                        {
                            currentRecords[diff.OldIndex] = diff.NewRecord;
                        }

                        break;
                }
            }

            // nullを除去
            currentRecords.RemoveAll(r => r == null);

            // SetRecordsを呼び出す
            var setRecordsMethod = targetTable.GetType().GetMethod("SetRecords");
            if (setRecordsMethod != null)
            {
                var typedArray = Array.CreateInstance(recordType, currentRecords.Count);
                for (var i = 0; i < currentRecords.Count; i++)
                    typedArray.SetValue(currentRecords[i], i);
                setRecordsMethod.Invoke(targetTable, new object[] { typedArray });
            }
        }

        private void RefreshDiff()
        {
            if (targetTable == null || importedRecords == null)
                return;

            var tableAsset = targetTable as ITableAsset;
            if (tableAsset == null)
                return;

            diffResult = DiffCalculator.Calculate(tableAsset, importedRecords);
            selectedKeys.Clear();

            foreach (var diff in diffResult.Diffs)
            {
                if (diff.DiffType != DiffType.Unchanged)
                    selectedKeys.Add(diff.PrimaryKey);
            }

            Repaint();
        }
    }
}