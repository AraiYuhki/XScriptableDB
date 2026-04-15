using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Xeon.XScriptableDB.Validation;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Validation window.
    /// </summary>
    public class ValidationWindow : EditorWindow
    {
        private List<TableValidationEntry> tableEntries = new();
        private Vector2 tableListScrollPosition;
        private Vector2 resultScrollPosition;
        private TableValidationEntry selectedEntry;
        private bool isValidating;

        private GUIStyle errorStyle;
        private GUIStyle warningStyle;
        private GUIStyle successStyle;
        private bool stylesInitialized;

        [MenuItem("Tools/XScriptableDB/Validation")]
        public static void ShowWindow()
        {
            var window = GetWindow<ValidationWindow>();
            window.titleContent = new GUIContent("Validation");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshTables();
        }

        private void RefreshTables()
        {
            tableEntries.Clear();

            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (asset is ITableAsset tableAsset)
                {
                    tableEntries.Add(new TableValidationEntry
                    {
                        TableName = asset.name,
                        AssetPath = path,
                        Asset = asset,
                        TableAsset = tableAsset
                    });
                }
            }

            tableEntries = tableEntries.OrderBy(t => t.TableName).ToList();
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            errorStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(1f, 0.4f, 0.4f) }
            };

            warningStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(1f, 0.8f, 0.2f) }
            };

            successStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.4f, 0.8f, 0.4f) }
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.BeginVertical();

            // Toolbar
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();

            // Left panel: table list
            DrawTableList();

            // Right panel: result details
            DrawResultDetails();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshTables();
            }

            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(isValidating);
            if (GUILayout.Button("Validate All", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ValidateAllTables();
            }

            if (GUILayout.Button("Validate Selected", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                ValidateSelectedTables();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();

            // Summary
            var totalErrors = tableEntries.Sum(t => t.Result?.TotalErrorCount ?? 0);
            var validatedCount = tableEntries.Count(t => t.Result != null);
            EditorGUILayout.LabelField($"Validated: {validatedCount}/{tableEntries.Count} | Errors: {totalErrors}",
                EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTableList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));

            EditorGUILayout.LabelField("Tables", EditorStyles.boldLabel);

            tableListScrollPosition = EditorGUILayout.BeginScrollView(tableListScrollPosition);

            foreach (var entry in tableEntries)
            {
                DrawTableEntry(entry);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawTableEntry(TableValidationEntry entry)
        {
            var isSelected = selectedEntry == entry;
            var bgColor = isSelected ? new Color(0.24f, 0.49f, 0.91f, 0.3f) : Color.clear;

            var rect = EditorGUILayout.BeginHorizontal();
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, bgColor);
            }

            // Checkbox
            entry.IsSelected = EditorGUILayout.Toggle(entry.IsSelected, GUILayout.Width(20));

            // Status icon
            var statusIcon = GetStatusIcon(entry);
            EditorGUILayout.LabelField(statusIcon, GUILayout.Width(20));

            // Table name
            if (GUILayout.Button(entry.TableName, EditorStyles.label))
            {
                selectedEntry = entry;
            }

            // Error count
            if (entry.Result != null && entry.Result.TotalErrorCount > 0)
            {
                EditorGUILayout.LabelField($"({entry.Result.TotalErrorCount})", errorStyle, GUILayout.Width(40));
            }

            EditorGUILayout.EndHorizontal();
        }

        private string GetStatusIcon(TableValidationEntry entry)
        {
            if (entry.Result == null) return "○";  // Not validated
            if (entry.Result.IsValid) return "✓";   // Passed
            return "✗";  // Error
        }

        private void DrawResultDetails()
        {
            EditorGUILayout.BeginVertical();

            if (selectedEntry == null)
            {
                EditorGUILayout.HelpBox("Select a table to view validation results", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(selectedEntry.TableName, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Validate", GUILayout.Width(70)))
            {
                ValidateTable(selectedEntry);
            }
            if (GUILayout.Button("Select Asset", GUILayout.Width(80)))
            {
                Selection.activeObject = selectedEntry.Asset;
                EditorGUIUtility.PingObject(selectedEntry.Asset);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Results
            if (selectedEntry.Result == null)
            {
                EditorGUILayout.HelpBox("Not validated yet. Click 'Validate' to check.", MessageType.Info);
            }
            else if (selectedEntry.Result.IsValid)
            {
                EditorGUILayout.HelpBox("Validation passed. No errors found.", MessageType.Info);
            }
            else
            {
                DrawValidationErrors(selectedEntry.Result);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawValidationErrors(TableValidationResult result)
        {
            EditorGUILayout.LabelField($"Errors: {result.TotalErrorCount}", errorStyle);

            resultScrollPosition = EditorGUILayout.BeginScrollView(resultScrollPosition);

            // Table-level errors
            if (result.TableLevelErrors.Count > 0)
            {
                EditorGUILayout.LabelField("Table-level Errors:", EditorStyles.boldLabel);
                foreach (var error in result.TableLevelErrors)
                {
                    DrawError(error);
                }
                EditorGUILayout.Space(10);
            }

            // Record-level errors
            var recordsWithErrors = result.RecordResults.Where(r => !r.IsValid).ToList();
            DrawRecordErrors(recordsWithErrors);

            EditorGUILayout.EndScrollView();
        }

        private void DrawRecordErrors(List<RecordValidationResult> recordsWithErrors)
        {
            if (recordsWithErrors.Count <= 0)
                return;
            EditorGUILayout.LabelField($"Record Errors ({recordsWithErrors.Count} records):", EditorStyles.boldLabel);

            foreach (var recordResult in recordsWithErrors)
            {
                var keyStr = recordResult.RecordKey?.ToString() ?? $"Index {recordResult.RecordIndex}";
                EditorGUILayout.LabelField($"  [{keyStr}]", EditorStyles.miniBoldLabel);

                foreach (var error in recordResult.Errors)
                {
                    EditorGUI.indentLevel++;
                    DrawError(error);
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawError(ValidationError error)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"• {error.FieldName}:", GUILayout.Width(120));
            EditorGUILayout.LabelField(error.Message, errorStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void ValidateAllTables()
        {
            isValidating = true;

            try
            {
                foreach (var entry in tableEntries)
                {
                    ValidateTable(entry);
                }
            }
            finally
            {
                isValidating = false;
            }

            Repaint();
        }

        private void ValidateSelectedTables()
        {
            isValidating = true;

            try
            {
                foreach (var entry in tableEntries.Where(t => t.IsSelected))
                {
                    ValidateTable(entry);
                }
            }
            finally
            {
                isValidating = false;
            }

            Repaint();
        }

        private void ValidateTable(TableValidationEntry entry)
        {
            var tableAsset = entry.TableAsset;
            var recordType = tableAsset.RecordType;

            // Call ValidateTable via reflection
            var method = typeof(RecordValidator)
                .GetMethod("ValidateTable")
                .MakeGenericMethod(recordType);

            // Create key selector
            var keySelector = CreateKeySelector(recordType);

            entry.Result = method.Invoke(null, new object[] { tableAsset, keySelector }) as TableValidationResult;
        }

        private Delegate CreateKeySelector(Type recordType)
        {
            // Search for PrimaryKey field
            var keyField = recordType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(f => f.GetCustomAttribute<PrimaryKeyAttribute>() != null);

            if (keyField == null) return null;

            // Create Func<T, object>
            var funcType = typeof(Func<,>).MakeGenericType(recordType, typeof(object));

            // Simply return null (key selector is optional)
            return null;
        }

        private class TableValidationEntry
        {
            public string TableName;
            public string AssetPath;
            public ScriptableObject Asset;
            public ITableAsset TableAsset;
            public TableValidationResult Result;
            public bool IsSelected;
        }
    }
}
