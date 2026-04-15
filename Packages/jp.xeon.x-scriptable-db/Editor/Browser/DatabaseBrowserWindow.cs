using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Database browser window.
    /// </summary>
    public class DatabaseBrowserWindow : EditorWindow
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<Type, string> TypeNameDictionary = new()
        {
            { typeof(int), "int" },
            { typeof(long), "long" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(bool), "bool" },
            { typeof(string), "string" },
            { typeof(DateTime), "DateTime" },
            { typeof(SerializableDateTime), "DateTime" }
        };

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

        [MenuItem("Tools/XScriptableDB/Database Browser")]
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

            foreach (var field in recordType.GetFields(FieldFlags))
            {
                var isSerializable = field.GetCustomAttribute<SerializeField>() != null;
                var isPrimaryKey = field.GetCustomAttribute<PrimaryKeyAttribute>() != null;
                var isSecondaryKey = field.GetCustomAttributes<SecondaryKeyAttribute>().Any();

                // Skip private fields without SerializeField
                if (field.IsPrivate && !isSerializable)
                    continue;

                columns.Add(new ColumnInfo(field.Name, field.FieldType, isPrimaryKey, isSecondaryKey, GetTypeDisplayName(field.FieldType)));
            }

            return new TableInfo(asset.name, assetPath, asset, tableAsset, recordType, tableAsset.Count, columns);
        }

        private string GetTypeDisplayName(Type type)
        {
            if (TypeNameDictionary.TryGetValue(type, out var result))
                return result;
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

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // Left panel: table list
            DrawTableList();

            // Splitter
            DrawSplitter();

            // Right panel: table details
            DrawTableInfo();

            EditorGUILayout.EndHorizontal();

            // Repaint while dragging the splitter
            if (isDraggingSplit)
                Repaint();
        }

        private void DrawTableList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(splitPosition));

            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField($"Tables ({tables.Count})", EditorStyles.boldLabel);
            if (GUILayout.Button("↻", EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                RefreshTables();
            }
            EditorGUILayout.EndHorizontal();

            // Search bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            searchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? EditorStyles.toolbarButton, GUILayout.Width(18)))
            {
                searchText = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            // Table list
            tableListScrollPosition = EditorGUILayout.BeginScrollView(tableListScrollPosition);

            var filteredTables = string.IsNullOrEmpty(searchText)
                ? tables
                : tables.Where(t => t.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var table in filteredTables)
            {
                var isSelected = selectedTable == table;
                var bgColor = isSelected ? new Color(0.24f, 0.49f, 0.91f, 0.5f) : Color.clear;

                var rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(22));

                // Draw background
                if (isSelected)
                    EditorGUI.DrawRect(rect, bgColor);

                // Table name
                var displayName = $"  {table.Name} ({table.RecordCount})";
                EditorGUILayout.LabelField(displayName, GUILayout.ExpandWidth(true));

                EditorGUILayout.EndHorizontal();

                // Detect click
                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    selectedTable = table;
                    Event.current.Use();
                    Repaint();
                }
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
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (selectedTable == null)
            {
                EditorGUILayout.HelpBox("Select a table from the list", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            tableInfoScrollPosition = EditorGUILayout.BeginScrollView(tableInfoScrollPosition, GUILayout.ExpandWidth(true));

            // Table name and actions
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

            // Basic information
            EditorGUILayout.LabelField("Table Information", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Asset Path:", selectedTable.AssetPath);
            EditorGUILayout.LabelField("Record Type:", selectedTable.RecordType.FullName);
            EditorGUILayout.LabelField("Record Count:", selectedTable.RecordCount.ToString());
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // Column information
            DrawColumnsInfo();

            EditorGUILayout.Space(10);

            // Data preview
            DrawDataPreview();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawColumnsInfo()
        {
            EditorGUILayout.LabelField($"Columns ({selectedTable.Columns.Count})", EditorStyles.boldLabel);

            if (selectedTable.Columns.Count == 0)
            {
                EditorGUILayout.HelpBox("No columns found", MessageType.Warning);
                return;
            }

            // Table header
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(120));
            EditorGUILayout.LabelField("Key", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Column list
            foreach (var column in selectedTable.Columns)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(column.Name, GUILayout.Width(150));
                EditorGUILayout.LabelField(column.TypeDisplayName, GUILayout.Width(120));

                var keyLabel = "";
                if (column.IsPrimaryKey)
                    keyLabel = "PK";
                else if (column.IsSecondaryKey)
                    keyLabel = "SK";

                EditorGUILayout.LabelField(keyLabel, GUILayout.Width(100));
                GUILayout.FlexibleSpace();

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
                GUILayout.Height(220), GUILayout.ExpandWidth(true));

            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            foreach (var column in selectedTable.Columns.Take(6)) // Show up to 6 columns
            {
                EditorGUILayout.LabelField(column.Name, EditorStyles.boldLabel, GUILayout.Width(100));
            }
            if (selectedTable.Columns.Count > 6)
            {
                EditorGUILayout.LabelField("...", GUILayout.Width(30));
            }
            EditorGUILayout.EndHorizontal();

            // Data rows
            var rowCount = 0;
            foreach (var record in selectedTable.TableAsset.Records)
            {
                if (record == null)
                    continue;
                if (rowCount >= PreviewRowCount)
                    break;

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
            var field = selectedTable.RecordType.GetField(fieldName, FieldFlags);
            return field?.GetValue(record);
        }

        private string FormatPreviewValue(object value)
        {
            if (value == null)
                return "(null)";

            if (value is DateTime dt)
                return DateTimeEditorUtility.FormatDateTime(dt);

            if (value is SerializableDateTime sdt)
                return DateTimeEditorUtility.FormatDateTime(sdt.DateTime);

            var str = value.ToString();
            if (str.Length > 15)
                return str.Substring(0, 12) + "...";

            return str;
        }

        private void OpenTableEditor()
        {
            if (selectedTable?.Asset == null) return;

            // Open the DataEditorWindow
            var window = EditorWindow.GetWindow<DataEditorWindow>();
            window.Show();

            // TODO: Add logic to open the selected table
        }
    }
}
