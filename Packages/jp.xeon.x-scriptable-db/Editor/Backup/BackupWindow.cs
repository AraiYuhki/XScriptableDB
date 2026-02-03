using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// バックアップ管理ウィンドウ。
    /// </summary>
    public class BackupWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<BackupInfo> backups = new();
        private string filterTableName = "";
        private bool showAutoBackups = true;
        private bool showManualBackups = true;

        private List<ITableAsset> availableTables = new();
        private string[] tableNames = Array.Empty<string>();
        private int selectedTableIndex = -1;

        [MenuItem("Tools/XScriptableDB/Backup Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<BackupWindow>("Backup Manager");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshBackupList();
            RefreshTableList();
        }

        private void RefreshBackupList()
        {
            backups = BackupManager.GetAllBackups();
        }

        private void RefreshTableList()
        {
            availableTables.Clear();

            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset is ITableAsset tableAsset)
                    availableTables.Add(tableAsset);
            }

            tableNames = availableTables.Select(t => t.GetType().Name).ToArray();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            DrawToolbar();
            DrawStats();
            DrawFilters();
            DrawBackupList();

            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("更新", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshBackupList();
                RefreshTableList();
            }

            GUILayout.Space(10);

            EditorGUILayout.LabelField("テーブル:", GUILayout.Width(60));
            selectedTableIndex = EditorGUILayout.Popup(selectedTableIndex, tableNames, GUILayout.Width(150));

            if (GUILayout.Button("バックアップ作成", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                CreateBackupForSelectedTable();
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("全削除", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("確認", "全てのバックアップを削除しますか？", "削除", "キャンセル"))
                {
                    BackupManager.ClearAllBackups();
                    RefreshBackupList();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStats()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            var totalSize = BackupManager.GetTotalBackupSize();
            var sizeStr = FormatFileSize(totalSize);

            EditorGUILayout.LabelField($"バックアップ数: {backups.Count}", GUILayout.Width(150));
            EditorGUILayout.LabelField($"合計サイズ: {sizeStr}", GUILayout.Width(150));
            EditorGUILayout.LabelField($"自動: {backups.Count(b => b.IsAutoBackup)} / 手動: {backups.Count(b => !b.IsAutoBackup)}");

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFilters()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("フィルター:", GUILayout.Width(60));
            filterTableName = EditorGUILayout.TextField(filterTableName, GUILayout.Width(150));

            showAutoBackups = GUILayout.Toggle(showAutoBackups, "自動", EditorStyles.toolbarButton, GUILayout.Width(50));
            showManualBackups = GUILayout.Toggle(showManualBackups, "手動", EditorStyles.toolbarButton, GUILayout.Width(50));

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBackupList()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var filteredBackups = backups
                .Where(b => string.IsNullOrEmpty(filterTableName) ||
                           b.TableName.Contains(filterTableName, StringComparison.OrdinalIgnoreCase))
                .Where(b => (showAutoBackups && b.IsAutoBackup) || (showManualBackups && !b.IsAutoBackup))
                .ToList();

            if (filteredBackups.Count == 0)
            {
                EditorGUILayout.HelpBox("バックアップがありません", MessageType.Info);
            }
            else
            {
                foreach (var backup in filteredBackups)
                {
                    DrawBackupItem(backup);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawBackupItem(BackupInfo backup)
        {
            var bgColor = backup.IsAutoBackup ? new Color(0.3f, 0.3f, 0.4f, 0.3f) : new Color(0.3f, 0.4f, 0.3f, 0.3f);
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = oldColor;

            EditorGUILayout.BeginHorizontal();

            // アイコン
            var icon = backup.IsAutoBackup
                ? EditorGUIUtility.IconContent("d_Refresh")
                : EditorGUIUtility.IconContent("d_SaveAs");
            GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));

            // 情報
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(backup.Name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{backup.TableName} | {backup.RecordCount} records | {FormatFileSize(backup.FileSize)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"作成日時: {backup.CreatedAt}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // アクションボタン
            EditorGUILayout.BeginVertical(GUILayout.Width(80));

            if (GUILayout.Button("リストア", GUILayout.Width(75)))
            {
                RestoreBackup(backup);
            }

            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("削除", GUILayout.Width(75)))
            {
                if (EditorUtility.DisplayDialog("確認", $"バックアップ '{backup.Name}' を削除しますか？", "削除", "キャンセル"))
                {
                    BackupManager.DeleteBackup(backup);
                    RefreshBackupList();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(backup.Description))
            {
                EditorGUILayout.LabelField(backup.Description, EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateBackupForSelectedTable()
        {
            if (selectedTableIndex < 0 || selectedTableIndex >= availableTables.Count)
            {
                EditorUtility.DisplayDialog("エラー", "テーブルを選択してください", "OK");
                return;
            }

            var table = availableTables[selectedTableIndex];
            var name = EditorInputDialog.Show("バックアップ名", "バックアップ名を入力してください:", $"{table.GetType().Name}_backup");

            if (!string.IsNullOrEmpty(name))
            {
                BackupManager.CreateBackup(table, name);
                RefreshBackupList();
                EditorUtility.DisplayDialog("完了", "バックアップを作成しました", "OK");
            }
        }

        private void RestoreBackup(BackupInfo backup)
        {
            var targetTable = availableTables.FirstOrDefault(t => t.GetType().Name == backup.TableName);

            if (targetTable == null)
            {
                EditorUtility.DisplayDialog("エラー", $"テーブル '{backup.TableName}' が見つかりません", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("確認",
                $"バックアップ '{backup.Name}' からリストアしますか？\n現在のデータは上書きされます。",
                "リストア", "キャンセル"))
            {
                return;
            }

            if (BackupManager.RestoreBackup(backup, targetTable))
            {
                EditorUtility.DisplayDialog("完了", "リストアが完了しました", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", "リストアに失敗しました", "OK");
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:F1} {suffixes[suffixIndex]}";
        }
    }
}
