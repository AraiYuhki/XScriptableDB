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

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshBackupList();
                RefreshTableList();
            }

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Table:", GUILayout.Width(60));
            selectedTableIndex = EditorGUILayout.Popup(selectedTableIndex, tableNames, GUILayout.Width(150));

            if (GUILayout.Button("Create Backup", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                CreateBackupForSelectedTable();
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Delete All", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Confirm", "Delete all backups?", "Delete", "Cancel"))
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

            EditorGUILayout.LabelField($"Backups: {backups.Count}", GUILayout.Width(150));
            EditorGUILayout.LabelField($"Total Size: {sizeStr}", GUILayout.Width(150));
            EditorGUILayout.LabelField($"Auto: {backups.Count(b => b.IsAutoBackup)} / Manual: {backups.Count(b => !b.IsAutoBackup)}");

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFilters()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Filter:", GUILayout.Width(60));
            filterTableName = EditorGUILayout.TextField(filterTableName, GUILayout.Width(150));

            showAutoBackups = GUILayout.Toggle(showAutoBackups, "Auto", EditorStyles.toolbarButton, GUILayout.Width(50));
            showManualBackups = GUILayout.Toggle(showManualBackups, "Manual", EditorStyles.toolbarButton, GUILayout.Width(50));

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
                EditorGUILayout.HelpBox("No backups found", MessageType.Info);
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
            EditorGUILayout.LabelField($"Created: {backup.CreatedAt}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // アクションボタン
            EditorGUILayout.BeginVertical(GUILayout.Width(80));

            if (GUILayout.Button("Restore", GUILayout.Width(75)))
            {
                RestoreBackup(backup);
            }

            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("Delete", GUILayout.Width(75)))
            {
                if (EditorUtility.DisplayDialog("Confirm", $"Delete backup '{backup.Name}'?", "Delete", "Cancel"))
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
                EditorUtility.DisplayDialog("Error", "Please select a table", "OK");
                return;
            }

            var table = availableTables[selectedTableIndex];
            var name = EditorInputDialog.Show("Backup Name", "Enter backup name:", $"{table.GetType().Name}_backup");

            if (!string.IsNullOrEmpty(name))
            {
                BackupManager.CreateBackup(table, name);
                RefreshBackupList();
                EditorUtility.DisplayDialog("Done", "Backup created successfully", "OK");
            }
        }

        private void RestoreBackup(BackupInfo backup)
        {
            var targetTable = availableTables.FirstOrDefault(t => t.GetType().Name == backup.TableName);

            if (targetTable == null)
            {
                EditorUtility.DisplayDialog("Error", $"Table '{backup.TableName}' not found", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Confirm",
                $"Restore from backup '{backup.Name}'?\nCurrent data will be overwritten.",
                "Restore", "Cancel"))
            {
                return;
            }

            if (BackupManager.RestoreBackup(backup, targetTable))
            {
                EditorUtility.DisplayDialog("Done", "Restore completed successfully", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Restore failed", "OK");
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
