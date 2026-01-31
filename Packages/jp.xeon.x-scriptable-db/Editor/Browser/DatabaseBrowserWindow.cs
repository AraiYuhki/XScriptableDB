using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// テーブル情報。
    /// </summary>
    public class TableInfo
    {
        public string Name { get; set; }
        public string AssetPath { get; set; }
        public ScriptableObject Asset { get; set; }
        public ITableAsset TableAsset { get; set; }
        public Type RecordType { get; set; }
        public int RecordCount { get; set; }
        public List<ColumnInfo> Columns { get; set; } = new();
    }

    /// <summary>
    /// カラム情報。
    /// </summary>
    public class ColumnInfo
    {
        public string Name { get; set; }
        public Type FieldType { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsSecondaryKey { get; set; }
        public string TypeDisplayName { get; set; }
    }

    /// <summary>
    /// データベースブラウザウィンドウ。
    /// </summary>
    public class DatabaseBrowserWindow : EditorWindow
    {
        private List<TableInfo> tables = new();
        private TableInfo selectedTable;
        private Vector2 tableListScrollPosition;
        private Vector2 tableInfoScrollPosition;
        private Vector2 dataPreviewScrollPosition;
        private string searchText = "";
        private float splitPosition = 250f;
        private bool isDraggingSplit;

        private GUIStyle headerStyle;
        private GUIStyle selectedItemStyle;
        private GUIStyle itemStyle;
        private bool stylesInitialized;

        private const int PreviewRowCount = 20;

        [MenuItem("Window/XScriptableDB/Database Browser")]
        public static void ShowWindow()
        {
            var window = GetWindow<DatabaseBrowserWindow>();
            window.titleContent = new GUIContent("DB Browser");
            window.minSize = new Vector2(800, 500);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshTables();
        }

        private void RefreshTables()
        {
            tables.Clear();

            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (asset is ITableAsset tableAsset)
                {
                    var tableInfo = CreateTableInfo(asset, tableAsset, path);
                    tables.Add(tableInfo);
                }
            }

            tables = tables.OrderBy(t => t.Name).ToList();
        }

        private TableInfo CreateTableInfo(ScriptableObject asset, ITableAsset tableAsset, string assetPath)
        {
            var recordType = tableAsset.RecordType;
            var columns = new List<ColumnInfo>();

            foreach (var field in recordType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var isPrimaryKey = field.GetCustomAttribute<PrimaryKeyAttribute>() != null;
                var isSecondaryKey = field.GetCustomAttribute<SecondaryKeyAttribute>() != null;

                columns.Add(new ColumnInfo
                {
                    Name = field.Name,
                    FieldType = field.FieldType,
                    IsPrimaryKey = isPrimaryKey,
                    IsSecondaryKey = isSecondaryKey,
                    TypeDisplayName = GetTypeDisplayName(field.FieldType)
                });
            }

            return new TableInfo
            {
                Name = asset.name,
                AssetPath = assetPath,
                Asset = asset,
                TableAsset = tableAsset,
                RecordType = recordType,
                RecordCount = tableAsset.Count,
                Columns = columns
            };
        }

        private string GetTypeDisplayName(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(long)) return "long";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var itemType = type.GetGenericArguments()[0];
                return $"List<{GetTypeDisplayName(itemType)}>";
            }
            if (type.IsArray)
            {
                return $"{GetTypeDisplayName(type.GetElementType())}[]";
            }
            return type.Name;
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                padding = new RectOffset(5, 5, 5, 5)
            };

            selectedItemStyle = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(10, 5, 3, 3)
            };
            selectedItemStyle.normal.background = MakeTexture(1, 1, new Color(0.24f, 0.49f, 0.91f, 0.5f));

            itemStyle = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(10, 5, 3, 3)
            };

            stylesInitialized = true;
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            var texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.BeginHorizontal();

            // 左パネル: テーブルリスト
            DrawTableList();

            // スプリッター
            DrawSplitter();

            // 右パネル: テーブル詳細
            DrawTableInfo();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTableList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(splitPosition));

            // ヘッダー
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField($"Tables ({tables.Count})", EditorStyles.boldLabel);
            if (GUILayout.Button("↻", EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                RefreshTables();
            }
            EditorGUILayout.EndHorizontal();

            // 検索バー
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            searchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? EditorStyles.toolbarButton, GUILayout.Width(18)))
            {
                searchText = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            // テーブルリスト
            tableListScrollPosition = EditorGUILayout.BeginScrollView(tableListScrollPosition);

            var filteredTables = string.IsNullOrEmpty(searchText)
                ? tables
                : tables.Where(t => t.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var table in filteredTables)
            {
                var isSelected = selectedTable == table;
                var style = isSelected ? selectedItemStyle : itemStyle;

                EditorGUILayout.BeginHorizontal(style);

                // テーブルアイコンと名前
                var displayName = $"📋 {table.Name} ({table.RecordCount})";
                if (GUILayout.Button(displayName, style, GUILayout.ExpandWidth(true)))
                {
                    selectedTable = table;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawSplitter()
        {
            var splitterRect = EditorGUILayout.GetControlRect(false, GUILayout.Width(5));
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
            {
                isDraggingSplit = true;
                Event.current.Use();
            }

            if (isDraggingSplit)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    splitPosition = Mathf.Clamp(Event.current.mousePosition.x, 150, position.width - 300);
                    Repaint();
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    isDraggingSplit = false;
                }
            }

            EditorGUI.DrawRect(splitterRect, new Color(0.2f, 0.2f, 0.2f));
        }

        private void DrawTableInfo()
        {
            EditorGUILayout.BeginVertical();

            if (selectedTable == null)
            {
                EditorGUILayout.HelpBox("Select a table from the list", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            tableInfoScrollPosition = EditorGUILayout.BeginScrollView(tableInfoScrollPosition);

            // テーブル名とアクション
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(selectedTable.Name, headerStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Open Editor", GUILayout.Width(90)))
            {
                OpenTableEditor();
            }
            if (GUILayout.Button("Select Asset", GUILayout.Width(90)))
            {
                Selection.activeObject = selectedTable.Asset;
                EditorGUIUtility.PingObject(selectedTable.Asset);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 基本情報
            EditorGUILayout.LabelField("Table Information", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Asset Path:", selectedTable.AssetPath);
            EditorGUILayout.LabelField("Record Type:", selectedTable.RecordType.FullName);
            EditorGUILayout.LabelField("Record Count:", selectedTable.RecordCount.ToString());
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // カラム情報
            DrawColumnsInfo();

            EditorGUILayout.Space(10);

            // データプレビュー
            DrawDataPreview();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawColumnsInfo()
        {
            EditorGUILayout.LabelField($"Columns ({selectedTable.Columns.Count})", EditorStyles.boldLabel);

            // テーブルヘッダー
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(120));
            EditorGUILayout.LabelField("Key", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            // カラム一覧
            foreach (var column in selectedTable.Columns)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(column.Name, GUILayout.Width(150));
                EditorGUILayout.LabelField(column.TypeDisplayName, GUILayout.Width(120));

                var keyLabel = "";
                if (column.IsPrimaryKey) keyLabel = "🔑 Primary";
                else if (column.IsSecondaryKey) keyLabel = "🔗 Secondary";

                EditorGUILayout.LabelField(keyLabel, GUILayout.Width(100));

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawDataPreview()
        {
            EditorGUILayout.LabelField($"Data Preview (First {PreviewRowCount} rows)", EditorStyles.boldLabel);

            if (selectedTable.RecordCount == 0)
            {
                EditorGUILayout.HelpBox("No records", MessageType.Info);
                return;
            }

            dataPreviewScrollPosition = EditorGUILayout.BeginScrollView(
                dataPreviewScrollPosition,
                GUILayout.Height(200));

            // ヘッダー
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            foreach (var column in selectedTable.Columns.Take(6)) // 最大6カラム表示
            {
                EditorGUILayout.LabelField(column.Name, EditorStyles.boldLabel, GUILayout.Width(100));
            }
            if (selectedTable.Columns.Count > 6)
            {
                EditorGUILayout.LabelField("...", GUILayout.Width(30));
            }
            EditorGUILayout.EndHorizontal();

            // データ行
            var rowCount = 0;
            foreach (var record in selectedTable.TableAsset.Records)
            {
                if (record == null) continue;
                if (rowCount >= PreviewRowCount) break;

                EditorGUILayout.BeginHorizontal();

                var colCount = 0;
                foreach (var column in selectedTable.Columns.Take(6))
                {
                    var value = GetFieldValue(record, column.Name);
                    var displayValue = FormatPreviewValue(value);
                    EditorGUILayout.LabelField(displayValue, GUILayout.Width(100));
                    colCount++;
                }

                if (selectedTable.Columns.Count > 6)
                {
                    EditorGUILayout.LabelField("...", GUILayout.Width(30));
                }

                EditorGUILayout.EndHorizontal();
                rowCount++;
            }

            EditorGUILayout.EndScrollView();
        }

        private object GetFieldValue(object record, string fieldName)
        {
            var field = selectedTable.RecordType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            return field?.GetValue(record);
        }

        private string FormatPreviewValue(object value)
        {
            if (value == null) return "(null)";

            var str = value.ToString();
            if (str.Length > 15)
            {
                return str.Substring(0, 12) + "...";
            }
            return str;
        }

        private void OpenTableEditor()
        {
            if (selectedTable?.Asset == null) return;

            // TableEditorWindowを開く
            var window = EditorWindow.GetWindow<DataEditorWindow>();
            window.Show();

            // TODO: 選択したテーブルを開く処理を追加
        }
    }
}
