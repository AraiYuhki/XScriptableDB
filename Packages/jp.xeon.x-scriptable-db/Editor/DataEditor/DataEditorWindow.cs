using System;
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
    /// 仮想スクロールにより大量レコードの表示に対応。
    /// </summary>
    public class DataEditorWindow : EditorWindow
    {
        private enum FileFormat
        {
            CSV,
            TSV
        }

        [MenuItem("Tools/XScriptableDB/Data Editor")]
        public static void Open()
        {
            var window = GetWindow<DataEditorWindow>();
            window.titleContent = new GUIContent("Data Editor");
            window.Show();
        }

        private ScriptableObject selectedTable;
        private ScriptableObject editingClone;
        private SerializedObject serializedTable;
        private SerializedProperty recordsProperty;

        private Vector2 tableListScrollPosition;
        private int selectedRecordIndex = -1;

        private List<ScriptableObject> allTables = new();
        private string searchFilter = "";

        private GUIStyle headerStyle;
        private GUIStyle warningStyle;
        private GUIStyle unsavedStyle;

        // 仮想スクロール
        private VirtualizedPropertyListView virtualizedList;
        private bool useVirtualScroll = true;
        private const int VirtualScrollThreshold = 100;

        // インポート/エクスポート設定
        private FileFormat exportFormat = FileFormat.CSV;
        private bool useExcelEncoding = false;

        // 変更追跡
        private bool idDirty;
        private int lastRecordHash;

        private void OnEnable()
        {
            RefreshTableList();
            InitializeVirtualizedList();

            // ドメインリロード後にクローンが失われた場合は再作成
            if (selectedTable != null && editingClone == null)
            {
                CreateEditingClone(selectedTable);
                idDirty = false; // リロード後は未保存状態をリセット
            }
        }

        private void InitializeVirtualizedList()
        {
            virtualizedList = new VirtualizedPropertyListView
            {
                ItemHeight = 26f,
                BufferCount = 5,
                CalculateExpandedHeight = CalculatePropertyHeight,
                OnDrawSummary = DrawRecordSummaryVirtualized,
                OnSelectionChanged = OnRecordSelectionChanged
            };
        }

        private float CalculatePropertyHeight(SerializedProperty property)
        {
            return EditorGUI.GetPropertyHeight(property, true) + 8f;
        }

        private void OnRecordSelectionChanged(int index)
        {
            selectedRecordIndex = index;
            Repaint();
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

            unsavedStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(1f, 0.5f, 0f) }
            };
        }

        private void DrawTableListPanel()
        {
            using var _ = new EditorGUILayout.VerticalScope(GUILayout.Width(250));
            EditorGUILayout.LabelField("Table List", headerStyle);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField);
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(40)))
                    RefreshTableList();
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(tableListScrollPosition))
            {
                tableListScrollPosition = scroll.scrollPosition;
                DrawTableList();
            }

            DrawBulkOperations();
        }

        private void DrawBulkOperations()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Bulk Operations", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                exportFormat = (FileFormat)EditorGUILayout.EnumPopup(exportFormat, GUILayout.Width(60));
                useExcelEncoding = GUILayout.Toggle(useExcelEncoding, "Excel", GUILayout.Width(50));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Export All"))
                    ExportAllTables();
                if (GUILayout.Button("Import All"))
                    ImportAllTables();
            }
        }

        private void ExportAllTables()
        {
            var folderPath = EditorUtility.SaveFolderPanel(
                "Select Export Destination Folder",
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

            EditorUtility.DisplayDialog("Export Complete", $"Exported {exportedCount} table(s)\n{folderPath}", "OK");
        }

        private void ImportAllTables()
        {
            var folderPath = EditorUtility.OpenFolderPanel(
                "Select Import Source Folder",
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
                EditorUtility.DisplayDialog("No Files Found", "No CSV/TSV files were found", "OK");
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
                virtualizedList?.ClearCache();
                Repaint();
            }

            EditorUtility.DisplayDialog("Import Complete", $"Imported {importedCount} table(s)", "OK");
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

                    if (GUILayout.Button("Select", GUILayout.Width(40)))
                    {
                        Selection.activeObject = table;
                        EditorGUIUtility.PingObject(table);
                    }
                }
            }

            if (filteredTables.Count == 0)
                EditorGUILayout.HelpBox("No tables found", MessageType.Info);
        }

        private void DrawRecordListPanel()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (selectedTable == null || editingClone == null)
                {
                    EditorGUILayout.HelpBox("Select a table from the list on the left", MessageType.Info);
                    return;
                }

                DrawRecordHeader();
                DrawDuplicateKeyWarning();
                DrawRecordToolbar();

                // 仮想スクロール使用判定
                var tableAsset = editingClone as ITableAsset;
                var recordCount = tableAsset?.Count ?? recordsProperty?.arraySize ?? 0;
                var shouldUseVirtualScroll = useVirtualScroll && recordCount >= VirtualScrollThreshold;

                if (shouldUseVirtualScroll)
                    DrawRecordListVirtualized();
                else
                    DrawRecordListStandard();
            }
        }

        private void DrawRecordHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var title = idDirty ? $"{selectedTable.name} *" : selectedTable.name;
                EditorGUILayout.LabelField(title, idDirty ? unsavedStyle : headerStyle);

                if (idDirty)
                    EditorGUILayout.LabelField("(Unsaved)", unsavedStyle, GUILayout.Width(60));
                
                if (editingClone is not ITableAsset editingTableAsset)
                    return;
                
                EditorGUILayout.LabelField($"Records: {editingTableAsset.Count}", GUILayout.Width(100));

                var originalTableAsset = selectedTable as ITableAsset;
                if (originalTableAsset != null)
                    EditorGUILayout.LabelField($"Key: {originalTableAsset.KeyType.Name}", GUILayout.Width(100));

                // 仮想スクロール切り替え
                if (editingTableAsset.Count < VirtualScrollThreshold)
                    return;
                
                var newUseVirtualScroll = GUILayout.Toggle(useVirtualScroll, "Virtual Scroll", GUILayout.Width(100));
                if (newUseVirtualScroll != useVirtualScroll)
                {
                    useVirtualScroll = newUseVirtualScroll;
                    virtualizedList?.ClearCache();
                }
            }
        }

        private void DrawDuplicateKeyWarning()
        {
            var tableAsset = editingClone as ITableAsset;
            if (tableAsset == null)
                return;

            var duplicates = tableAsset.FindDuplicateKeysAsObjects();
            if (duplicates == null || duplicates.Count == 0)
                return;

            var keysString = string.Join(", ", duplicates.Cast<object>().Take(5));
            if (duplicates.Count > 5)
                keysString += $"... and {duplicates.Count - 5} more";

            EditorGUILayout.HelpBox($"Duplicate PrimaryKeys found: {keysString}", MessageType.Warning);
        }

        private void DrawRecordToolbar()
        {
            using var _ = new EditorGUILayout.HorizontalScope(EditorStyles.toolbar);
            
            if (GUILayout.Button("Add", EditorStyles.toolbarButton, GUILayout.Width(60)))
                AddNewRecord();

            EditorGUI.BeginDisabledGroup(selectedRecordIndex < 0);
            if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(60)))
                DeleteSelectedRecord();
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();

            // インポート/エクスポート
            DrawImportExportButtons();

            GUILayout.Space(10);

            if (GUILayout.Button("Sort", EditorStyles.toolbarButton, GUILayout.Width(60)))
                SortRecords();

            EditorGUI.BeginDisabledGroup(!idDirty);
            if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(60)))
                ResetChanges();

            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
                SaveTable();
            EditorGUI.EndDisabledGroup();
        }

        private void DrawImportExportButtons()
        {
            var isExportable = selectedTable is IExportable;
            var isImportable = selectedTable is IImportable;

            if (!isExportable && !isImportable)
                return;

            // フォーマット選択
            exportFormat =
                (FileFormat)EditorGUILayout.EnumPopup(exportFormat, EditorStyles.toolbarPopup, GUILayout.Width(50));

            EditorGUI.BeginDisabledGroup(!isImportable);
            if (GUILayout.Button("Import", EditorStyles.toolbarButton, GUILayout.Width(80)))
                ImportFromFile();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!isExportable);
            if (GUILayout.Button("Export", EditorStyles.toolbarButton, GUILayout.Width(80)))
                ExportToFile();

            // Excel対応エンコーディングオプション
            useExcelEncoding =
                GUILayout.Toggle(useExcelEncoding, "Excel", EditorStyles.toolbarButton, GUILayout.Width(50));
            EditorGUI.EndDisabledGroup();
        }

        private void ImportFromFile()
        {
            var editingTableAsset = editingClone as ITableAsset;
            var originalTableAsset = selectedTable as ITableAsset;

            if (editingTableAsset == null && editingClone is not IImportable)
                return;

            var extension = exportFormat == FileFormat.CSV ? "csv" : "tsv";
            var filterName = exportFormat == FileFormat.CSV ? "CSV files" : "TSV files";

            var filePath = EditorUtility.OpenFilePanelWithFilters(
                $"Select File to Import ({selectedTable.name})",
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                new[] { filterName, extension, "All files", "*" });

            if (string.IsNullOrEmpty(filePath))
                return;

            try
            {
                // ITableAssetの場合はプレビュー付きインポート
                if (editingTableAsset != null && originalTableAsset != null)
                {
                    var encoding = useExcelEncoding ? Encoding.GetEncoding(932) : Encoding.UTF8;
                    var importedRecords = TableImporter.ParseFile(filePath, originalTableAsset.RecordType, encoding);

                    if (importedRecords.Length == 0)
                    {
                        EditorUtility.DisplayDialog("Warning", "No records to import", "OK");
                        return;
                    }

                    // 差分を計算してDiffViewerを開く（クローンに対して）
                    var diffResult = DiffCalculator.Calculate(editingTableAsset, importedRecords);
                    DiffViewerWindow.Open(diffResult, editingClone, importedRecords, () =>
                    {
                        serializedTable.Update();
                        virtualizedList?.ClearCache();
                        idDirty = true;
                        Repaint();
                    });
                }
                else if (editingClone is IImportable importer)
                {
                    // 従来の直接インポート
                    importer.Import(filePath);
                    serializedTable.Update();
                    virtualizedList?.ClearCache();
                    idDirty = true;
                    EditorUtility.DisplayDialog("Import Complete", $"Imported {selectedTable.name} (unsaved)", "OK");
                    Repaint();
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Import Error", $"An error occurred during import:\n{e.Message}", "OK");
                Debug.LogException(e);
            }
        }

        private void ExportToFile()
        {
            if (selectedTable is not IExportable exporter)
                return;

            var extension = exportFormat == FileFormat.CSV ? "csv" : "tsv";
            var filePath = EditorUtility.SaveFilePanel(
                "Select Export Destination",
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
                EditorUtility.DisplayDialog("Export Complete", $"Exported {selectedTable.name}\n{filePath}", "OK");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Export Error", $"An error occurred during export:\n{e.Message}", "OK");
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// 仮想スクロールを使用したレコードリスト描画。
        /// </summary>
        private void DrawRecordListVirtualized()
        {
            if (serializedTable == null || recordsProperty == null)
                return;

            serializedTable.Update();

            // 仮想スクロールリストの設定
            virtualizedList.SetProperty(recordsProperty);
            virtualizedList.SelectedIndex = selectedRecordIndex;

            // 描画領域を確保
            var rect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (rect.height > 0)
            {
                virtualizedList.Draw(rect);
                selectedRecordIndex = virtualizedList.SelectedIndex;
            }

            if (serializedTable.ApplyModifiedProperties())
            {
                virtualizedList.ClearCache();
                CheckForChanges();
            }
        }

        /// <summary>
        /// 標準のレコードリスト描画。
        /// </summary>
        private void DrawRecordListStandard()
        {
            if (serializedTable == null || recordsProperty == null)
                return;

            serializedTable.Update();

            using (var scroll =
                   new EditorGUILayout.ScrollViewScope(virtualizedList?.GetScrollPosition() ?? Vector2.zero))
            {
                virtualizedList?.SetScrollPosition(scroll.scrollPosition);

                for (var i = 0; i < recordsProperty.arraySize; i++)
                {
                    var isSelected = i == selectedRecordIndex;
                    var element = recordsProperty.GetArrayElementAtIndex(i);
                    using var _ = new EditorGUILayout.VerticalScope(isSelected ? "SelectionRect" : "Box");
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(40));
                        if (GUILayout.Button(isSelected ? "v" : ">", GUILayout.Width(25)))
                            selectedRecordIndex = isSelected ? -1 : i;

                        DrawRecordSummary(element);
                    }

                    if (isSelected)
                        EditorGUILayout.PropertyField(element, GUIContent.none, true);
                }
            }

            if (serializedTable.ApplyModifiedProperties())
                CheckForChanges();
        }

        private void DrawRecordSummaryVirtualized(int index, SerializedProperty element, Rect rect)
        {
            var iterator = element.Copy();
            var enterChildren = true;
            var depth = iterator.depth;
            var count = 0;
            const int maxFields = 4;
            var x = rect.x;

            while (iterator.NextVisible(enterChildren) && iterator.depth > depth && count < maxFields)
            {
                enterChildren = false;
                var value = GetPropertyValueString(iterator);
                var content = new GUIContent($"{iterator.name}: {value}");
                var width = Mathf.Min(150, rect.width / maxFields);
                EditorGUI.LabelField(new Rect(x, rect.y + 4, width, rect.height - 8), content);
                x += width + 4;
                count++;
            }
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
            if (property.propertyType == SerializedPropertyType.Generic)
            {
                if (DateTimeEditorUtility.IsDateTimeProperty(property))
                {
                    var dateTime = DateTimeEditorUtility.GetDateTime(property);
                    return DateTimeEditorUtility.FormatDateTime(dateTime);
                }
            }

            return property.propertyType switch
            {
                SerializedPropertyType.Integer => property.intValue.ToString(),
                SerializedPropertyType.Float => property.floatValue.ToString("F2"),
                SerializedPropertyType.String => property.stringValue.Length > 15
                    ? property.stringValue.Substring(0, 15) + "..."
                    : property.stringValue,
                SerializedPropertyType.Boolean => property.boolValue.ToString(),
                SerializedPropertyType.Enum => property.enumDisplayNames.Length > property.enumValueIndex &&
                                               property.enumValueIndex >= 0
                    ? property.enumDisplayNames[property.enumValueIndex]
                    : property.enumValueIndex.ToString(),
                _ => "..."
            };
        }

        private void SelectTable(ScriptableObject table)
        {
            // 未保存の変更がある場合は確認
            if (idDirty && selectedTable != null)
            {
                var result = EditorUtility.DisplayDialogComplex(
                    "Unsaved Changes",
                    $"{selectedTable.name} has unsaved changes. Do you want to save?",
                    "Save", "Discard", "Cancel");

                switch (result)
                {
                    case 0:
                        ApplyChangesToOriginal();
                        break;
                    case 1:
                        break;
                    case 2:
                        return;
                }
            }

            selectedTable = table;
            selectedRecordIndex = -1;
            idDirty = false;

            if (table != null)
            {
                CreateEditingClone(table);

                virtualizedList?.ClearCache();
                virtualizedList?.ResetScroll();
                if (virtualizedList != null)
                {
                    virtualizedList.ExpandedIndex = -1;
                    virtualizedList.SelectedIndex = -1;
                }
            }
            else
            {
                CleanupClone();
                serializedTable = null;
                recordsProperty = null;
            }
        }

        private void CreateEditingClone(ScriptableObject original)
        {
            CleanupClone();

            // クローンを作成
            editingClone = Instantiate(original);
            editingClone.name = original.name + " (Editing)";
            editingClone.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

            serializedTable = new SerializedObject(editingClone);
            recordsProperty = serializedTable.FindProperty("records")
                              ?? serializedTable.FindProperty("data");

            lastRecordHash = CalculateRecordHash();
        }

        private void CleanupClone()
        {
            if (editingClone != null)
            {
                DestroyImmediate(editingClone);
                editingClone = null;
            }
        }

        private int CalculateRecordHash()
        {
            if (recordsProperty == null)
                return 0;

            unchecked
            {
                var hash = recordsProperty.arraySize;
                for (var i = 0; i < Mathf.Min(recordsProperty.arraySize, 100); i++)
                {
                    var element = recordsProperty.GetArrayElementAtIndex(i);
                    hash = hash * 31 + (int)element.contentHash;
                }

                return hash;
            }
        }

        private void CheckForChanges()
        {
            if (recordsProperty == null)
                return;

            var currentHash = CalculateRecordHash();
            if (currentHash != lastRecordHash)
            {
                idDirty = true;
                lastRecordHash = currentHash;
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
            var tableAsset = editingClone as ITableAsset;
            if (tableAsset == null)
                return;

            var newRecord = tableAsset.CreateNewRecord();
            tableAsset.AddRecordObject(newRecord);

            serializedTable.Update();
            selectedRecordIndex = tableAsset.Count - 1;
            virtualizedList?.ClearCache();
            idDirty = true;

            // 新しいレコードにスクロール
            virtualizedList?.ScrollToIndex(selectedRecordIndex);

            Repaint();
        }

        private void DeleteSelectedRecord()
        {
            if (selectedRecordIndex < 0)
                return;

            var tableAsset = editingClone as ITableAsset;
            if (tableAsset == null)
                return;

            if (!EditorUtility.DisplayDialog("Confirm", "Delete the selected record?", "Delete", "Cancel"))
                return;

            tableAsset.RemoveRecordAt(selectedRecordIndex);
            serializedTable.Update();
            virtualizedList?.ClearCache();
            idDirty = true;

            if (selectedRecordIndex >= tableAsset.Count)
                selectedRecordIndex = tableAsset.Count - 1;

            if (virtualizedList != null)
                virtualizedList.SelectedIndex = selectedRecordIndex;

            Repaint();
        }

        private void SortRecords()
        {
            if (editingClone == null)
                return;

            var method = editingClone.GetType().GetMethod("EnsureSorted");
            if (method != null)
            {
                method.Invoke(editingClone, null);
                serializedTable.Update();
                virtualizedList?.ClearCache();
                idDirty = true;
                Repaint();
            }
        }

        private void ResetChanges()
        {
            if (!idDirty)
                return;

            if (!EditorUtility.DisplayDialog("Confirm", "Discard changes and revert?", "Discard", "Cancel"))
                return;

            CreateEditingClone(selectedTable);
            virtualizedList?.ClearCache();
            idDirty = false;
            Repaint();
        }

        private void SaveTable()
        {
            if (selectedTable == null || editingClone == null)
                return;

            var editingTableAsset = editingClone as ITableAsset;
            if (editingTableAsset != null)
            {
                var duplicates = editingTableAsset.FindDuplicateKeysAsObjects();
                if (duplicates != null && duplicates.Count > 0)
                {
                    var keysString = string.Join(", ", duplicates.Cast<object>().Take(5));
                    if (!EditorUtility.DisplayDialog("Warning",
                            $"Duplicate PrimaryKeys found: {keysString}\nDo you want to save?",
                            "Save", "Cancel"))
                        return;
                }
            }

            // クローンの変更を適用
            serializedTable.ApplyModifiedProperties();

            // 元のアセットにコピー
            ApplyChangesToOriginal();

            EditorUtility.DisplayDialog("Save Complete", $"Saved {selectedTable.name}", "OK");
        }

        private void ApplyChangesToOriginal()
        {
            if (selectedTable == null || editingClone == null)
                return;

            // 元の名前を保持
            var originalName = selectedTable.name;

            // EditorUtility.CopySerializedでクローンから元にコピー
            EditorUtility.CopySerialized(editingClone, selectedTable);

            // CopySerializedは名前もコピーするため、元の名前を復元
            selectedTable.name = originalName;

            EditorUtility.SetDirty(selectedTable);
            AssetDatabase.SaveAssetIfDirty(selectedTable);

            idDirty = false;
            lastRecordHash = CalculateRecordHash();
        }

        private void OnDisable()
        {
            // ドメインリロード前にクローンをクリーンアップ
            CleanupClone();
        }

        private void OnDestroy()
        {
            if (idDirty && selectedTable != null)
            {
                var result = EditorUtility.DisplayDialogComplex(
                    "Unsaved Changes",
                    $"{selectedTable.name} has unsaved changes. Do you want to save?",
                    "Save", "Discard", "Cancel");

                if (result == 0)
                    ApplyChangesToOriginal();
                // キャンセルの場合でもクリーンアップは行う（ウィンドウは既に閉じられるため）
            }

            CleanupClone();
        }
    }
}