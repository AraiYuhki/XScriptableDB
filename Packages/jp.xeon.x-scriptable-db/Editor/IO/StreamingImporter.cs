using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// ストリーミングインポートの進捗状態。
    /// </summary>
    public enum StreamingImportState
    {
        NotStarted,
        Reading,
        Parsing,
        Applying,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// ストリーミングインポートの進捗情報。
    /// </summary>
    public class StreamingImportProgress
    {
        /// <summary>現在の状態</summary>
        public StreamingImportState State { get; set; } = StreamingImportState.NotStarted;

        /// <summary>総行数</summary>
        public int TotalLines { get; set; }

        /// <summary>処理済み行数</summary>
        public int ProcessedLines { get; set; }

        /// <summary>成功したレコード数</summary>
        public int SuccessCount { get; set; }

        /// <summary>失敗したレコード数</summary>
        public int ErrorCount { get; set; }

        /// <summary>警告リスト</summary>
        public List<string> Warnings { get; } = new();

        /// <summary>エラーリスト</summary>
        public List<string> Errors { get; } = new();

        /// <summary>現在のチャンク番号</summary>
        public int CurrentChunk { get; set; }

        /// <summary>総チャンク数</summary>
        public int TotalChunks { get; set; }

        /// <summary>処理の進捗（0.0〜1.0）</summary>
        public float Progress
        {
            get
            {
                if (TotalLines <= 0)
                    return 0f;
                return (float)ProcessedLines / TotalLines;
            }
        }

        /// <summary>エラーメッセージ（失敗時）</summary>
        public string ErrorMessage { get; set; }

        /// <summary>開始時刻</summary>
        public DateTime StartTime { get; set; }

        /// <summary>終了時刻</summary>
        public DateTime? EndTime { get; set; }

        /// <summary>経過時間</summary>
        public TimeSpan ElapsedTime => (EndTime ?? DateTime.Now) - StartTime;
    }

    /// <summary>
    /// ストリーミングインポートの設定。
    /// </summary>
    public class StreamingImportSettings
    {
        /// <summary>チャンクサイズ（1回で処理する行数）</summary>
        public int ChunkSize { get; set; } = 1000;

        /// <summary>エンコーディング</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>区切り文字</summary>
        public char Delimiter { get; set; } = ',';

        /// <summary>ヘッダー行があるか</summary>
        public bool HasHeader { get; set; } = true;

        /// <summary>エラー時に継続するか</summary>
        public bool ContinueOnError { get; set; } = true;

        /// <summary>最大エラー数（これを超えると処理を停止）</summary>
        public int MaxErrors { get; set; } = 100;

        /// <summary>進捗コールバック</summary>
        public Action<StreamingImportProgress> OnProgress { get; set; }
    }

    /// <summary>
    /// 大量データのストリーミングインポートを行うクラス。
    /// チャンク単位でデータを読み込み、メモリ使用量を抑えながらインポートする。
    /// </summary>
    public class StreamingImporter
    {
        private StreamingImportProgress progress;
        private bool isCancelled;

        /// <summary>
        /// インポートをキャンセルする。
        /// </summary>
        public void Cancel()
        {
            isCancelled = true;
        }

        /// <summary>
        /// ストリーミングインポートを実行する。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <param name="filePath">インポートするファイルのパス</param>
        /// <param name="targetTable">インポート先のテーブル</param>
        /// <param name="settings">インポート設定</param>
        /// <returns>インポート結果</returns>
        public StreamingImportProgress Import<T>(
            string filePath,
            ScriptableObject targetTable,
            StreamingImportSettings settings = null)
            where T : CsvData, new()
        {
            settings ??= new StreamingImportSettings();
            progress = new StreamingImportProgress
            {
                State = StreamingImportState.Reading,
                StartTime = DateTime.Now
            };
            isCancelled = false;

            try
            {
                // ファイルの行数をカウント
                progress.TotalLines = CountLines(filePath, settings.Encoding);
                progress.TotalChunks = (progress.TotalLines + settings.ChunkSize - 1) / settings.ChunkSize;

                var allRecords = new List<T>();
                var lineNumber = 0;
                string[] headers = null;

                using (var reader = new StreamReader(filePath, settings.Encoding))
                {
                    // ヘッダー行の処理
                    if (settings.HasHeader)
                    {
                        var headerLine = reader.ReadLine();
                        if (headerLine != null)
                        {
                            headers = ParseLine(headerLine, settings.Delimiter);
                            lineNumber++;
                        }
                    }

                    progress.State = StreamingImportState.Parsing;
                    var chunkRecords = new List<T>();
                    var chunkLines = new List<string>();

                    while (!reader.EndOfStream && !isCancelled)
                    {
                        var line = reader.ReadLine();
                        lineNumber++;

                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        chunkLines.Add(line);

                        // チャンクが満タンになったら処理
                        if (chunkLines.Count >= settings.ChunkSize)
                        {
                            ProcessChunk(chunkLines, headers, settings, allRecords);
                            chunkLines.Clear();
                            progress.CurrentChunk++;
                            settings.OnProgress?.Invoke(progress);

                            // エラーが多すぎる場合は中断
                            if (progress.ErrorCount >= settings.MaxErrors)
                            {
                                progress.State = StreamingImportState.Failed;
                                progress.ErrorMessage = $"エラーが{settings.MaxErrors}件を超えたため中断しました";
                                break;
                            }
                        }

                        progress.ProcessedLines = lineNumber;
                    }

                    // 残りのチャンクを処理
                    if (chunkLines.Count > 0 && !isCancelled && progress.State != StreamingImportState.Failed)
                    {
                        ProcessChunk(chunkLines, headers, settings, allRecords);
                        progress.CurrentChunk++;
                    }
                }

                if (isCancelled)
                {
                    progress.State = StreamingImportState.Cancelled;
                    progress.ErrorMessage = "インポートがキャンセルされました";
                }
                else if (progress.State != StreamingImportState.Failed)
                {
                    // テーブルに適用
                    progress.State = StreamingImportState.Applying;
                    settings.OnProgress?.Invoke(progress);

                    ApplyToTable(targetTable, allRecords);

                    progress.State = StreamingImportState.Completed;
                    progress.SuccessCount = allRecords.Count;
                }
            }
            catch (Exception e)
            {
                progress.State = StreamingImportState.Failed;
                progress.ErrorMessage = e.Message;
                progress.Errors.Add($"Fatal: {e.Message}");
                Debug.LogException(e);
            }
            finally
            {
                progress.EndTime = DateTime.Now;
                settings.OnProgress?.Invoke(progress);
            }

            return progress;
        }

        /// <summary>
        /// ストリーミングインポートを実行する（非ジェネリック版）。
        /// </summary>
        public StreamingImportProgress Import(
            string filePath,
            ScriptableObject targetTable,
            Type recordType,
            StreamingImportSettings settings = null)
        {
            settings ??= new StreamingImportSettings();
            progress = new StreamingImportProgress
            {
                State = StreamingImportState.Reading,
                StartTime = DateTime.Now
            };
            isCancelled = false;

            try
            {
                progress.TotalLines = CountLines(filePath, settings.Encoding);
                progress.TotalChunks = (progress.TotalLines + settings.ChunkSize - 1) / settings.ChunkSize;

                var allRecords = new List<object>();
                var lineNumber = 0;
                string[] headers = null;

                using (var reader = new StreamReader(filePath, settings.Encoding))
                {
                    if (settings.HasHeader)
                    {
                        var headerLine = reader.ReadLine();
                        if (headerLine != null)
                        {
                            headers = ParseLine(headerLine, settings.Delimiter);
                            lineNumber++;
                        }
                    }

                    progress.State = StreamingImportState.Parsing;
                    var chunkLines = new List<string>();

                    while (!reader.EndOfStream && !isCancelled)
                    {
                        var line = reader.ReadLine();
                        lineNumber++;

                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        chunkLines.Add(line);

                        if (chunkLines.Count >= settings.ChunkSize)
                        {
                            ProcessChunkNonGeneric(chunkLines, headers, settings, recordType, allRecords);
                            chunkLines.Clear();
                            progress.CurrentChunk++;
                            settings.OnProgress?.Invoke(progress);

                            if (progress.ErrorCount >= settings.MaxErrors)
                            {
                                progress.State = StreamingImportState.Failed;
                                progress.ErrorMessage = $"エラーが{settings.MaxErrors}件を超えたため中断しました";
                                break;
                            }
                        }

                        progress.ProcessedLines = lineNumber;
                    }

                    if (chunkLines.Count > 0 && !isCancelled && progress.State != StreamingImportState.Failed)
                    {
                        ProcessChunkNonGeneric(chunkLines, headers, settings, recordType, allRecords);
                        progress.CurrentChunk++;
                    }
                }

                if (isCancelled)
                {
                    progress.State = StreamingImportState.Cancelled;
                    progress.ErrorMessage = "インポートがキャンセルされました";
                }
                else if (progress.State != StreamingImportState.Failed)
                {
                    progress.State = StreamingImportState.Applying;
                    settings.OnProgress?.Invoke(progress);

                    ApplyToTableNonGeneric(targetTable, allRecords, recordType);

                    progress.State = StreamingImportState.Completed;
                    progress.SuccessCount = allRecords.Count;
                }
            }
            catch (Exception e)
            {
                progress.State = StreamingImportState.Failed;
                progress.ErrorMessage = e.Message;
                progress.Errors.Add($"Fatal: {e.Message}");
                Debug.LogException(e);
            }
            finally
            {
                progress.EndTime = DateTime.Now;
                settings.OnProgress?.Invoke(progress);
            }

            return progress;
        }

        private int CountLines(string filePath, Encoding encoding)
        {
            var count = 0;
            using (var reader = new StreamReader(filePath, encoding))
            {
                while (reader.ReadLine() != null)
                    count++;
            }
            return count;
        }

        private string[] ParseLine(string line, char delimiter)
        {
            var result = new List<string>();
            var inQuotes = false;
            var current = new StringBuilder();

            foreach (var c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == delimiter && !inQuotes)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString().Trim());
            return result.ToArray();
        }

        private void ProcessChunk<T>(
            List<string> lines,
            string[] headers,
            StreamingImportSettings settings,
            List<T> allRecords) where T : CsvData, new()
        {
            foreach (var line in lines)
            {
                try
                {
                    var values = ParseLine(line, settings.Delimiter);
                    var record = new T();

                    if (headers != null)
                    {
                        var dict = new Dictionary<string, string>();
                        for (var i = 0; i < headers.Length && i < values.Length; i++)
                            dict[headers[i]] = values[i];

                        PopulateRecord(record, dict);
                    }

                    allRecords.Add(record);
                }
                catch (Exception e)
                {
                    progress.ErrorCount++;
                    progress.Errors.Add($"Line {progress.ProcessedLines}: {e.Message}");

                    if (!settings.ContinueOnError)
                        throw;
                }
            }
        }

        private void ProcessChunkNonGeneric(
            List<string> lines,
            string[] headers,
            StreamingImportSettings settings,
            Type recordType,
            List<object> allRecords)
        {
            foreach (var line in lines)
            {
                try
                {
                    var values = ParseLine(line, settings.Delimiter);
                    var record = Activator.CreateInstance(recordType);

                    if (headers != null)
                    {
                        var dict = new Dictionary<string, string>();
                        for (var i = 0; i < headers.Length && i < values.Length; i++)
                            dict[headers[i]] = values[i];

                        PopulateRecordReflection(record, recordType, dict);
                    }

                    allRecords.Add(record);
                }
                catch (Exception e)
                {
                    progress.ErrorCount++;
                    progress.Errors.Add($"Line {progress.ProcessedLines}: {e.Message}");

                    if (!settings.ContinueOnError)
                        throw;
                }
            }
        }

        private void PopulateRecord<T>(T record, Dictionary<string, string> values) where T : CsvData
        {
            var type = typeof(T);
            foreach (var field in ReflectionUtility.GetSerializableFields(type))
            {
                var csvAttr = field.GetCustomAttribute<CsvColumn>();
                var columnName = csvAttr?.Name ?? field.Name;

                if (values.TryGetValue(columnName, out var value))
                {
                    var convertedValue = ConvertValue(value, field.FieldType);
                    field.SetValue(record, convertedValue);
                }
            }
        }

        private void PopulateRecordReflection(object record, Type recordType, Dictionary<string, string> values)
        {
            foreach (var field in ReflectionUtility.GetSerializableFields(recordType))
            {
                var csvAttr = field.GetCustomAttribute<CsvColumn>();
                var columnName = csvAttr?.Name ?? field.Name;

                if (values.TryGetValue(columnName, out var value))
                {
                    var convertedValue = ConvertValue(value, field.FieldType);
                    field.SetValue(record, convertedValue);
                }
            }
        }

        private object ConvertValue(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            if (targetType == typeof(string))
                return value;
            if (targetType == typeof(int))
                return int.Parse(value);
            if (targetType == typeof(float))
                return float.Parse(value);
            if (targetType == typeof(double))
                return double.Parse(value);
            if (targetType == typeof(bool))
                return bool.Parse(value);
            if (targetType == typeof(long))
                return long.Parse(value);
            if (targetType.IsEnum)
                return Enum.Parse(targetType, value);

            return Convert.ChangeType(value, targetType);
        }

        private void ApplyToTable<T>(ScriptableObject targetTable, List<T> records)
        {
            var setRecordsMethod = targetTable.GetType().GetMethod("SetRecords");
            if (setRecordsMethod != null)
            {
                var array = records.ToArray();
                setRecordsMethod.Invoke(targetTable, new object[] { array });
                EditorUtility.SetDirty(targetTable);
            }
        }

        private void ApplyToTableNonGeneric(ScriptableObject targetTable, List<object> records, Type recordType)
        {
            var setRecordsMethod = targetTable.GetType().GetMethod("SetRecords");
            if (setRecordsMethod != null)
            {
                var array = Array.CreateInstance(recordType, records.Count);
                for (var i = 0; i < records.Count; i++)
                    array.SetValue(records[i], i);

                setRecordsMethod.Invoke(targetTable, new object[] { array });
                EditorUtility.SetDirty(targetTable);
            }
        }
    }

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
