using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// ストリーミングインポートのウィンドウ。
    /// </summary>
    public class StreamingImportWindow : EditorWindow
    {
        private string filePath;
        private ScriptableObject targetTable;
        private StreamingImportSettings settings = new();
        private StreamingImporter importer;
        private StreamingImportProgress currentProgress;
        private bool isImporting;

        public static void Open(ScriptableObject table)
        {
            var window = GetWindow<StreamingImportWindow>("ストリーミングインポート");
            window.targetTable = table;
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("ストリーミングインポート", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // ターゲットテーブル
            using (new EditorGUI.DisabledGroupScope(isImporting))
            {
                targetTable = EditorGUILayout.ObjectField("ターゲットテーブル", targetTable, typeof(ScriptableObject), false) as ScriptableObject;

                // ファイル選択
                using (new EditorGUILayout.HorizontalScope())
                {
                    filePath = EditorGUILayout.TextField("ファイルパス", filePath);
                    if (GUILayout.Button("参照", GUILayout.Width(60)))
                    {
                        var path = EditorUtility.OpenFilePanel("CSVファイルを選択", "", "csv");
                        if (!string.IsNullOrEmpty(path))
                            filePath = path;
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("設定", EditorStyles.boldLabel);

                settings.ChunkSize = EditorGUILayout.IntField("チャンクサイズ", settings.ChunkSize);
                settings.HasHeader = EditorGUILayout.Toggle("ヘッダー行あり", settings.HasHeader);
                settings.ContinueOnError = EditorGUILayout.Toggle("エラー時に継続", settings.ContinueOnError);
                settings.MaxErrors = EditorGUILayout.IntField("最大エラー数", settings.MaxErrors);

                var delimiterOptions = new[] { "カンマ (,)", "タブ", "セミコロン (;)" };
                var delimiterChars = new[] { ',', '\t', ';' };
                var delimiterIndex = Array.IndexOf(delimiterChars, settings.Delimiter);
                if (delimiterIndex < 0)
                    delimiterIndex = 0;
                delimiterIndex = EditorGUILayout.Popup("区切り文字", delimiterIndex, delimiterOptions);
                settings.Delimiter = delimiterChars[delimiterIndex];
            }

            EditorGUILayout.Space();

            // 進捗表示
            if (currentProgress != null)
            {
                EditorGUILayout.LabelField("進捗", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"状態: {currentProgress.State}");
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(20)), currentProgress.Progress, $"{currentProgress.ProcessedLines} / {currentProgress.TotalLines}");
                EditorGUILayout.LabelField($"チャンク: {currentProgress.CurrentChunk} / {currentProgress.TotalChunks}");
                EditorGUILayout.LabelField($"成功: {currentProgress.SuccessCount}, エラー: {currentProgress.ErrorCount}");
                EditorGUILayout.LabelField($"経過時間: {currentProgress.ElapsedTime.TotalSeconds:F1}秒");

                if (!string.IsNullOrEmpty(currentProgress.ErrorMessage))
                    EditorGUILayout.HelpBox(currentProgress.ErrorMessage, MessageType.Error);

                if (currentProgress.Errors.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("エラー一覧", EditorStyles.boldLabel);
                    foreach (var error in currentProgress.Errors.Take(10))
                        EditorGUILayout.LabelField(error, EditorStyles.miniLabel);
                    if (currentProgress.Errors.Count > 10)
                        EditorGUILayout.LabelField($"... 他 {currentProgress.Errors.Count - 10} 件");
                }
            }

            EditorGUILayout.Space();

            // ボタン
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledGroupScope(isImporting || targetTable == null || string.IsNullOrEmpty(filePath)))
                {
                    if (GUILayout.Button("インポート開始"))
                        StartImport();
                }

                using (new EditorGUI.DisabledGroupScope(!isImporting))
                {
                    if (GUILayout.Button("キャンセル"))
                        CancelImport();
                }
            }
        }

        private void StartImport()
        {
            if (targetTable is not ITableAsset tableAsset)
            {
                EditorUtility.DisplayDialog("エラー", "テーブルがITableAssetを実装していません", "OK");
                return;
            }

            isImporting = true;
            importer = new StreamingImporter();
            settings.OnProgress = OnProgressUpdate;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    currentProgress = importer.Import(filePath, targetTable, tableAsset.RecordType, settings);

                    if (currentProgress.State == StreamingImportState.Completed)
                        EditorUtility.DisplayDialog("完了", $"インポートが完了しました\n成功: {currentProgress.SuccessCount}件", "OK");
                }
                finally
                {
                    isImporting = false;
                    Repaint();
                }
            };
        }

        private void CancelImport()
        {
            importer?.Cancel();
        }

        private void OnProgressUpdate(StreamingImportProgress progress)
        {
            currentProgress = progress;
            Repaint();
        }
    }
}