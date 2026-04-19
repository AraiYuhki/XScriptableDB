using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// ストリーミングインポートの進行状態。
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
    /// 大量データのストリーミングインポートを実行するクラス。
    /// メモリ使用量を抑えながらチャンク単位でデータを読み込んでインポートします。
    /// </summary>
    public class StreamingImporter
    {
        private StreamingImportProgress progress;
        private bool isCancelled;

        /// <summary>
        /// インポートをキャンセルします。
        /// </summary>
        public void Cancel()
        {
            isCancelled = true;
        }

        /// <summary>
        /// ストリーミングインポートを実行します。
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
            InitializeProgress(filePath, settings);

            try
            {
                var allRecords = new List<T>();
                ReadAndProcessFile(filePath, settings, allRecords);
                FinalizeImport(targetTable, allRecords, settings);
            }
            catch (Exception e)
            {
                HandleFatalError(e);
            }
            finally
            {
                FinalizeProgress(settings);
            }

            return progress;
        }

        /// <summary>
        /// ストリーミングインポートを実行します（非ジェネリック版）。
        /// </summary>
        public StreamingImportProgress Import(
            string filePath,
            ScriptableObject targetTable,
            Type recordType,
            StreamingImportSettings settings = null)
        {
            settings ??= new StreamingImportSettings();
            InitializeProgress(filePath, settings);

            try
            {
                var allRecords = new List<object>();
                ReadAndProcessFileNonGeneric(filePath, settings, recordType, allRecords);
                FinalizeImportNonGeneric(targetTable, allRecords, recordType, settings);
            }
            catch (Exception e)
            {
                HandleFatalError(e);
            }
            finally
            {
                FinalizeProgress(settings);
            }

            return progress;
        }

        /// <summary>
        /// 進行状態を初期化します。
        /// </summary>
        private void InitializeProgress(string filePath, StreamingImportSettings settings)
        {
            progress = new StreamingImportProgress
            {
                State = StreamingImportState.Reading,
                StartTime = DateTime.Now
            };
            isCancelled = false;

            progress.TotalLines = CountLines(filePath, settings.Encoding);
            progress.TotalChunks = (progress.TotalLines + settings.ChunkSize - 1) / settings.ChunkSize;
        }

        /// <summary>
        /// ファイルを読み込んで処理します（ジェネリック版）。
        /// </summary>
        private void ReadAndProcessFile<T>(
            string filePath,
            StreamingImportSettings settings,
            List<T> allRecords) where T : CsvData, new()
        {
            var lineNumber = 0;

            using var reader = new StreamReader(filePath, settings.Encoding);

            var headers = ReadHeader(reader, settings, ref lineNumber);
            progress.State = StreamingImportState.Parsing;

            var chunkLines = new List<string>();
            ProcessAllLines(reader, settings, chunkLines, headers, allRecords, ref lineNumber);
            ProcessRemainingChunk(chunkLines, headers, settings, allRecords);
        }

        /// <summary>
        /// ファイルを読み込んで処理します（非ジェネリック版）。
        /// </summary>
        private void ReadAndProcessFileNonGeneric(
            string filePath,
            StreamingImportSettings settings,
            Type recordType,
            List<object> allRecords)
        {
            var lineNumber = 0;

            using var reader = new StreamReader(filePath, settings.Encoding);

            var headers = ReadHeader(reader, settings, ref lineNumber);
            progress.State = StreamingImportState.Parsing;

            var chunkLines = new List<string>();
            ProcessAllLinesNonGeneric(reader, settings, chunkLines, headers, recordType, allRecords, ref lineNumber);
            ProcessRemainingChunkNonGeneric(chunkLines, headers, settings, recordType, allRecords);
        }

        /// <summary>
        /// ヘッダー行を読み込みます。
        /// </summary>
        private string[] ReadHeader(StreamReader reader, StreamingImportSettings settings, ref int lineNumber)
        {
            if (!settings.HasHeader)
                return null;

            var headerLine = reader.ReadLine();
            if (headerLine == null)
                return null;

            lineNumber++;
            return ParseLine(headerLine, settings.Delimiter);
        }

        /// <summary>
        /// すべての行を処理します（ジェネリック版）。
        /// </summary>
        private void ProcessAllLines<T>(
            StreamReader reader,
            StreamingImportSettings settings,
            List<string> chunkLines,
            string[] headers,
            List<T> allRecords,
            ref int lineNumber) where T : CsvData, new()
        {
            while (!reader.EndOfStream && !isCancelled)
            {
                var line = reader.ReadLine();
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                chunkLines.Add(line);
                progress.ProcessedLines = lineNumber;

                if (chunkLines.Count < settings.ChunkSize)
                    continue;

                ProcessChunk(chunkLines, headers, settings, allRecords);
                chunkLines.Clear();
                progress.CurrentChunk++;
                settings.OnProgress?.Invoke(progress);

                if (ShouldAbortDueToErrors(settings))
                    break;
            }
        }

        /// <summary>
        /// すべての行を処理します（非ジェネリック版）。
        /// </summary>
        private void ProcessAllLinesNonGeneric(
            StreamReader reader,
            StreamingImportSettings settings,
            List<string> chunkLines,
            string[] headers,
            Type recordType,
            List<object> allRecords,
            ref int lineNumber)
        {
            while (!reader.EndOfStream && !isCancelled)
            {
                var line = reader.ReadLine();
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                chunkLines.Add(line);
                progress.ProcessedLines = lineNumber;

                if (chunkLines.Count < settings.ChunkSize)
                    continue;

                ProcessChunkNonGeneric(chunkLines, headers, settings, recordType, allRecords);
                chunkLines.Clear();
                progress.CurrentChunk++;
                settings.OnProgress?.Invoke(progress);

                if (ShouldAbortDueToErrors(settings))
                    break;
            }
        }

        /// <summary>
        /// エラー数が制限を超えたかどうかを判断し、超えた場合は状態を更新します。
        /// </summary>
        private bool ShouldAbortDueToErrors(StreamingImportSettings settings)
        {
            if (progress.ErrorCount < settings.MaxErrors)
                return false;

            progress.State = StreamingImportState.Failed;
            progress.ErrorMessage = $"Aborted because errors exceeded {settings.MaxErrors}";
            return true;
        }

        /// <summary>
        /// 残りのチャンクを処理します（ジェネリック版）。
        /// </summary>
        private void ProcessRemainingChunk<T>(
            List<string> chunkLines,
            string[] headers,
            StreamingImportSettings settings,
            List<T> allRecords) where T : CsvData, new()
        {
            if (chunkLines.Count == 0 || isCancelled || progress.State == StreamingImportState.Failed)
                return;

            ProcessChunk(chunkLines, headers, settings, allRecords);
            progress.CurrentChunk++;
        }

        /// <summary>
        /// 残りのチャンクを処理します（非ジェネリック版）。
        /// </summary>
        private void ProcessRemainingChunkNonGeneric(
            List<string> chunkLines,
            string[] headers,
            StreamingImportSettings settings,
            Type recordType,
            List<object> allRecords)
        {
            if (chunkLines.Count == 0 || isCancelled || progress.State == StreamingImportState.Failed)
                return;

            ProcessChunkNonGeneric(chunkLines, headers, settings, recordType, allRecords);
            progress.CurrentChunk++;
        }

        /// <summary>
        /// インポートを完了します（ジェネリック版）。
        /// </summary>
        private void FinalizeImport<T>(
            ScriptableObject targetTable,
            List<T> allRecords,
            StreamingImportSettings settings)
        {
            if (isCancelled)
            {
                progress.State = StreamingImportState.Cancelled;
                progress.ErrorMessage = "Import was cancelled";
                return;
            }

            if (progress.State == StreamingImportState.Failed)
                return;

            progress.State = StreamingImportState.Applying;
            settings.OnProgress?.Invoke(progress);

            ApplyToTable(targetTable, allRecords);

            progress.State = StreamingImportState.Completed;
            progress.SuccessCount = allRecords.Count;
        }

        /// <summary>
        /// インポートを完了します（非ジェネリック版）。
        /// </summary>
        private void FinalizeImportNonGeneric(
            ScriptableObject targetTable,
            List<object> allRecords,
            Type recordType,
            StreamingImportSettings settings)
        {
            if (isCancelled)
            {
                progress.State = StreamingImportState.Cancelled;
                progress.ErrorMessage = "Import was cancelled";
                return;
            }

            if (progress.State == StreamingImportState.Failed)
                return;

            progress.State = StreamingImportState.Applying;
            settings.OnProgress?.Invoke(progress);

            ApplyToTableNonGeneric(targetTable, allRecords, recordType);

            progress.State = StreamingImportState.Completed;
            progress.SuccessCount = allRecords.Count;
        }

        /// <summary>
        /// 致命的なエラーを処理します。
        /// </summary>
        private void HandleFatalError(Exception e)
        {
            progress.State = StreamingImportState.Failed;
            progress.ErrorMessage = e.Message;
            progress.Errors.Add($"Fatal: {e.Message}");
            Debug.LogException(e);
        }

        /// <summary>
        /// 進行状態を完了します。
        /// </summary>
        private void FinalizeProgress(StreamingImportSettings settings)
        {
            progress.EndTime = DateTime.Now;
            settings.OnProgress?.Invoke(progress);
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
}
