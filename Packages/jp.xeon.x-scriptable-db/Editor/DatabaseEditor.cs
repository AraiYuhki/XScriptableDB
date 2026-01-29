using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Editor
{
    public class DatabaseEditor : EditorWindow
    {
        [MenuItem("Tools/Master/データベースエディタ")]
        public static void Open() => GetWindow<DatabaseEditor>();

        private int tabIndex = 0;

        private List<SerializedProperty> dataObjects = new();

        private List<Vector2> scrollPositions = new();

        public void OnGUI()
        {
            Initialize();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.ExpandWidth(true)))
            {
                tabIndex = EditorGUILayout.Popup(tabIndex, DB.Instance.GetTableNames());
                if (GUILayout.Button("再読み込み"))
                    Refresh();
            }
            if (tabIndex > dataObjects.Count) return;
            var tableName = DB.Instance.GetTableNames()[tabIndex];
            var table = dataObjects[tabIndex];
            if (table == null || table.serializedObject == null)
            {
                Refresh();
                table = dataObjects[tabIndex];
            }
            table.serializedObject.Update();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("TSVに保存"))
                {
                    Export(tableName, Encoding.UTF8);
                }
                if (GUILayout.Button("TSVに保存(Excel対応)"))
                {
                    Export(tableName, Encoding.GetEncoding(932));
                }
                if (GUILayout.Button("TSVからインポート"))
                {
                    Import(tableName);
                }
                if (GUILayout.Button("保存"))
                {
                    EditorUtility.SetDirty(table.serializedObject.targetObject);
                    AssetDatabase.SaveAssetIfDirty(table.serializedObject.targetObject);
                    table.serializedObject.ApplyModifiedProperties();
                    EditorUtility.DisplayDialog("保存完了", $"{tableName}を保存しました", "OK");
                }
            }
            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPositions[tabIndex]))
            {
                EditorGUILayout.PropertyField(table);
                scrollPositions[tabIndex] = scrollView.scrollPosition;
            }
            table.serializedObject.ApplyModifiedProperties();
        }

        private void Import(string tableName)
        {
            var filePath = EditorUtility.OpenFilePanelWithFilters($"インポートするファイルを選択({tableName})", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), new string[] { "TSV files", "tsv" });
            if (string.IsNullOrEmpty(filePath)) return;

            var serializedObject = dataObjects[tabIndex].serializedObject;
            var importer = serializedObject.targetObject as IImportable;
            if (importer == null) return;
            importer.Import(filePath);
            serializedObject.SetIsDifferentCacheDirty();
            EditorUtility.DisplayDialog("インポート完了", $"{tableName}をインポートしました", "OK");
            Repaint();
        }

        private void Export(string tableName, Encoding encoding)
        {
            var filePath = EditorUtility.SaveFilePanel("エクスポート先を選択", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"{tableName}.tsv", "tsv");
            var table = DB.Instance.GetTable(tableName);
            var exporter = table as IExportable;
            if (exporter == null) return;
            exporter.Export(filePath, encoding);
        }

        private void Initialize()
        {
            if (dataObjects != null && dataObjects.Count > 0) return;
            if (dataObjects == null) dataObjects = new();
            dataObjects.Clear();
            scrollPositions.Clear();
            foreach ((var _, var table) in DB.Instance.GetTableList())
            {
                var serializedObject = new SerializedObject(table);
                var serializedProperty = serializedObject.FindProperty("data");
                if (serializedProperty == null)
                {
                    Debug.LogError($"{table.name} has not data property");
                    dataObjects.Add(null);
                    continue;
                }
                dataObjects.Add(serializedProperty);
                scrollPositions.Add(Vector2.zero);
            }
        }

        private void Refresh()
        {
            DB.ReloadDB();
            dataObjects.Clear();
            scrollPositions.Clear();
            Initialize();
        }

        private void OnDestroy()
        {
            foreach ((var _, var table) in DB.Instance.GetTableList())
                AssetDatabase.SaveAssetIfDirty(table);
        }
    }
}
