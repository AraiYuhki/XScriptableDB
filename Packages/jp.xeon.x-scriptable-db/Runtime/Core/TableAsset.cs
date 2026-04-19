using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// テーブルアセットの基底クラス。
    /// PrimaryKeyでソートされたレコード配列を管理し、バイナリサーチによる高速な検索を提供します。
    /// </summary>
    /// <typeparam name="T">レコードの型</typeparam>
    /// <typeparam name="TKey">PrimaryKeyの型</typeparam>
    public abstract class TableAsset<T, TKey> : ScriptableObject, ITable<T>, ITableAsset, IImportable, IExportable
        where T : class, new()
        where TKey : IComparable<TKey>
    {
        [SerializeField]
        protected T[] records = Array.Empty<T>();

        [SerializeField]
        protected IndexContainer secondaryIndices = new();

        private PrimaryKeyAccessor<T> keyAccessor;
        private bool isSorted;

        /// <summary>
        /// すべてのレコードへの読み取り専用アクセス。
        /// </summary>
        public IReadOnlyList<T> All => records;

        /// <summary>
        /// 内部レコード配列への直接アクセス（クエリ用）。
        /// </summary>
        internal T[] RecordsInternal => records;

        /// <summary>
        /// レコード数。
        /// </summary>
        public int Count => records.Length;

        /// <summary>
        /// ITableAsset用：すべてのレコードを返します。
        /// </summary>
        IEnumerable ITableAsset.Records => records;

        /// <summary>
        /// ITableAsset用：レコードの型。
        /// </summary>
        Type ITableAsset.RecordType => typeof(T);

        /// <summary>
        /// ITableAsset用：PrimaryKeyの型。
        /// </summary>
        Type ITableAsset.KeyType => typeof(TKey);

        /// <summary>
        /// PrimaryKeyアクセサ。
        /// </summary>
        protected PrimaryKeyAccessor<T> KeyAccessor
        {
            get
            {
                keyAccessor ??= new PrimaryKeyAccessor<T>();
                return keyAccessor;
            }
        }

        /// <summary>
        /// SecondaryKeyインデックスコンテナ。
        /// </summary>
        public IndexContainer SecondaryIndices => secondaryIndices;

        protected virtual void OnEnable()
        {
            EnsureSorted();
        }

        /// <summary>
        /// レコードがPrimaryKeyでソートされていることを保証します。
        /// </summary>
        public void EnsureSorted()
        {
            if (isSorted || records == null || records.Length <= 1)
            {
                isSorted = true;
                return;
            }

            Array.Sort(records, KeyAccessor.CreateComparer<TKey>());
            isSorted = true;
        }

        /// <summary>
        /// PrimaryKeyでレコードを検索します（バイナリサーチ）。
        /// </summary>
        /// <param name="key">検索するPrimaryKey</param>
        /// <returns>一致するレコード、見つからない場合はnull</returns>
        public T FindByKey(TKey key)
        {
            EnsureSorted();

            if (records == null || records.Length == 0)
                return null;

            var index = BinarySearch(key);
            if (index < 0)
                return null;

            return records[index];
        }

        /// <summary>
        /// PrimaryKeyでレコードを検索します（バイナリサーチ）。
        /// </summary>
        /// <param name="key">検索するPrimaryKey</param>
        /// <param name="record">見つかったレコード</param>
        /// <returns>見つかった場合はtrue</returns>
        public bool TryFindByKey(TKey key, out T record)
        {
            record = FindByKey(key);
            return record != null;
        }

        /// <summary>
        /// 指定されたインデックスのレコードを取得します。
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns>レコード</returns>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= records.Length)
                    throw new IndexOutOfRangeException($"インデックス {index} は範囲外です [0, {records.Length})");
                return records[index];
            }
        }

        /// <summary>
        /// バイナリサーチを使用してレコードのインデックスを検索します。
        /// </summary>
        /// <param name="key">検索するPrimaryKey</param>
        /// <returns>見つかった場合はインデックス、見つからない場合は負の値</returns>
        protected int BinarySearch(TKey key)
        {
            if (records == null || records.Length == 0)
                return -1;

            var left = 0;
            var right = records.Length - 1;

            while (left <= right)
            {
                var mid = left + (right - left) / 2;
                var midKey = KeyAccessor.GetKey<TKey>(records[mid]);
                var comparison = midKey.CompareTo(key);

                if (comparison == 0)
                    return mid;
                if (comparison < 0)
                    left = mid + 1;
                else
                    right = mid - 1;
            }

            return -1;
        }

        /// <summary>
        /// キーが指定された範囲内に収まるレコードを取得します。
        /// </summary>
        /// <param name="minKey">最小キー（包含）</param>
        /// <param name="maxKey">最大キー（包含）</param>
        /// <returns>範囲内のレコード</returns>
        public IEnumerable<T> FindInRange(TKey minKey, TKey maxKey)
        {
            EnsureSorted();

            if (records == null || records.Length == 0)
                yield break;

            var startIndex = FindLowerBound(minKey);
            if (startIndex < 0)
                yield break;

            for (var i = startIndex; i < records.Length; i++)
            {
                var recordKey = KeyAccessor.GetKey<TKey>(records[i]);
                if (recordKey.CompareTo(maxKey) > 0)
                    yield break;
                yield return records[i];
            }
        }

        /// <summary>
        /// キーが指定されたキー以上となる最初のインデックスを取得します。
        /// </summary>
        private int FindLowerBound(TKey key)
        {
            if (records == null || records.Length == 0)
                return -1;

            var left = 0;
            var right = records.Length;

            while (left < right)
            {
                var mid = left + (right - left) / 2;
                var midKey = KeyAccessor.GetKey<TKey>(records[mid]);
                if (midKey.CompareTo(key) < 0)
                    left = mid + 1;
                else
                    right = mid;
            }

            return left < records.Length ? left : -1;
        }

        /// <summary>
        /// SecondaryKeyでレコードを検索します（O(1)）。
        /// </summary>
        /// <typeparam name="TSecondaryKey">SecondaryKeyの型</typeparam>
        /// <param name="indexName">インデックス名</param>
        /// <param name="key">検索するキー</param>
        /// <returns>一致するレコード、見つからない場合はnull</returns>
        public T FindBySecondaryKey<TSecondaryKey>(string indexName, TSecondaryKey key)
        {
            var index = secondaryIndices.GetIndex(indexName);
            if (index == null)
            {
                Debug.LogWarning($"インデックス '{indexName}' が見つかりませんでした");
                return null;
            }

            var recordIndices = index.FindByKey(key);
            if (recordIndices.Length == 0)
                return null;

            return records[recordIndices[0]];
        }

        /// <summary>
        /// SecondaryKeyでレコードを検索します（O(1)）。
        /// </summary>
        /// <typeparam name="TSecondaryKey">SecondaryKeyの型</typeparam>
        /// <param name="indexName">インデックス名</param>
        /// <param name="key">検索するキー</param>
        /// <param name="record">見つかったレコード</param>
        /// <returns>見つかった場合はtrue</returns>
        public bool TryFindBySecondaryKey<TSecondaryKey>(string indexName, TSecondaryKey key, out T record)
        {
            record = FindBySecondaryKey(indexName, key);
            return record != null;
        }

        /// <summary>
        /// SecondaryKeyで複数のレコードを検索します（O(1)）。
        /// </summary>
        /// <typeparam name="TSecondaryKey">SecondaryKeyの型</typeparam>
        /// <param name="indexName">インデックス名</param>
        /// <param name="key">検索するキー</param>
        /// <returns>一致するレコードの列挙</returns>
        public IEnumerable<T> FindAllBySecondaryKey<TSecondaryKey>(string indexName, TSecondaryKey key)
        {
            var index = secondaryIndices.GetIndex(indexName);
            if (index == null)
            {
                Debug.LogWarning($"Index '{indexName}' not found");
                yield break;
            }

            var recordIndices = index.FindByKey(key);
            foreach (var i in recordIndices)
            {
                if (i >= 0 && i < records.Length)
                    yield return records[i];
            }
        }

        /// <summary>
        /// SecondaryKeyで複数のレコードを検索し、配列として返します（O(1)）。
        /// </summary>
        /// <typeparam name="TSecondaryKey">SecondaryKeyの型</typeparam>
        /// <param name="indexName">インデックス名</param>
        /// <param name="key">検索するキー</param>
        /// <returns>一致するレコードの配列</returns>
        public T[] FindAllBySecondaryKeyAsArray<TSecondaryKey>(string indexName, TSecondaryKey key)
        {
            var index = secondaryIndices.GetIndex(indexName);
            if (index == null)
            {
                Debug.LogWarning($"Index '{indexName}' not found");
                return Array.Empty<T>();
            }

            var recordIndices = index.FindByKey(key);
            if (recordIndices.Length == 0)
                return Array.Empty<T>();

            var result = new T[recordIndices.Length];
            for (var i = 0; i < recordIndices.Length; i++)
            {
                var recordIndex = recordIndices[i];
                if (recordIndex >= 0 && recordIndex < records.Length)
                    result[i] = records[recordIndex];
            }
            return result;
        }

        // ========================================
        // 複合SecondaryKey検索メソッド
        // ========================================

        /// <summary>
        /// 複合SecondaryKeyでレコードを検索します（O(1)）。
        /// </summary>
        /// <param name="indexName">インデックス名</param>
        /// <param name="keyParts">検索するキー値の配列</param>
        /// <returns>一致するレコード、見つからない場合はnull</returns>
        public T FindBySecondaryKey(string indexName, params object[] keyParts)
        {
            var index = secondaryIndices.GetIndex(indexName);
            if (index == null)
            {
                Debug.LogWarning($"Index '{indexName}' not found");
                return null;
            }

            var compositeString = CompositeKeyHelper.ComputeCompositeString(keyParts);
            var recordIndices = index.FindByString(compositeString);
            if (recordIndices.Length == 0)
                return null;

            return records[recordIndices[0]];
        }

        /// <summary>
        /// 複合SecondaryKeyでレコードを検索します（O(1)）。
        /// </summary>
        /// <param name="indexName">インデックス名</param>
        /// <param name="record">見つかったレコード</param>
        /// <param name="keyParts">検索するキー値の配列</param>
        /// <returns>見つかった場合はtrue</returns>
        public bool TryFindBySecondaryKey(string indexName, out T record, params object[] keyParts)
        {
            record = FindBySecondaryKey(indexName, keyParts);
            return record != null;
        }

        /// <summary>
        /// 複合SecondaryKeyで複数のレコードを検索します（O(1)）。
        /// </summary>
        /// <param name="indexName">インデックス名</param>
        /// <param name="keyParts">検索するキー値の配列</param>
        /// <returns>一致するレコードの列挙</returns>
        public IEnumerable<T> FindAllBySecondaryKey(string indexName, params object[] keyParts)
        {
            var index = secondaryIndices.GetIndex(indexName);
            if (index == null)
            {
                Debug.LogWarning($"Index '{indexName}' not found");
                yield break;
            }

            var compositeString = CompositeKeyHelper.ComputeCompositeString(keyParts);
            var recordIndices = index.FindByString(compositeString);
            foreach (var i in recordIndices)
            {
                if (i >= 0 && i < records.Length)
                    yield return records[i];
            }
        }

        /// <summary>
        /// 複合SecondaryKeyで複数のレコードを検索し、配列として返します（O(1)）。
        /// </summary>
        /// <param name="indexName">インデックス名</param>
        /// <param name="keyParts">検索するキー値の配列</param>
        /// <returns>一致するレコードの配列</returns>
        public T[] FindAllBySecondaryKeyAsArray(string indexName, params object[] keyParts)
        {
            var index = secondaryIndices.GetIndex(indexName);
            if (index == null)
            {
                Debug.LogWarning($"Index '{indexName}' not found");
                return Array.Empty<T>();
            }

            var compositeString = CompositeKeyHelper.ComputeCompositeString(keyParts);
            var recordIndices = index.FindByString(compositeString);
            if (recordIndices.Length == 0)
                return Array.Empty<T>();

            var result = new T[recordIndices.Length];
            for (var i = 0; i < recordIndices.Length; i++)
            {
                var recordIndex = recordIndices[i];
                if (recordIndex >= 0 && recordIndex < records.Length)
                    result[i] = records[recordIndex];
            }
            return result;
        }

        /// <summary>
        /// 2つのキーを使用した複合SecondaryKey検索（型安全なオーバーロード）。
        /// </summary>
        public T FindBySecondaryKey<TKey1, TKey2>(string indexName, TKey1 key1, TKey2 key2)
        {
            return FindBySecondaryKey(indexName, (object)key1, (object)key2);
        }

        /// <summary>
        /// 3つのキーを使用した複合SecondaryKey検索（型安全なオーバーロード）。
        /// </summary>
        public T FindBySecondaryKey<TKey1, TKey2, TKey3>(string indexName, TKey1 key1, TKey2 key2, TKey3 key3)
        {
            return FindBySecondaryKey(indexName, (object)key1, (object)key2, (object)key3);
        }

        /// <summary>
        /// 2つのキーを使用した複合SecondaryKeyの複数レコード検索（型安全なオーバーロード）。
        /// </summary>
        public T[] FindAllBySecondaryKeyAsArray<TKey1, TKey2>(string indexName, TKey1 key1, TKey2 key2)
        {
            return FindAllBySecondaryKeyAsArray(indexName, (object)key1, (object)key2);
        }

        /// <summary>
        /// 3つのキーを使用した複合SecondaryKeyの複数レコード検索（型安全なオーバーロード）。
        /// </summary>
        public T[] FindAllBySecondaryKeyAsArray<TKey1, TKey2, TKey3>(string indexName, TKey1 key1, TKey2 key2, TKey3 key3)
        {
            return FindAllBySecondaryKeyAsArray(indexName, (object)key1, (object)key2, (object)key3);
        }

        /// <summary>
        /// CSVファイルからレコードをインポートします。
        /// </summary>
        /// <param name="filePath">CSVファイルのパス</param>
        public void Import(string filePath)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("インポート失敗: filePath が null または空です");
                return;
            }

            if (!File.Exists(filePath))
            {
                Debug.LogError($"インポート失敗: {filePath} にファイルが見つかりませんでした");
                return;
            }

            try
            {
                var importedRecords = CsvParser.ParseRecordFile<T>(filePath);
                SetRecords(importedRecords.ToArray());
                Debug.Log($"{filePath} から {importedRecords.Count} 件のレコードをインポートしました");
            }
            catch (Exception e)
            {
                Debug.LogError($"インポート失敗: {e.Message}");
                Debug.LogException(e);
            }
#else
            Debug.LogWarning("インポートはUnityエディタでのみ利用可能です");
#endif
        }

        /// <summary>
        /// CSVファイルにレコードをエクスポートします。
        /// </summary>
        /// <param name="filePath">出力ファイルパス</param>
        /// <param name="encoding">エンコーディング（デフォルトはUTF-8）</param>
        public void Export(string filePath, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("エクスポート失敗: filePath が null または空です");
                return;
            }

            try
            {
                encoding ??= Encoding.UTF8;
                var csv = CsvParser.ToCSV(records.ToList(), typeof(T));
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(filePath, csv, encoding);
            }
            catch (Exception e)
            {
                Debug.LogError($"エクスポート失敗: {e.Message}");
                Debug.LogException(e);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// エディタでレコードを設定します。
        /// </summary>
        /// <param name="newRecords">新しいレコードの配列</param>
        public void SetRecords(T[] newRecords)
        {
            records = newRecords ?? Array.Empty<T>();
            isSorted = false;
            EnsureSorted();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// エディタでレコードを追加します。
        /// </summary>
        /// <param name="record">追加するレコード</param>
        public void AddRecord(T record)
        {
            if (record == null)
                return;

            var newRecords = new T[records.Length + 1];
            Array.Copy(records, newRecords, records.Length);
            newRecords[records.Length] = record;
            records = newRecords;
            isSorted = false;
            EnsureSorted();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// エディタで指定されたインデックスのレコードを削除します。
        /// </summary>
        /// <param name="index">削除するレコードのインデックス</param>
        public void RemoveRecordAt(int index)
        {
            if (index < 0 || index >= records.Length)
                return;

            var newRecords = new T[records.Length - 1];
            if (index > 0)
                Array.Copy(records, 0, newRecords, 0, index);
            if (index < records.Length - 1)
                Array.Copy(records, index + 1, newRecords, index, records.Length - index - 1);
            records = newRecords;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// 重複するPrimaryKeyをチェックします。
        /// </summary>
        /// <returns>重複キーのリスト</returns>
        public List<TKey> FindDuplicateKeys()
        {
            var duplicates = new List<TKey>();
            var seen = new HashSet<TKey>();

            foreach (var record in records)
            {
                var key = KeyAccessor.GetKey<TKey>(record);
                if (!seen.Add(key))
                    duplicates.Add(key);
            }

            return duplicates;
        }

        /// <summary>
        /// 内部レコード配列を取得します（エディタ専用）。
        /// </summary>
        public T[] GetRecordsForEditor() => records;

        /// <summary>
        /// SecondaryKeyインデックスを再構築します（エディタ専用）。
        /// </summary>
        public void RebuildSecondaryIndices()
        {
            secondaryIndices = IndexBuilder.BuildIndices(records);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// ITableAsset用：新しい空のレコードを作成します。
        /// </summary>
        object ITableAsset.CreateNewRecord() => new T();

        /// <summary>
        /// ITableAsset用：レコードを追加します。
        /// </summary>
        void ITableAsset.AddRecordObject(object record)
        {
            if (record is T typedRecord)
                AddRecord(typedRecord);
        }

        /// <summary>
        /// ITableAsset用：重複するPrimaryKeyをチェックします。
        /// </summary>
        IList ITableAsset.FindDuplicateKeysAsObjects()
        {
            var duplicates = FindDuplicateKeys();
            var result = new ArrayList(duplicates.Count);
            foreach (var key in duplicates)
                result.Add(key);
            return result;
        }
#endif
    }
}
