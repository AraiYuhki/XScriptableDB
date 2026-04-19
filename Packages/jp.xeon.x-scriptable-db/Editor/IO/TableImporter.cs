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
    /// CSVファイルからテーブルにインポートするクラス。
    /// </summary>
    public static class TableImporter
    {
        /// <summary>
        /// ファイルを解析し、レコードの配列を返します。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <param name="filePath">ファイルパス</param>
        /// <param name="encoding">エンコーディング（nullの場合はデフォルトでUTF-8）</param>
        /// <returns>解析されたレコードの配列</returns>
        public static T[] ParseFile<T>(string filePath, Encoding encoding = null) where T : CsvData, new()
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            encoding ??= Encoding.UTF8;
            var content = File.ReadAllText(filePath, encoding);
            var records = CsvParser.Parse<T>(content);
            return records.ToArray();
        }

        /// <summary>
        /// ファイルを解析し、オブジェクトの配列を返します（非ジェネリック版）。
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <param name="recordType">レコードの型</param>
        /// <param name="encoding">エンコーディング</param>
        /// <returns>解析されたレコードの配列</returns>
        public static object[] ParseFile(string filePath, Type recordType, Encoding encoding = null)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            encoding ??= Encoding.UTF8;
            var content = File.ReadAllText(filePath, encoding);

            // リフレクション経由でCsvParser.ParseRecord<T>(string)を呼び出します
            var parseMethod = typeof(CsvParser).GetMethod(
                "ParseRecord",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null);

            if (parseMethod == null)
                throw new InvalidOperationException("CsvParser.ParseRecord method not found");

            var genericMethod = parseMethod.MakeGenericMethod(recordType);
            var result = genericMethod.Invoke(null, new object[] { content });

            // List<T>をobject[]に変換します
            if (result is System.Collections.IList list)
            {
                var array = new object[list.Count];
                for (var i = 0; i < list.Count; i++)
                    array[i] = list[i];
                return array;
            }

            return Array.Empty<object>();
        }

        /// <summary>
        /// プレビュー付きでインポートします。
        /// </summary>
        /// <param name="targetTable">インポート先のテーブル</param>
        /// <param name="settings">インポート設定</param>
        /// <returns>インポートがキャンセルされた場合はfalse</returns>
        public static bool ImportWithPreview(ScriptableObject targetTable, ImportSettings settings)
        {
            if (targetTable == null)
                throw new ArgumentNullException(nameof(targetTable));

            if (string.IsNullOrEmpty(settings?.FilePath))
                throw new ArgumentException("File path is required", nameof(settings));

            var tableAsset = targetTable as ITableAsset;
            if (tableAsset == null)
                throw new InvalidOperationException("Target table must implement ITableAsset");

            try
            {
                // ファイルを解析します
                var importedRecords = ParseFile(settings.FilePath, tableAsset.RecordType, settings.Encoding);

                if (importedRecords.Length == 0)
                {
                    EditorUtility.DisplayDialog("Warning", "No records to import", "OK");
                    return false;
                }

                // 差分を計算します
                var diffResult = DiffCalculator.Calculate(tableAsset, importedRecords);

                if (settings.ShowPreview)
                {
                    // プレビューウィンドウを表示します
                    DiffViewerWindow.Open(diffResult, targetTable, importedRecords);
                    return true; // ユーザーはDiffViewerで適用するかどうかを決定します
                }
                // プレビューなしで直接適用します
                return ApplyImport(targetTable, tableAsset, importedRecords);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Import Error", e.Message, "OK");
                Debug.LogException(e);
                return false;
            }
        }

        /// <summary>
        /// インポートを適用します。
        /// </summary>
        private static bool ApplyImport(ScriptableObject targetTable, ITableAsset tableAsset, object[] importedRecords)
        {
            var setRecordsMethod = targetTable.GetType().GetMethod("SetRecords");
            if (setRecordsMethod == null)
            {
                EditorUtility.DisplayDialog("Error", "SetRecords method not found", "OK");
                return false;
            }

            var recordType = tableAsset.RecordType;
            var typedArray = Array.CreateInstance(recordType, importedRecords.Length);
            Array.Copy(importedRecords, typedArray, importedRecords.Length);

            setRecordsMethod.Invoke(targetTable, new object[] { typedArray });
            EditorUtility.SetDirty(targetTable);

            return true;
        }

        /// <summary>
        /// ファイルの区切り文字を自動検出します。
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>区切り文字</returns>
        public static char DetectDelimiter(string filePath)
        {
            if (!File.Exists(filePath))
                return ',';

            using var reader = new StreamReader(filePath, Encoding.UTF8, true);
            var firstLine = reader.ReadLine();

            if (string.IsNullOrEmpty(firstLine))
                return ',';

            // タブとカンマの数をカウントします
            var tabCount = 0;
            var commaCount = 0;

            foreach (var c in firstLine)
            {
                if (c == '\t') tabCount++;
                else if (c == ',') commaCount++;
            }

            return tabCount > commaCount ? '\t' : ',';
        }

        /// <summary>
        /// ファイルのエンコーディングを自動検出します。
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>エンコーディング</returns>
        public static Encoding DetectEncoding(string filePath)
        {
            if (!File.Exists(filePath))
                return Encoding.UTF8;

            var bytes = File.ReadAllBytes(filePath);

            // BOMを確認します
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8;

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode;

            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode;

            // BOMがない場合、有効なUTF-8かどうかを確認します
            if (IsValidUtf8(bytes))
                return Encoding.UTF8;

            // Shift-JISにフォールバックします
            return Encoding.GetEncoding(932);
        }

        private static bool IsValidUtf8(byte[] bytes)
        {
            var i = 0;
            while (i < bytes.Length)
            {
                if (bytes[i] <= 0x7F)
                {
                    i++;
                    continue;
                }

                int expectedBytes;
                if ((bytes[i] & 0xE0) == 0xC0) expectedBytes = 2;
                else if ((bytes[i] & 0xF0) == 0xE0) expectedBytes = 3;
                else if ((bytes[i] & 0xF8) == 0xF0) expectedBytes = 4;
                else return false;

                if (i + expectedBytes > bytes.Length)
                    return false;

                for (var j = 1; j < expectedBytes; j++)
                {
                    if ((bytes[i + j] & 0xC0) != 0x80)
                        return false;
                }

                i += expectedBytes;
            }

            return true;
        }
    }
}
