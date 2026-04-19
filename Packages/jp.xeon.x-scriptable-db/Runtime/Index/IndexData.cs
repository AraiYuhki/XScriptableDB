using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// SecondaryKeyインデックス用のシリアライズ可能なデータ構造。
    /// キー値からレコードインデックスへのマッピングを保持します。
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
        /// エントリの数。
        /// </summary>
        public int Count => entries.Count;

        // Runtime hash maps (not serialized)
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
        /// インデックスにエントリを追加します。
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
        /// インデックスをクリアします。
        /// </summary>
        public void Clear()
        {
            entries.Clear();
            hashToIndices?.Clear();
            stringToIndices?.Clear();
            isInitialized = false;
        }

        /// <summary>
        /// ハッシュ値によるレコードインデックスの検索。
        /// </summary>
        /// <param name="keyHash">キーのハッシュ値</param>
        /// <returns>レコードインデックスの配列。見つからない場合は空の配列</returns>
        public int[] FindByHash(int keyHash)
        {
            EnsureInitialized();
            return hashToIndices.TryGetValue(keyHash, out var indices) ? indices : Array.Empty<int>();
        }

        /// <summary>
        /// 文字列キーによるレコードインデックスの検索。
        /// </summary>
        /// <param name="keyString">キーの文字列表現</param>
        /// <returns>レコードインデックスの配列。見つからない場合は空の配列</returns>
        public int[] FindByString(string keyString)
        {
            EnsureInitialized();
            return stringToIndices.TryGetValue(keyString, out var indices) ? indices : Array.Empty<int>();
        }

        /// <summary>
        /// キーによるレコードインデックスの検索（型安全なオーバーロード）。
        /// ハッシュの衝突を避けるために文字列キーベースのルックアップを使用します。
        /// </summary>
        /// <typeparam name="TKey">キーの型</typeparam>
        /// <param name="key">検索するキー</param>
        /// <returns>レコードインデックスの配列。見つからない場合は空の配列</returns>
        public int[] FindByKey<TKey>(TKey key)
        {
            if (key == null)
                return Array.Empty<int>();

            EnsureInitialized();

            // Search by string key (avoids hash collisions)
            // Convert using the same invariant culture used at index build time
            var keyString = ConvertToInvariantString(key);
            return FindByString(keyString);
        }

        /// <summary>
        /// キーによるレコードインデックスの検索（オブジェクトオーバーロード）。
        /// ハッシュの衝突を避けるために文字列キーベースのルックアップを使用します。
        /// </summary>
        /// <param name="key">検索するキー</param>
        /// <returns>レコードインデックスの配列。見つからない場合は空の配列</returns>
        public int[] FindByKey(object key)
        {
            if (key == null)
                return Array.Empty<int>();

            EnsureInitialized();

            // Convert using the same invariant culture used at index build time
            var keyString = ConvertToInvariantString(key);
            return FindByString(keyString);
        }

        /// <summary>
        /// すべてのエントリを取得します。
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
        /// 値をカルチャに依存しない文字列に変換します。
        /// IndexBuilderと同じ変換ロジックを使用します。
        /// float/doubleは精度を保つためにラウンドトリップ形式を使用します。
        /// </summary>
        private static string ConvertToInvariantString(object value)
        {
            return value switch
            {
                float f => f.ToString("R", CultureInfo.InvariantCulture),
                double d => d.ToString("R", CultureInfo.InvariantCulture),
                DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
                SerializableDateTime sdt => sdt.Ticks.ToString(CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }
    }
}
