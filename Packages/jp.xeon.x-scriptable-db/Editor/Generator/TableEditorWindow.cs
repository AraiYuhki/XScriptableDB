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
        private ReorderableList indeciesView;
        private string[] columnsCache;

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("新規作成"))
                {
                    tableDefinition = new TableDefinition();
                    CreateColumnView();
                    CreateIndeciesView();
                }

                if (GUILayout.Button("YAML読み込み"))
                {
                    LoadYaml();
                }
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
            if (indeciesView == null)
                CreateIndeciesView();
            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                columnView.DoLayoutList();
                indeciesView.DoLayoutList();
                scrollPosition = scrollView.scrollPosition;
            }
        }

        private void Validate()
        {
            if (tableDefinition.Columns.Count(column => column.IsPrimaryKey) > 1)
                EditorGUILayout.HelpBox("プライマリーキーは必ず一つ設定してください", MessageType.Error);
            if (columnsCache.Length != columnsCache.Distinct().Count())
                EditorGUILayout.HelpBox("同名のカラムは作成できません", MessageType.Error);
            if (tableDefinition.Indecies.Count != tableDefinition.Indecies.Distinct().Count())
                EditorGUILayout.HelpBox("同じカラムを複数のインデックスに指定できません", MessageType.Error);

        }

        private void LoadYaml()
        {
            var filePath = EditorUtility.OpenFilePanel("Select open Table definition YAML file", Path.Combine(Application.dataPath, "../"), "yaml,yml");
            if (string.IsNullOrEmpty(filePath))
                return;
            this.filePath = filePath;
            tableDefinition = DefinitionLoader.LoadDefinition(filePath);
            CreateColumnView();
            CreateIndeciesView();
        }

        private void SaveAs()
        {
            var defaultPath = string.IsNullOrEmpty(filePath) ? "NewTable" : filePath;
            var saveFilePath = EditorUtility.SaveFilePanel("YAMLファイルの保存先を選択してください", Path.Combine(Application.dataPath, "../"), defaultPath, "yaml,yml");
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
            var tableFilePath = Path.Join(Application.dataPath, savePath, tableDefinition.TableName.ToCamelCase() + "Table.cs");
            var recordFilePath = Path.Join(Application.dataPath, savePath, tableDefinition.TableName.ToCamelCase() + "Record.cs");

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

        private void CreateIndeciesView()
        {
            indeciesView = new ReorderableList(tableDefinition.Indecies, typeof(string));
            indeciesView.drawElementCallback = DrawIndex;
            indeciesView.drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"Indecies({tableDefinition.Indecies.Count})");
            UpdateColumnsCache();
        }

        private void OnAddColumn(ReorderableList list)
        {
            tableDefinition.Columns.Add(new ColumnDefinition { Name = "NewColumn", Type = "string" });
            UpdateColumnsCache();
        }

        private void OnRemoveColumn(ReorderableList list)
        {
            if (list.index >= 0 && list.index < tableDefinition.Columns.Count)
                tableDefinition.Columns.RemoveAt(list.index);
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
            if (columnsCache == null || columnsCache.Length == 0)
            {
                EditorGUI.LabelField(rect, "No columns");
                if (tableDefinition.Indecies[index] != string.Empty)
                    tableDefinition.Indecies[index] = string.Empty;
                return;
            }

            var selectedIndex = System.Array.IndexOf(columnsCache, tableDefinition.Indecies[index]);
            if (selectedIndex < 0)
                selectedIndex = 0;
            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUI.Popup(rect, selectedIndex, columnsCache);
            if (EditorGUI.EndChangeCheck())
                tableDefinition.Indecies[index] = columnsCache[selectedIndex];

        }

        private void DrawColumn(Rect rect, int index, bool isActive, bool isFocused)
        {
            var column = tableDefinition.Columns[index];
            var lineHeight = EditorGUIUtility.singleLineHeight;
            // center vertically
            var y = rect.y + (rect.height - lineHeight) / 2f;
            var padding = 6f;

            // layout: Name | Type | Nullable | PrimaryKey
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

            // Draw fields without labels to keep a compact horizontal layout
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
