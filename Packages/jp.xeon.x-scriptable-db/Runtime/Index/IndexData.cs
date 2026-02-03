using System;
using System.Collections.Generic;
using System.Globalization;
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
            // インデックス構築時と同じインバリアントカルチャで変換
            var keyString = ConvertToInvariantString(key);
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

            // インデックス構築時と同じインバリアントカルチャで変換
            var keyString = ConvertToInvariantString(key);
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
        /// カルチャ非依存の文字列変換を行う。
        /// IndexBuilderと同じ変換ロジックを使用する。
        /// float/doubleはラウンドトリップフォーマットを使用して精度を保持する。
        /// </summary>
        private static string ConvertToInvariantString(object value)
        {
            return value switch
            {
                float f => f.ToString("R", CultureInfo.InvariantCulture),
                double d => d.ToString("R", CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }
    }
}
