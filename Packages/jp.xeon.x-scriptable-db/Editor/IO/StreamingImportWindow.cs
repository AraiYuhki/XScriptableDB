using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Window for streaming import.
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
            var window = GetWindow<StreamingImportWindow>("Streaming Import");
            window.targetTable = table;
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Streaming Import", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Target table
            using (new EditorGUI.DisabledGroupScope(isImporting))
            {
                targetTable = EditorGUILayout.ObjectField("Target Table", targetTable, typeof(ScriptableObject), false) as ScriptableObject;

                // File selection
                using (new EditorGUILayout.HorizontalScope())
                {
                    filePath = EditorGUILayout.TextField("File Path", filePath);
                    if (GUILayout.Button("Browse", GUILayout.Width(60)))
                    {
                        var path = EditorUtility.OpenFilePanel("Select CSV File", "", "csv");
                        if (!string.IsNullOrEmpty(path))
                            filePath = path;
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

                settings.ChunkSize = EditorGUILayout.IntField("Chunk Size", settings.ChunkSize);
                settings.HasHeader = EditorGUILayout.Toggle("Has Header Row", settings.HasHeader);
                settings.ContinueOnError = EditorGUILayout.Toggle("Continue on Error", settings.ContinueOnError);
                settings.MaxErrors = EditorGUILayout.IntField("Max Errors", settings.MaxErrors);

                var delimiterOptions = new[] { "Comma (,)", "Tab", "Semicolon (;)" };
                var delimiterChars = new[] { ',', '\t', ';' };
                var delimiterIndex = Array.IndexOf(delimiterChars, settings.Delimiter);
                if (delimiterIndex < 0)
                    delimiterIndex = 0;
                delimiterIndex = EditorGUILayout.Popup("Delimiter", delimiterIndex, delimiterOptions);
                settings.Delimiter = delimiterChars[delimiterIndex];
            }

            EditorGUILayout.Space();

            // Progress display
            if (currentProgress != null)
            {
                EditorGUILayout.LabelField("Progress", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"State: {currentProgress.State}");
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(20)), currentProgress.Progress, $"{currentProgress.ProcessedLines} / {currentProgress.TotalLines}");
                EditorGUILayout.LabelField($"Chunk: {currentProgress.CurrentChunk} / {currentProgress.TotalChunks}");
                EditorGUILayout.LabelField($"Success: {currentProgress.SuccessCount}, Errors: {currentProgress.ErrorCount}");
                EditorGUILayout.LabelField($"Elapsed: {currentProgress.ElapsedTime.TotalSeconds:F1}s");

                if (!string.IsNullOrEmpty(currentProgress.ErrorMessage))
                    EditorGUILayout.HelpBox(currentProgress.ErrorMessage, MessageType.Error);

                if (currentProgress.Errors.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Error List", EditorStyles.boldLabel);
                    foreach (var error in currentProgress.Errors.Take(10))
                        EditorGUILayout.LabelField(error, EditorStyles.miniLabel);
                    if (currentProgress.Errors.Count > 10)
                        EditorGUILayout.LabelField($"... and {currentProgress.Errors.Count - 10} more");
                }
            }

            EditorGUILayout.Space();

            // Buttons
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledGroupScope(isImporting || targetTable == null || string.IsNullOrEmpty(filePath)))
                {
                    if (GUILayout.Button("Start Import"))
                        StartImport();
                }

                using (new EditorGUI.DisabledGroupScope(!isImporting))
                {
                    if (GUILayout.Button("Cancel"))
                        CancelImport();
                }
            }
        }

        private void StartImport()
        {
            if (targetTable is not ITableAsset tableAsset)
            {
                EditorUtility.DisplayDialog("Error", "The table does not implement ITableAsset", "OK");
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
                        EditorUtility.DisplayDialog("Completed", $"Import finished\nSuccess: {currentProgress.SuccessCount} records", "OK");
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