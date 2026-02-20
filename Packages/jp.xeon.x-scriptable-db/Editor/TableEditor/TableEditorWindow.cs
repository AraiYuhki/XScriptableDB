using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    public class TableEditorWindow : EditorWindow
    {
        [MenuItem("Tools/XScriptableDB/Table Editor")]
        public static void Open() => GetWindow<TableEditorWindow>("Table Editor");

        private string filePath = string.Empty;
        private TableDefinition tableDefinition;
        private Vector2 scrollPosition;
        private ReorderableList columnView;
        private ReorderableList indicesView;
        private string[] columnsCache;

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("新規作成"))
                {
                    tableDefinition = new TableDefinition();
                    CreateColumnView();
                    CreateIndicesView();
                }

                if (GUILayout.Button("YAML読み込み"))
                    LoadYaml();
            }

            if (tableDefinition == null)
            {
                EditorGUILayout.HelpBox("編集するファイルを選択するか、新規作成してください", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("名前を付けて保存"))
                    SaveAs();
                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(filePath));
                if (GUILayout.Button("上書き保存"))
                    OverrideSave();

                if (GUILayout.Button("C#ファイル生成"))
                    GenerateFiles();
                EditorGUI.EndDisabledGroup();
            }

            Validate();

            using (new EditorGUILayout.HorizontalScope())
            {
                tableDefinition.TableName = EditorGUILayout.TextField("テーブル名", tableDefinition.TableName);
                tableDefinition.IsReadOnly = EditorGUILayout.ToggleLeft("読み取り専用", tableDefinition.IsReadOnly, GUILayout.Width(100f));
            }
            if (columnView == null)
                CreateColumnView();
            if (indicesView == null)
                CreateIndicesView();
            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                columnView.DoLayoutList();
                indicesView.DoLayoutList();
                scrollPosition = scrollView.scrollPosition;
            }
        }

        private void Validate()
        {
            if (tableDefinition.Columns.Count(column => column.IsPrimaryKey) > 1)
                EditorGUILayout.HelpBox("プライマリーキーは必ず一つ設定してください", MessageType.Error);
            if (columnsCache != null && columnsCache.Length != columnsCache.Distinct().Count())
                EditorGUILayout.HelpBox("同名のカラムは作成できません", MessageType.Error);
            var indexNames = tableDefinition.Indices.Select(idx => idx.Name).ToList();
            if (indexNames.Count != indexNames.Distinct().Count())
                EditorGUILayout.HelpBox("同名のインデックスは作成できません", MessageType.Error);
        }

        private void LoadYaml()
        {
            var filePath = EditorUtility.OpenFilePanel("Select open Table definition YAML file", Path.Combine(Application.dataPath, "../"), "yaml,yml");
            if (string.IsNullOrEmpty(filePath))
                return;
            this.filePath = filePath;
            tableDefinition = DefinitionLoader.LoadDefinition(filePath);
            CreateColumnView();
            CreateIndicesView();
        }

        private void SaveAs()
        {
            var defaultFileName = string.IsNullOrEmpty(filePath) ? "NewTable" : Path.GetFileNameWithoutExtension(filePath);
            var saveFilePath = EditorUtility.SaveFilePanel("YAMLファイルの保存先を選択してください", Path.Combine(Application.dataPath, "../"), defaultFileName, "yaml");
            if (string.IsNullOrEmpty(saveFilePath))
                return;
            filePath = saveFilePath;
            DefinitionLoader.ExportYAML(tableDefinition, saveFilePath);
            EditorUtility.DisplayDialog("ファイルを保存しました", $"{saveFilePath}に保存しました", "OK");
        }

        private void OverrideSave()
        {
            if (string.IsNullOrEmpty(filePath))
                return;
            DefinitionLoader.ExportYAML(tableDefinition, filePath);
            EditorUtility.DisplayDialog("ファイルを保存しました", $"{filePath}に上書き保存しました", "OK");
        }

        private void GenerateFiles()
        {
            var savePath = TableGenerateSetting.Instance.SavePath;
            var tableFilePath = Path.Join(Application.dataPath, savePath, tableDefinition.TableName.SnakeToPascalCase() + "Table.cs");
            var recordFilePath = Path.Join(Application.dataPath, savePath, tableDefinition.TableName.SnakeToPascalCase() + "Record.cs");

            var directoryInfo = new DirectoryInfo(Path.Join(Application.dataPath, savePath));
            if (!directoryInfo.Exists)
                directoryInfo.Create();

            File.WriteAllText(tableFilePath, ClassGenerator.GenerateTable(tableDefinition));
            File.WriteAllText(recordFilePath, ClassGenerator.GenerateRecord(tableDefinition));
            EditorUtility.DisplayDialog("ファイルの生成完了", $"以下のファイルを生成しました\n{tableFilePath}\n{recordFilePath}", "OK");
            AssetDatabase.ImportAsset(tableFilePath.Replace(Application.dataPath, "Assets"));
            AssetDatabase.ImportAsset(recordFilePath.Replace(Application.dataPath, "Assets"));
            AssetDatabase.Refresh();
        }

        private void CreateColumnView()
        {
            columnView = new ReorderableList(tableDefinition.Columns, typeof(ColumnDefinition));
            columnView.drawElementCallback = DrawColumn;
            columnView.drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"Columns({tableDefinition.Columns.Count})");
            columnView.onAddCallback = OnAddColumn;
            columnView.onRemoveCallback = OnRemoveColumn;
            columnView.onReorderCallback = _ => UpdateColumnsCache();
            columnView.onChangedCallback = _ => UpdateColumnsCache();
            UpdateColumnsCache();
        }

        private void CreateIndicesView()
        {
            indicesView = new ReorderableList(tableDefinition.Indices, typeof(IndexDefinition));
            indicesView.drawElementCallback = DrawIndex;
            indicesView.elementHeightCallback = GetIndexElementHeight;
            indicesView.drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"Indices({tableDefinition.Indices.Count})");
            indicesView.onAddCallback = OnAddIndex;
            UpdateColumnsCache();
        }

        private void OnAddIndex(ReorderableList list)
        {
            var defaultColumn = columnsCache?.Length > 0 ? columnsCache[0] : "";
            var newIndex = new IndexDefinition("NewIndex")
            {
                Columns = new System.Collections.Generic.List<string> { defaultColumn }
            };
            tableDefinition.Indices.Add(newIndex);
        }

        private float GetIndexElementHeight(int index)
        {
            var indexDef = tableDefinition.Indices[index];
            var lineHeight = EditorGUIUtility.singleLineHeight + 2f;
            return lineHeight * (2 + indexDef.Columns.Count);
        }

        private void OnAddColumn(ReorderableList list)
        {
            tableDefinition.Columns.Add(new ColumnDefinition { Name = "NewColumn", Type = "string" });
            UpdateColumnsCache();
        }

        private void OnRemoveColumn(ReorderableList list)
        {
            if (list.index < 0 || list.index >= tableDefinition.Columns.Count)
                return;

            var removedColumnName = tableDefinition.Columns[list.index].Name;
            tableDefinition.Columns.RemoveAt(list.index);

            foreach (var indexDef in tableDefinition.Indices)
                indexDef.Columns.Remove(removedColumnName);

            UpdateColumnsCache();
        }

        private void UpdateColumnsCache()
        {
            if (tableDefinition == null || tableDefinition.Columns == null)
            {
                columnsCache = new string[0];
                return;
            }
            columnsCache = tableDefinition.Columns.Select(c => c.Name ?? string.Empty).ToArray();
        }

        private void DrawIndex(Rect rect, int index, bool isActive, bool isFocused)
        {
            var indexDef = tableDefinition.Indices[index];
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var padding = 2f;
            var y = rect.y + padding;

            var nameRect = new Rect(rect.x, y, rect.width * 0.6f, lineHeight);
            var duplicateRect = new Rect(rect.x + rect.width * 0.65f, y, rect.width * 0.35f, lineHeight);
            indexDef.Name = EditorGUI.TextField(nameRect, "Name", indexDef.Name);
            indexDef.AllowDuplicates = EditorGUI.ToggleLeft(duplicateRect, "AllowDuplicates", indexDef.AllowDuplicates);
            y += lineHeight + padding;

            EditorGUI.LabelField(new Rect(rect.x, y, 60f, lineHeight), "Columns:");
            var addButtonRect = new Rect(rect.x + 65f, y, 20f, lineHeight);
            if (GUI.Button(addButtonRect, "+"))
                indexDef.Columns.Add(columnsCache?.Length > 0 ? columnsCache[0] : "");
            y += lineHeight + padding;

            if (columnsCache == null || columnsCache.Length == 0)
            {
                EditorGUI.LabelField(new Rect(rect.x + 20f, y, rect.width - 20f, lineHeight), "No columns defined");
                return;
            }

            for (var i = 0; i < indexDef.Columns.Count; i++)
            {
                var colRect = new Rect(rect.x + 20f, y, rect.width - 60f, lineHeight);
                var removeRect = new Rect(rect.x + rect.width - 35f, y, 20f, lineHeight);

                var selectedIdx = System.Array.IndexOf(columnsCache, indexDef.Columns[i]);
                if (selectedIdx < 0)
                    selectedIdx = 0;
                EditorGUI.BeginChangeCheck();
                selectedIdx = EditorGUI.Popup(colRect, $"[{i}]", selectedIdx, columnsCache);
                if (EditorGUI.EndChangeCheck())
                    indexDef.Columns[i] = columnsCache[selectedIdx];

                if (GUI.Button(removeRect, "-") && indexDef.Columns.Count > 1)
                {
                    indexDef.Columns.RemoveAt(i);
                    break;
                }

                y += lineHeight + padding;
            }
        }

        private void DrawColumn(Rect rect, int index, bool isActive, bool isFocused)
        {
            var column = tableDefinition.Columns[index];
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var y = rect.y + (rect.height - lineHeight) / 2f;
            var padding = 6f;

            var totalWidth = rect.width;
            var nameWidth = totalWidth * 0.35f;
            var typeWidth = totalWidth * 0.35f;
            var nullableWidth = totalWidth * 0.15f;
            var primaryKeyWidth = totalWidth - (nameWidth + typeWidth + nullableWidth) - padding * 3f;

            var x = rect.x;
            var nameRect = new Rect(x, y, nameWidth, lineHeight);
            x += nameWidth + padding;
            var typeRect = new Rect(x, y, typeWidth, lineHeight);
            x += typeWidth + padding;
            var nullableRect = new Rect(x, y, nullableWidth, lineHeight);
            x += nullableWidth + padding;
            var primaryKeyRect = new Rect(x, y, primaryKeyWidth, lineHeight);

            EditorGUI.BeginChangeCheck();
            var newName = EditorGUI.TextField(nameRect, column.Name);
            if (EditorGUI.EndChangeCheck())
            {
                column.Name = newName;
                UpdateColumnsCache();
            }

            column.Type = EditorGUI.TextField(typeRect, column.Type);
            column.IsNullable = EditorGUI.ToggleLeft(nullableRect, "Nullable", column.IsNullable);
            column.IsPrimaryKey = EditorGUI.ToggleLeft(primaryKeyRect, "PK", column.IsPrimaryKey);
        }
    }
}
