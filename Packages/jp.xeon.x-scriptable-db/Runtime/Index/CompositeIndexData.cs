using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// 複合インデックスのシリアライズ可能なデータ構造。
    /// 複数のキー値の組み合わせからレコードインデックスへのマッピングを保持する。
    /// </summary>
    /// <remarks>
    /// 文字列キーを主キーとして使用し、ハッシュ衝突の問題を回避。
    /// 決定的ハッシュを使用して環境間の互換性を保証。
    /// </remarks>
    [Serializable]
    public class CompositeIndexData
    {
        private const char KeyDelimiter = '\x1F'; // Unit Separator (ASCII 31)
        private const string NullPlaceholder = "\x00NULL\x00";

        [SerializeField]
        private string indexName;

        [SerializeField]
        private List<string> keyTypeNames = new();

        [SerializeField]
        private List<string> memberNames = new();

        [SerializeField]
        private List<CompositeIndexEntry> entries = new();

        /// <summary>
        /// インデックスの名前。
        /// </summary>
        public string IndexName => indexName;

        /// <summary>
        /// キーの型名リスト。
        /// </summary>
        public IReadOnlyList<string> KeyTypeNames => keyTypeNames;

        /// <summary>
        /// メンバー名リスト。
        /// </summary>
        public IReadOnlyList<string> MemberNames => memberNames;

        /// <summary>
        /// エントリ数。
        /// </summary>
        public int Count => entries.Count;

        /// <summary>
        /// 複合キーを構成するフィールド数。
        /// </summary>
        public int KeyCount => memberNames.Count;

        // ランタイム用のハッシュマップ（シリアライズされない）
        [NonSerialized]
        private Dictionary<string, int[]> stringToIndices;

        [NonSerialized]
        private bool isInitialized;

        public CompositeIndexData() { }

        /// <summary>
        /// CompositeIndexData を作成する。
        /// </summary>
        /// <param name="name">インデックスの名前</param>
        /// <param name="members">メンバー名と型のリスト</param>
        public CompositeIndexData(string name, List<(string memberName, Type keyType)> members)
        {
            indexName = name;
            foreach (var (memberName, keyType) in members)
            {
                memberNames.Add(memberName);
                keyTypeNames.Add(keyType.FullName);
            }
        }

        /// <summary>
        /// インデックスにエントリを追加する。
        /// </summary>
        /// <param name="compositeKeyHash">複合キーのハッシュ値（後方互換性用、使用されない）</param>
        /// <param name="compositeKeyString">複合キーの文字列表現</param>
        /// <param name="keyValues">各キー値の文字列表現</param>
        /// <param name="recordIndices">レコードインデックスの配列</param>
        public void AddEntry(int compositeKeyHash, string compositeKeyString, string[] keyValues, int[] recordIndices)
        {
            entries.Add(new CompositeIndexEntry
            {
                compositeKeyHash = compositeKeyHash,
                compositeKeyString = compositeKeyString,
                keyValues = keyValues,
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
            stringToIndices?.Clear();
            isInitialized = false;
        }

        /// <summary>
        /// 複合キーでレコードインデックスを検索する。
        /// </summary>
        /// <param name="keys">検索するキー値の配列</param>
        /// <returns>レコードインデックスの配列、見つからない場合は空の配列</returns>
        public int[] FindByKeys(params object[] keys)
        {
            if (keys == null || keys.Length != memberNames.Count)
                return Array.Empty<int>();

            var compositeString = ComputeCompositeString(keys);
            return FindByString(compositeString);
        }

        /// <summary>
        /// ハッシュ値でレコードインデックスを検索する（後方互換性用）。
        /// </summary>
        /// <param name="compositeKeyHash">複合キーのハッシュ値</param>
        /// <returns>レコードインデックスの配列、見つからない場合は空の配列</returns>
        [Obsolete("ハッシュ衝突の問題があるため、FindByKeys または FindByString を使用してください。")]
        public int[] FindByHash(int compositeKeyHash)
        {
            EnsureInitialized();

            // ハッシュ検索は廃止予定。全エントリをスキャンして一致するものを返す
            foreach (var entry in entries)
            {
                if (entry.compositeKeyHash == compositeKeyHash)
                    return entry.recordIndices;
            }
            return Array.Empty<int>();
        }

        /// <summary>
        /// 文字列キーでレコードインデックスを検索する。
        /// </summary>
        /// <param name="compositeKeyString">複合キーの文字列表現</param>
        /// <returns>レコードインデックスの配列、見つからない場合は空の配列</returns>
        public int[] FindByString(string compositeKeyString)
        {
            EnsureInitialized();
            return stringToIndices.TryGetValue(compositeKeyString, out var indices) ? indices : Array.Empty<int>();
        }

        /// <summary>
        /// 全エントリを取得する。
        /// </summary>
        public IReadOnlyList<CompositeIndexEntry> GetAllEntries() => entries;

        /// <summary>
        /// 複合キーの決定的ハッシュ値を計算する。
        /// </summary>
        /// <remarks>
        /// 環境に依存しない決定的なハッシュを計算。
        /// 永続化には適さないため、ランタイムキャッシュ用途のみに使用。
        /// </remarks>
        /// <param name="keys">キー値の配列</param>
        /// <returns>ハッシュ値</returns>
        public static int ComputeCompositeHash(params object[] keys)
        {
            if (keys == null || keys.Length == 0)
                return 0;

            // 文字列表現を生成してからハッシュを計算（決定的）
            var compositeString = ComputeCompositeString(keys);
            return GetDeterministicHashCode(compositeString);
        }

        /// <summary>
        /// 複合キーの文字列表現を生成する。
        /// </summary>
        /// <remarks>
        /// Unit Separator (ASCII 31) を区切り文字として使用。
        /// 通常のユーザー入力には含まれないため、衝突を回避。
        /// </remarks>
        /// <param name="keys">キー値の配列</param>
        /// <returns>文字列表現</returns>
        public static string ComputeCompositeString(params object[] keys)
        {
            if (keys == null || keys.Length == 0)
                return string.Empty;

            var sb = new StringBuilder();
            for (var i = 0; i < keys.Length; i++)
            {
                if (i > 0)
                    sb.Append(KeyDelimiter);

                if (keys[i] == null)
                    sb.Append(NullPlaceholder);
                else
                    sb.Append(keys[i].ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// デバッグ用の可読性のある文字列表現を生成する。
        /// </summary>
        /// <param name="keys">キー値の配列</param>
        /// <returns>可読性のある文字列表現</returns>
        public static string ComputeReadableString(params object[] keys)
        {
            if (keys == null || keys.Length == 0)
                return string.Empty;

            var parts = new string[keys.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                parts[i] = keys[i]?.ToString() ?? "null";
            }
            return string.Join("|", parts);
        }

        /// <summary>
        /// 決定的なハッシュコードを計算する。
        /// </summary>
        /// <remarks>
        /// string.GetHashCode() は .NET 実装によって異なる値を返す可能性があるため、
        /// 独自の決定的ハッシュ関数を使用。
        /// </remarks>
        private static int GetDeterministicHashCode(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;

            unchecked
            {
                var hash1 = 5381;
                var hash2 = hash1;

                for (var i = 0; i < str.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i + 1 < str.Length)
                        hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        private void EnsureInitialized()
        {
            if (isInitialized)
                return;

            stringToIndices = new Dictionary<string, int[]>(entries.Count);

            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.compositeKeyString))
                    stringToIndices[entry.compositeKeyString] = entry.recordIndices;
            }

            isInitialized = true;
        }

        /// <summary>
        /// 複合インデックスエントリ。
        /// </summary>
        [Serializable]
        public class CompositeIndexEntry
        {
            /// <summary>
            /// 複合キーのハッシュ値（後方互換性用）。
            /// </summary>
            public int compositeKeyHash;

            /// <summary>
            /// 複合キーの文字列表現（主キー）。
            /// </summary>
            public string compositeKeyString;

            /// <summary>
            /// 各キー値の文字列表現（デバッグ用）。
            /// </summary>
            public string[] keyValues;

            /// <summary>
            /// このキーに対応するレコードのインデックス配列。
            /// </summary>
            public int[] recordIndices;
        }
    }

    /// <summary>
    /// テーブルの全複合インデックスを保持するコンテナ。
    /// </summary>
    [Serializable]
    public class CompositeIndexContainer
    {
        [SerializeField]
        private List<CompositeIndexData> indices = new();

        /// <summary>
        /// インデックス数。
        /// </summary>
        public int Count => indices.Count;

        /// <summary>
        /// 名前でインデックスを取得する。
        /// </summary>
        /// <param name="name">インデックス名</param>
        /// <returns>インデックスデータ、見つからない場合はnull</returns>
        public CompositeIndexData GetIndex(string name)
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
        public void SetIndex(CompositeIndexData indexData)
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
        public IReadOnlyList<CompositeIndexData> GetAllIndices() => indices;
    }
}
