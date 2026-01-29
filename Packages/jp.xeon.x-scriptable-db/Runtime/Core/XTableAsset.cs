using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// XScriptableDBのテーブルアセット基底クラス。
    /// PrimaryKeyでソートされたレコード配列を管理し、二分探索による高速検索を提供する。
    /// </summary>
    /// <typeparam name="T">レコードの型</typeparam>
    /// <typeparam name="TKey">PrimaryKeyの型</typeparam>
    public abstract class XTableAsset<T, TKey> : ScriptableObject, ITable<T>, IXTableAsset
        where T : class, new()
        where TKey : IComparable<TKey>
    {
        [SerializeField]
        protected T[] records = Array.Empty<T>();

        private PrimaryKeyAccessor<T> keyAccessor;
        private bool isSorted;

        /// <summary>
        /// 全レコードへの読み取り専用アクセス。
        /// </summary>
        public IReadOnlyList<T> All => records;

        /// <summary>
        /// レコード数。
        /// </summary>
        public int Count => records.Length;

        /// <summary>
        /// IXTableAsset用：全レコードを取得する。
        /// </summary>
        IEnumerable IXTableAsset.Records => records;

        /// <summary>
        /// IXTableAsset用：レコードの型。
        /// </summary>
        Type IXTableAsset.RecordType => typeof(T);

        /// <summary>
        /// IXTableAsset用：PrimaryKeyの型。
        /// </summary>
        Type IXTableAsset.KeyType => typeof(TKey);

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

        protected virtual void OnEnable()
        {
            EnsureSorted();
        }

        /// <summary>
        /// レコードがPrimaryKeyでソートされていることを保証する。
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
        /// PrimaryKeyでレコードを検索する（二分探索）。
        /// </summary>
        /// <param name="key">検索するPrimaryKey</param>
        /// <returns>見つかったレコード、見つからない場合はnull</returns>
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
        /// PrimaryKeyでレコードを検索する（二分探索）。
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
        /// 指定したインデックスのレコードを取得する。
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns>レコード</returns>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= records.Length)
                    throw new IndexOutOfRangeException($"Index {index} is out of range [0, {records.Length})");
                return records[index];
            }
        }

        /// <summary>
        /// 二分探索でレコードのインデックスを検索する。
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
        /// 指定した範囲のキーを持つレコードを取得する。
        /// </summary>
        /// <param name="minKey">最小キー（含む）</param>
        /// <param name="maxKey">最大キー（含む）</param>
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
        /// 指定したキー以上の最初のインデックスを取得する。
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

#if UNITY_EDITOR
        /// <summary>
        /// Editorでレコードを設定する。
        /// </summary>
        /// <param name="newRecords">新しいレコード配列</param>
        public void SetRecords(T[] newRecords)
        {
            records = newRecords ?? Array.Empty<T>();
            isSorted = false;
            EnsureSorted();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Editorでレコードを追加する。
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
        /// Editorでレコードを削除する。
        /// </summary>
        /// <param name="index">削除するインデックス</param>
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
        /// PrimaryKeyの重複をチェックする。
        /// </summary>
        /// <returns>重複しているキーのリスト</returns>
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
        /// 内部のレコード配列を取得する（Editor専用）。
        /// </summary>
        public T[] GetRecordsForEditor() => records;

        /// <summary>
        /// IXTableAsset用：新しい空のレコードを作成する。
        /// </summary>
        object IXTableAsset.CreateNewRecord() => new T();

        /// <summary>
        /// IXTableAsset用：レコードを追加する。
        /// </summary>
        void IXTableAsset.AddRecordObject(object record)
        {
            if (record is T typedRecord)
                AddRecord(typedRecord);
        }

        /// <summary>
        /// IXTableAsset用：PrimaryKeyの重複をチェックする。
        /// </summary>
        IList IXTableAsset.FindDuplicateKeysAsObjects()
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
