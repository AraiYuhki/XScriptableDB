using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SQLエディターウィンドウ。
    /// </summary>
    public class SqlEditorWindow : EditorWindow
    {
        private const BindingFlags FieldBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private string sqlText = "SELECT * FROM ";
        private Vector2 sqlScrollPosition;
        private Vector2 resultScrollPosition;
        private SqlExecutor executor;
        private SqlQueryResult lastResult;
        private List<ITableAsset> registeredTables = new();

        private GUIStyle sqlInputStyle;
        private GUIStyle resultHeaderStyle;
        private GUIStyle resultCellStyle;
        private GUIStyle errorStyle;
        private bool stylesInitialized;

        // 結果表示の設定
        private const int MaxDisplayRows = 1000;
        private const int ColumnMinWidth = 80;
        private const int ColumnMaxWidth = 300;

        // SQL履歴
        private List<string> sqlHistory = new();
        private int historyIndex = -1;
        private const int MaxHistoryCount = 50;

        [MenuItem("Tools/XScriptableDB/SQL Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<SqlEditorWindow>();
            window.titleContent = new GUIContent("SQL Editor");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            executor = new SqlExecutor();
            RefreshTables();
        }

        private void RefreshTables()
        {
            registeredTables.Clear();

            // プロジェクト内のTableAssetを検索
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (asset is ITableAsset tableAsset)
                {
                    var tableName = asset.name;
                    executor.RegisterTable(tableName, tableAsset);
                    registeredTables.Add(tableAsset);
                }
            }
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            sqlInputStyle = new GUIStyle(EditorStyles.textArea)
            {
                font = GetMonospaceFont(),
                fontSize = 13,
                wordWrap = false,
                padding = new RectOffset(8, 8, 8, 8)
            };

            resultHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(4, 4, 2, 2)
            };
            resultHeaderStyle.normal.background = MakeTexture(1, 1, new Color(0.2f, 0.2f, 0.2f));

            resultCellStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(4, 4, 2, 2),
                wordWrap = false
            };

            errorStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(1f, 0.4f, 0.4f) },
                wordWrap = true
            };

            stylesInitialized = true;
        }

        private Font GetMonospaceFont()
        {
            // システムの等幅フォントを取得（あれば）
            return null; // デフォルトフォントを使用
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

            EditorGUILayout.BeginVertical();

            // ツールバー
            DrawToolbar();

            // SQL入力エリア
            DrawSqlInput();

            // 実行ボタン
            DrawExecuteButton();

            // 結果表示エリア
            DrawResults();

            EditorGUILayout.EndVertical();

            // キーボードショートカット
            HandleKeyboardShortcuts();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh Tables", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                RefreshTables();
            }

            GUILayout.Space(10);

            // テーブル選択ドロップダウン
            if (GUILayout.Button("Insert Table", EditorStyles.toolbarDropDown, GUILayout.Width(100)))
            {
                ShowTableMenu();
            }

            GUILayout.FlexibleSpace();

            // 履歴ボタン
            EditorGUI.BeginDisabledGroup(historyIndex <= 0);
            if (GUILayout.Button("◀", EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                NavigateHistory(-1);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(historyIndex >= sqlHistory.Count - 1);
            if (GUILayout.Button("▶", EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                NavigateHistory(1);
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                sqlText = "";
                lastResult = null;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ShowTableMenu()
        {
            var menu = new GenericMenu();

            foreach (var tableName in executor.TableNames.OrderBy(n => n))
            {
                var name = tableName;
                menu.AddItem(new GUIContent(name), false, () =>
                {
                    InsertTableName(name);
                });
            }

            if (!executor.TableNames.Any())
            {
                menu.AddDisabledItem(new GUIContent("No tables found"));
            }

            menu.ShowAsContext();
        }

        private void InsertTableName(string tableName)
        {
            // カーソル位置にテーブル名を挿入（簡易実装）
            sqlText += tableName;
            Repaint();
        }

        private void DrawSqlInput()
        {
            EditorGUILayout.LabelField("SQL Query:", EditorStyles.boldLabel);

            var height = Mathf.Min(150, Mathf.Max(60, sqlText.Split('\n').Length * 18 + 20));

            sqlScrollPosition = EditorGUILayout.BeginScrollView(sqlScrollPosition, GUILayout.Height(height));

            EditorGUI.BeginChangeCheck();
            sqlText = EditorGUILayout.TextArea(sqlText, sqlInputStyle, GUILayout.ExpandHeight(true));
            if (EditorGUI.EndChangeCheck())
            {
                // テキスト変更時の処理
            }

            EditorGUILayout.EndScrollView();

            // テーブル一覧を表示
            if (executor.TableNames.Any())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Available tables:", GUILayout.Width(100));
                EditorGUILayout.LabelField(string.Join(", ", executor.TableNames.OrderBy(n => n)),
                    EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawExecuteButton()
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fixedHeight = 28,
                fixedWidth = 120
            };

            if (GUILayout.Button("Execute (F5)", buttonStyle))
            {
                ExecuteQuery();
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
        }

        private void DrawResults()
        {
            EditorGUILayout.LabelField("Results:", EditorStyles.boldLabel);

            if (lastResult == null)
            {
                EditorGUILayout.HelpBox("Enter a SQL query and press Execute or F5", MessageType.Info);
                return;
            }

            // ステータスバー
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            if (lastResult.IsSuccess)
            {
                var statusText = lastResult.Statement switch
                {
                    SelectStatement => $"{lastResult.Records.Count} rows returned",
                    UpdateStatement or DeleteStatement => $"{lastResult.AffectedCount} rows affected",
                    _ => "Executed"
                };
                EditorGUILayout.LabelField($"✓ {statusText} ({lastResult.ExecutionTimeMs:F2}ms)",
                    EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField($"✗ Error", errorStyle);
            }
            EditorGUILayout.EndHorizontal();

            // エラー表示
            if (!lastResult.IsSuccess)
            {
                EditorGUILayout.HelpBox(lastResult.ErrorMessage, MessageType.Error);
                return;
            }

            // SELECT結果の表示
            if (lastResult.Statement is SelectStatement && lastResult.Records.Count > 0)
            {
                DrawSelectResults();
            }
        }

        private void DrawSelectResults()
        {
            var records = lastResult.Records;
            var columnNames = lastResult.ColumnNames;

            if (columnNames.Count == 0 && records.Count > 0)
            {
                // カラム名がない場合はレコードから取得
                columnNames = SqlResultFormatter.GetColumnNamesFromRecord(records[0]);
            }

            // カラム幅の計算
            var columnWidths = CalculateColumnWidths(columnNames, records);

            resultScrollPosition = EditorGUILayout.BeginScrollView(resultScrollPosition);

            // ヘッダー行
            EditorGUILayout.BeginHorizontal();
            for (var i = 0; i < columnNames.Count; i++)
            {
                EditorGUILayout.LabelField(columnNames[i], resultHeaderStyle, GUILayout.Width(columnWidths[i]));
            }
            EditorGUILayout.EndHorizontal();

            // データ行
            var displayCount = Math.Min(records.Count, MaxDisplayRows);
            var recordType = records.Count > 0 ? records[0].GetType() : null;

            for (var row = 0; row < displayCount; row++)
            {
                var record = records[row];
                EditorGUILayout.BeginHorizontal();

                for (var col = 0; col < columnNames.Count; col++)
                {
                    var value = SqlResultFormatter.GetFieldValue(record, recordType, columnNames[col]);
                    var displayValue = SqlResultFormatter.FormatValue(value);
                    EditorGUILayout.LabelField(displayValue, resultCellStyle, GUILayout.Width(columnWidths[col]));
                }

                EditorGUILayout.EndHorizontal();
            }

            if (records.Count > MaxDisplayRows)
            {
                EditorGUILayout.HelpBox($"Showing {MaxDisplayRows} of {records.Count} rows", MessageType.Warning);
            }

            EditorGUILayout.EndScrollView();
        }

        private float[] CalculateColumnWidths(List<string> columnNames, List<object> records)
        {
            var widths = new float[columnNames.Count];

            for (var i = 0; i < columnNames.Count; i++)
            {
                // ヘッダーの幅
                widths[i] = GUI.skin.label.CalcSize(new GUIContent(columnNames[i])).x + 20;
            }

            // データの幅をサンプリング
            var sampleCount = Math.Min(100, records.Count);
            var recordType = records.Count > 0 ? records[0].GetType() : null;

            for (var row = 0; row < sampleCount; row++)
            {
                var record = records[row];
                for (var col = 0; col < columnNames.Count; col++)
                {
                    var value = SqlResultFormatter.GetFieldValue(record, recordType, columnNames[col]);
                    var displayValue = SqlResultFormatter.FormatValue(value);
                    var width = GUI.skin.label.CalcSize(new GUIContent(displayValue)).x + 20;
                    widths[col] = Mathf.Max(widths[col], width);
                }
            }

            // 最小・最大幅を適用
            for (var i = 0; i < widths.Length; i++)
            {
                widths[i] = Mathf.Clamp(widths[i], ColumnMinWidth, ColumnMaxWidth);
            }

            return widths;
        }

        private void ExecuteQuery()
        {
            if (string.IsNullOrWhiteSpace(sqlText))
                return;

            lastResult = executor.Execute(sqlText);

            // 履歴に追加
            AddToHistory(sqlText);

            Repaint();
        }

        private void AddToHistory(string sql)
        {
            // 重複を削除
            sqlHistory.RemoveAll(s => s.Equals(sql, StringComparison.OrdinalIgnoreCase));

            // 先頭に追加
            sqlHistory.Add(sql);

            // 最大数を超えたら古いものを削除
            while (sqlHistory.Count > MaxHistoryCount)
            {
                sqlHistory.RemoveAt(0);
            }

            historyIndex = sqlHistory.Count - 1;
        }

        private void NavigateHistory(int direction)
        {
            var newIndex = historyIndex + direction;
            if (newIndex >= 0 && newIndex < sqlHistory.Count)
            {
                historyIndex = newIndex;
                sqlText = sqlHistory[historyIndex];
                Repaint();
            }
        }

        private void HandleKeyboardShortcuts()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

            // F5 で実行
            if (e.keyCode == KeyCode.F5)
            {
                ExecuteQuery();
                e.Use();
            }

            // Ctrl+Enter で実行
            if (e.keyCode == KeyCode.Return && (e.control || e.command))
            {
                ExecuteQuery();
                e.Use();
            }

            // Ctrl+上下 で履歴ナビゲーション
            if ((e.control || e.command) && e.keyCode == KeyCode.UpArrow)
            {
                NavigateHistory(-1);
                e.Use();
            }
            if ((e.control || e.command) && e.keyCode == KeyCode.DownArrow)
            {
                NavigateHistory(1);
                e.Use();
            }
        }
    }
}
