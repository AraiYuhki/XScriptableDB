using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// SecondaryKeyインデックスのシリアライズ可能なデータ構造。
    /// キー値からレコードインデックスへのマッピングを保持する。
    /// </summary>
    [Serializable]
    public class IndexData
    {
        [SerializeField]
        private string indexName;

        [SerializeField]
        private string keyTypeName;

        [SerializeField]
        private List<IndexEntry> entries = new();

        /// <summary>
        /// インデックスの名前。
        /// </summary>
        public string IndexName => indexName;

        /// <summary>
        /// キーの型名。
        /// </summary>
        public string KeyTypeName => keyTypeName;

        /// <summary>
        /// エントリ数。
        /// </summary>
        public int Count => entries.Count;

        // ランタイム用のハッシュマップ（シリアライズされない）
        [NonSerialized]
        private Dictionary<int, int[]> hashToIndices;

        [NonSerialized]
        private Dictionary<string, int[]> stringToIndices;

        [NonSerialized]
        private bool isInitialized;

        public IndexData() { }

        public IndexData(string name, Type keyType)
        {
            indexName = name;
            keyTypeName = keyType.FullName;
        }

        /// <summary>
        /// インデックスにエントリを追加する。
        /// </summary>
        /// <param name="keyHash">キーのハッシュ値</param>
        /// <param name="keyString">キーの文字列表現</param>
        /// <param name="recordIndices">レコードインデックスの配列</param>
        public void AddEntry(int keyHash, string keyString, int[] recordIndices)
        {
            entries.Add(new IndexEntry
            {
                keyHash = keyHash,
                keyString = keyString,
                recordIndices = recordIndices
            });
            isInitialized = false;
        }

        /// <summary>
        /// インデックスをクリアする。
        /// </summary>
        public void Clear()
        {
            entries.Clear();
            hashToIndices?.Clear();
            stringToIndices?.Clear();
            isInitialized = false;
        }

        /// <summary>
        /// ハッシュ値でレコードインデックスを検索する。
        /// </summary>
        /// <param name="keyHash">キーのハッシュ値</param>
        /// <returns>レコードインデックスの配列、見つからない場合は空の配列</returns>
        public int[] FindByHash(int keyHash)
        {
            EnsureInitialized();
            return hashToIndices.TryGetValue(keyHash, out var indices) ? indices : Array.Empty<int>();
        }

        /// <summary>
        /// 文字列キーでレコードインデックスを検索する。
        /// </summary>
        /// <param name="keyString">キーの文字列表現</param>
        /// <returns>レコードインデックスの配列、見つからない場合は空の配列</returns>
        public int[] FindByString(string keyString)
        {
            EnsureInitialized();
            return stringToIndices.TryGetValue(keyString, out var indices) ? indices : Array.Empty<int>();
        }

        /// <summary>
        /// キーでレコードインデックスを検索する（型安全版）。
        /// 文字列キーベースで検索し、ハッシュ衝突を回避する。
        /// </summary>
        /// <typeparam name="TKey">キーの型</typeparam>
        /// <param name="key">検索するキー</param>
        /// <returns>レコードインデックスの配列、見つからない場合は空の配列</returns>
        public int[] FindByKey<TKey>(TKey key)
        {
            if (key == null)
                return Array.Empty<int>();

            EnsureInitialized();

            // 文字列キーで検索（ハッシュ衝突回避）
            var keyString = key.ToString();
            return FindByString(keyString);
        }

        /// <summary>
        /// キーでレコードインデックスを検索する（object版）。
        /// 文字列キーベースで検索し、ハッシュ衝突を回避する。
        /// </summary>
        /// <param name="key">検索するキー</param>
        /// <returns>レコードインデックスの配列、見つからない場合は空の配列</returns>
        public int[] FindByKey(object key)
        {
            if (key == null)
                return Array.Empty<int>();

            EnsureInitialized();

            var keyString = key.ToString();
            return FindByString(keyString);
        }

        /// <summary>
        /// 全エントリを取得する。
        /// </summary>
        public IReadOnlyList<IndexEntry> GetAllEntries() => entries;

        private void EnsureInitialized()
        {
            if (isInitialized)
                return;

            hashToIndices = new Dictionary<int, int[]>(entries.Count);
            stringToIndices = new Dictionary<string, int[]>(entries.Count);

            foreach (var entry in entries)
            {
                hashToIndices[entry.keyHash] = entry.recordIndices;
                if (!string.IsNullOrEmpty(entry.keyString))
                    stringToIndices[entry.keyString] = entry.recordIndices;
            }

            isInitialized = true;
        }

        /// <summary>
        /// インデックスエントリ。
        /// </summary>
        [Serializable]
        public class IndexEntry
        {
            /// <summary>
            /// キーのハッシュ値。
            /// </summary>
            public int keyHash;

            /// <summary>
            /// キーの文字列表現（デバッグ・文字列検索用）。
            /// </summary>
            public string keyString;

            /// <summary>
            /// このキーに対応するレコードのインデックス配列。
            /// </summary>
            public int[] recordIndices;
        }
    }

    /// <summary>
    /// テーブルの全SecondaryKeyインデックスを保持するコンテナ。
    /// </summary>
    [Serializable]
    public class IndexContainer
    {
        [SerializeField]
        private List<IndexData> indices = new();

        /// <summary>
        /// インデックス数。
        /// </summary>
        public int Count => indices.Count;

        /// <summary>
        /// 名前でインデックスを取得する。
        /// </summary>
        /// <param name="name">インデックス名</param>
        /// <returns>インデックスデータ、見つからない場合はnull</returns>
        public IndexData GetIndex(string name)
        {
            foreach (var index in indices)
            {
                if (index.IndexName == name)
                    return index;
            }
            return null;
        }

        /// <summary>
        /// インデックスを追加または更新する。
        /// </summary>
        /// <param name="indexData">追加するインデックスデータ</param>
        public void SetIndex(IndexData indexData)
        {
            for (var i = 0; i < indices.Count; i++)
            {
                if (indices[i].IndexName == indexData.IndexName)
                {
                    indices[i] = indexData;
                    return;
                }
            }
            indices.Add(indexData);
        }

        /// <summary>
        /// 指定した名前のインデックスを削除する。
        /// </summary>
        /// <param name="name">削除するインデックス名</param>
        /// <returns>削除に成功した場合はtrue</returns>
        public bool RemoveIndex(string name)
        {
            for (var i = 0; i < indices.Count; i++)
            {
                if (indices[i].IndexName == name)
                {
                    indices.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 全インデックスをクリアする。
        /// </summary>
        public void Clear()
        {
            indices.Clear();
        }

        /// <summary>
        /// 全インデックスを取得する。
        /// </summary>
        public IReadOnlyList<IndexData> GetAllIndices() => indices;
    }
}
