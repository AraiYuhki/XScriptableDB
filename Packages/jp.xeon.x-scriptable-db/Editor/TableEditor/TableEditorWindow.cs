using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// TableAssetを編集するためのEditorWindow。
    /// </summary>
    public class TableEditorWindow : EditorWindow
    {
        private enum FileFormat
        {
            CSV,
            TSV
        }

        [MenuItem("Tools/XScriptableDB/テーブルエディタ")]
        public static void Open()
        {
            var window = GetWindow<TableEditorWindow>();
            window.titleContent = new GUIContent("Table Editor");
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

        // インポート/エクスポート設定
        private FileFormat exportFormat = FileFormat.CSV;
        private bool useExcelEncoding = false;

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

                DrawBulkOperations();
            }
        }

        private void DrawBulkOperations()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("一括操作", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                exportFormat = (FileFormat)EditorGUILayout.EnumPopup(exportFormat, GUILayout.Width(60));
                useExcelEncoding = GUILayout.Toggle(useExcelEncoding, "Excel", GUILayout.Width(50));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("全てエクスポート"))
                    ExportAllTables();
                if (GUILayout.Button("一括インポート"))
                    ImportAllTables();
            }
        }

        private void ExportAllTables()
        {
            var folderPath = EditorUtility.SaveFolderPanel(
                "エクスポート先フォルダを選択",
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "Masters");

            if (string.IsNullOrEmpty(folderPath))
                return;

            var encoding = useExcelEncoding ? Encoding.GetEncoding(932) : Encoding.UTF8;
            var extension = exportFormat == FileFormat.CSV ? "csv" : "tsv";
            var exportedCount = 0;

            foreach (var table in allTables)
            {
                if (table is not IExportable exporter)
                    continue;

                var filePath = Path.Combine(folderPath, $"{table.name}.{extension}");
                try
                {
                    exporter.Export(filePath, encoding);
                    exportedCount++;
                    Debug.Log($"Exported: {filePath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to export {table.name}: {e.Message}");
                }
            }

            EditorUtility.DisplayDialog("エクスポート完了", $"{exportedCount}件のテーブルをエクスポートしました\n{folderPath}", "OK");
        }

        private void ImportAllTables()
        {
            var folderPath = EditorUtility.OpenFolderPanel(
                "インポート元フォルダを選択",
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "Masters");

            if (string.IsNullOrEmpty(folderPath))
                return;

            var directoryInfo = new DirectoryInfo(folderPath);
            var csvFiles = directoryInfo.GetFiles("*.csv", SearchOption.TopDirectoryOnly);
            var tsvFiles = directoryInfo.GetFiles("*.tsv", SearchOption.TopDirectoryOnly);
            var allFiles = csvFiles.Concat(tsvFiles).ToArray();

            if (allFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("ファイルが見つかりません", "CSV/TSVファイルが見つかりませんでした", "OK");
                return;
            }

            var importedCount = 0;
            foreach (var file in allFiles)
            {
                var tableName = Path.GetFileNameWithoutExtension(file.Name);
                var targetTable = allTables.FirstOrDefault(t =>
                    string.Equals(t.name, tableName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.GetType().Name, tableName, StringComparison.OrdinalIgnoreCase));

                if (targetTable is not IImportable importer)
                    continue;

                try
                {
                    Debug.Log($"Importing: {file.Name} -> {targetTable.name}");
                    importer.Import(file.FullName);
                    EditorUtility.SetDirty(targetTable);
                    importedCount++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to import {file.Name}: {e.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 選択中のテーブルを更新
            if (selectedTable != null)
            {
                serializedTable?.Update();
                Repaint();
            }

            EditorUtility.DisplayDialog("インポート完了", $"{importedCount}件のテーブルをインポートしました", "OK");
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

                var tableAsset = selectedTable as ITableAsset;
                if (tableAsset != null)
                {
                    EditorGUILayout.LabelField($"レコード数: {tableAsset.Count}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"Key: {tableAsset.KeyType.Name}", GUILayout.Width(100));
                }
            }
        }

        private void DrawDuplicateKeyWarning()
        {
            var tableAsset = selectedTable as ITableAsset;
            if (tableAsset == null)
                return;

            var duplicates = tableAsset.FindDuplicateKeysAsObjects();
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

                // インポート/エクスポート
                DrawImportExportButtons();

                GUILayout.Space(10);

                if (GUILayout.Button("ソート", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    SortRecords();

                if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    SaveTable();
            }
        }

        private void DrawImportExportButtons()
        {
            var isExportable = selectedTable is IExportable;
            var isImportable = selectedTable is IImportable;

            if (!isExportable && !isImportable)
                return;

            // フォーマット選択
            exportFormat = (FileFormat)EditorGUILayout.EnumPopup(exportFormat, EditorStyles.toolbarPopup, GUILayout.Width(50));

            EditorGUI.BeginDisabledGroup(!isImportable);
            if (GUILayout.Button("インポート", EditorStyles.toolbarButton, GUILayout.Width(80)))
                ImportFromFile();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!isExportable);
            if (GUILayout.Button("エクスポート", EditorStyles.toolbarButton, GUILayout.Width(80)))
                ExportToFile();

            // Excel対応エンコーディングオプション
            useExcelEncoding = GUILayout.Toggle(useExcelEncoding, "Excel", EditorStyles.toolbarButton, GUILayout.Width(50));
            EditorGUI.EndDisabledGroup();
        }

        private void ImportFromFile()
        {
            var tableAsset = selectedTable as ITableAsset;
            if (tableAsset == null && selectedTable is not IImportable)
                return;

            var extension = exportFormat == FileFormat.CSV ? "csv" : "tsv";
            var filterName = exportFormat == FileFormat.CSV ? "CSV files" : "TSV files";

            var filePath = EditorUtility.OpenFilePanelWithFilters(
                $"インポートするファイルを選択 ({selectedTable.name})",
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                new[] { filterName, extension, "All files", "*" });

            if (string.IsNullOrEmpty(filePath))
                return;

            try
            {
                // ITableAssetの場合はプレビュー付きインポート
                if (tableAsset != null)
                {
                    var encoding = useExcelEncoding ? Encoding.GetEncoding(932) : Encoding.UTF8;
                    var importedRecords = TableImporter.ParseFile(filePath, tableAsset.RecordType, encoding);

                    if (importedRecords.Length == 0)
                    {
                        EditorUtility.DisplayDialog("警告", "インポートするレコードがありません", "OK");
                        return;
                    }

                    // 差分を計算してDiffViewerを開く
                    var diffResult = DiffCalculator.Calculate(tableAsset, importedRecords);
                    DiffViewerWindow.Open(diffResult, selectedTable, importedRecords);
                }
                else if (selectedTable is IImportable importer)
                {
                    // 従来の直接インポート
                    importer.Import(filePath);
                    serializedTable.Update();
                    EditorUtility.SetDirty(selectedTable);
                    EditorUtility.DisplayDialog("インポート完了", $"{selectedTable.name}をインポートしました", "OK");
                    Repaint();
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("インポートエラー", $"インポート中にエラーが発生しました:\n{e.Message}", "OK");
                Debug.LogException(e);
            }
        }

        private void ExportToFile()
        {
            if (selectedTable is not IExportable exporter)
                return;

            var extension = exportFormat == FileFormat.CSV ? "csv" : "tsv";
            var filePath = EditorUtility.SaveFilePanel(
                "エクスポート先を選択",
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                $"{selectedTable.name}.{extension}",
                extension);

            if (string.IsNullOrEmpty(filePath))
                return;

            try
            {
                // Excel対応の場合はShift-JIS (CP932)を使用
                var encoding = useExcelEncoding ? Encoding.GetEncoding(932) : Encoding.UTF8;
                exporter.Export(filePath, encoding);
                EditorUtility.DisplayDialog("エクスポート完了", $"{selectedTable.name}をエクスポートしました\n{filePath}", "OK");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("エクスポートエラー", $"エクスポート中にエラーが発生しました:\n{e.Message}", "OK");
                Debug.LogException(e);
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
                if (asset is ITableAsset)
                    allTables.Add(asset);
            }

            allTables.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        }

        private void AddNewRecord()
        {
            var tableAsset = selectedTable as ITableAsset;
            if (tableAsset == null)
                return;

            var newRecord = tableAsset.CreateNewRecord();
            tableAsset.AddRecordObject(newRecord);

            serializedTable.Update();
            selectedRecordIndex = tableAsset.Count - 1;
            Repaint();
        }

        private void DeleteSelectedRecord()
        {
            if (selectedRecordIndex < 0)
                return;

            var tableAsset = selectedTable as ITableAsset;
            if (tableAsset == null)
                return;

            if (!EditorUtility.DisplayDialog("確認", "選択したレコードを削除しますか？", "削除", "キャンセル"))
                return;

            tableAsset.RemoveRecordAt(selectedRecordIndex);
            serializedTable.Update();

            if (selectedRecordIndex >= tableAsset.Count)
                selectedRecordIndex = tableAsset.Count - 1;

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

            var tableAsset = selectedTable as ITableAsset;
            if (tableAsset != null)
            {
                var duplicates = tableAsset.FindDuplicateKeysAsObjects();
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
