using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// SecondaryKeyインデックスを構築するビルダークラス。
    /// 単一フィールドインデックスと複合インデックスの両方をサポート。
    /// </summary>
    public static class IndexBuilder
    {
        private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// レコード配列からSecondaryKeyインデックスを構築する。
        /// 同じ名前を持つ複数のフィールドは複合インデックスとして構築される。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <param name="records">レコード配列</param>
        /// <returns>構築されたインデックスコンテナ</returns>
        public static IndexContainer BuildIndices<T>(T[] records)
        {
            var container = new IndexContainer();
            var type = typeof(T);
            var groups = GroupSecondaryKeyMembers(type);

            foreach (var (indexName, members) in groups)
            {
                var allowDuplicates = members[0].attribute.AllowDuplicates;
                IndexData indexData;

                if (members.Count == 1)
                {
                    var (member, _) = members[0];
                    var keyType = GetMemberType(member);
                    indexData = BuildSingleFieldIndex(records, member, indexName, keyType, allowDuplicates);
                }
                else
                {
                    indexData = BuildCompositeIndex(records, indexName, members, allowDuplicates);
                }

                container.SetIndex(indexData);
            }

            return container;
        }

        /// <summary>
        /// 特定のSecondaryKeyに対するインデックスを構築する。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <param name="records">レコード配列</param>
        /// <param name="indexName">インデックス名</param>
        /// <returns>構築されたインデックスデータ、見つからない場合はnull</returns>
        public static IndexData BuildIndex<T>(T[] records, string indexName)
        {
            var type = typeof(T);
            var groups = GroupSecondaryKeyMembers(type);

            if (!groups.TryGetValue(indexName, out var members))
            {
                Debug.LogWarning($"SecondaryKey '{indexName}' not found in type {type.Name}");
                return null;
            }

            var allowDuplicates = members[0].attribute.AllowDuplicates;
            if (members.Count == 1)
            {
                var (member, _) = members[0];
                var keyType = GetMemberType(member);
                return BuildSingleFieldIndex(records, member, indexName, keyType, allowDuplicates);
            }

            return BuildCompositeIndex(records, indexName, members, allowDuplicates);
        }

        /// <summary>
        /// 型からSecondaryKeyメンバーを検索する。
        /// </summary>
        /// <param name="type">検索する型</param>
        /// <returns>SecondaryKeyメンバーと属性のリスト</returns>
        public static List<(MemberInfo member, SecondaryKeyAttribute attribute)> FindSecondaryKeyMembers(Type type)
        {
            var result = new List<(MemberInfo, SecondaryKeyAttribute)>();

            foreach (var field in type.GetFields(MemberFlags))
            {
                var attrs = field.GetCustomAttributes<SecondaryKeyAttribute>();
                foreach (var attr in attrs)
                    result.Add((field, attr));
            }

            foreach (var property in type.GetProperties(MemberFlags))
            {
                var attrs = property.GetCustomAttributes<SecondaryKeyAttribute>();
                foreach (var attr in attrs)
                    result.Add((property, attr));
            }

            return result;
        }

        /// <summary>
        /// SecondaryKeyメンバーをインデックス名でグループ化して取得する。
        /// 同じインデックス名を持つメンバーは複合インデックスとして扱われる。
        /// </summary>
        /// <param name="type">検索する型</param>
        /// <returns>インデックス名をキーとするグループ化されたメンバーの辞書</returns>
        public static Dictionary<string, List<(MemberInfo member, SecondaryKeyAttribute attribute)>> GroupSecondaryKeyMembers(Type type)
        {
            var result = new Dictionary<string, List<(MemberInfo member, SecondaryKeyAttribute attribute)>>();
            var allMembers = FindSecondaryKeyMembers(type);

            foreach (var (member, attr) in allMembers)
            {
                var indexName = attr.Name ?? member.Name;
                if (!result.TryGetValue(indexName, out var list))
                {
                    list = new List<(MemberInfo member, SecondaryKeyAttribute attribute)>();
                    result[indexName] = list;
                }
                list.Add((member, attr));
            }

            foreach (var key in result.Keys.ToList())
                result[key] = result[key].OrderBy(item => item.attribute.Order).ToList();

            return result;
        }

        /// <summary>
        /// インデックスが複合キーかどうかを判定する。
        /// </summary>
        /// <param name="indexName">インデックス名</param>
        /// <param name="type">レコードの型</param>
        /// <returns>複合インデックスの場合はtrue</returns>
        public static bool IsCompositeIndex(string indexName, Type type)
        {
            var groups = GroupSecondaryKeyMembers(type);
            return groups.TryGetValue(indexName, out var members) && members.Count > 1;
        }

        /// <summary>
        /// 型がSecondaryKeyを持つかどうかを確認する。
        /// </summary>
        /// <param name="type">確認する型</param>
        /// <returns>SecondaryKeyを持つ場合はtrue</returns>
        public static bool HasSecondaryKeys(Type type)
        {
            foreach (var field in type.GetFields(MemberFlags))
            {
                if (field.GetCustomAttribute<SecondaryKeyAttribute>() != null)
                    return true;
            }

            foreach (var property in type.GetProperties(MemberFlags))
            {
                if (property.GetCustomAttribute<SecondaryKeyAttribute>() != null)
                    return true;
            }

            return false;
        }

        private static IndexData BuildSingleFieldIndex<T>(
            T[] records,
            MemberInfo member,
            string indexName,
            Type keyType,
            bool allowDuplicates)
        {
            var indexData = new IndexData(indexName, keyType);
            var getValue = CreateGetter<T>(member);

            if (getValue == null)
            {
                Debug.LogError($"Failed to create getter for member {member.Name}");
                return indexData;
            }

            // 文字列キーをプライマリキーとして使用（ハッシュ衝突回避）
            var keyGroups = new Dictionary<string, List<int>>();

            for (var i = 0; i < records.Length; i++)
            {
                var record = records[i];
                if (record == null)
                    continue;

                var keyValue = getValue(record);
                if (keyValue == null)
                    continue;

                var keyString = ConvertToInvariantString(keyValue);

                if (!keyGroups.TryGetValue(keyString, out var indices))
                {
                    indices = new List<int>();
                    keyGroups[keyString] = indices;
                }

                if (!allowDuplicates && indices.Count > 0)
                {
                    Debug.LogWarning(
                        $"Duplicate SecondaryKey '{indexName}' value '{keyString}' at index {i}. " +
                        $"Set AllowDuplicates=true to allow multiple records per key.");
                    continue;
                }

                indices.Add(i);
            }

            foreach (var (keyString, indices) in keyGroups)
            {
                var keyHash = CompositeKeyHelper.GetDeterministicHashCode(keyString);
                indexData.AddEntry(keyHash, keyString, indices.ToArray());
            }

            return indexData;
        }

        private static IndexData BuildCompositeIndex<T>(
            T[] records,
            string indexName,
            List<(MemberInfo member, SecondaryKeyAttribute attribute)> members,
            bool allowDuplicates)
        {
            var indexData = new IndexData(indexName, typeof(string));
            var getters = members.Select(m => CreateGetter<T>(m.member)).ToArray();

            if (getters.Any(getter => getter == null))
            {
                Debug.LogError($"Failed to create getter for composite index '{indexName}'.");
                return indexData;
            }

            // 文字列キーをプライマリキーとして使用（ハッシュ衝突完全回避）
            var keyGroups = new Dictionary<string, List<int>>();

            for (var i = 0; i < records.Length; i++)
            {
                var record = records[i];
                if (record == null)
                    continue;

                var keyParts = new object[members.Count];
                var hasNull = false;
                for (var j = 0; j < members.Count; j++)
                {
                    keyParts[j] = getters[j](record);
                    if (keyParts[j] == null)
                        hasNull = true;
                }

                if (hasNull)
                    continue;

                var compositeString = CompositeKeyHelper.ComputeCompositeString(keyParts);

                if (!keyGroups.TryGetValue(compositeString, out var indices))
                {
                    indices = new List<int>();
                    keyGroups[compositeString] = indices;
                }

                if (!allowDuplicates && indices.Count > 0)
                {
                    var readableKey = CompositeKeyHelper.ComputeReadableString(keyParts);
                    Debug.LogWarning(
                        $"Duplicate CompositeSecondaryKey '{indexName}' value '{readableKey}' at index {i}. " +
                        $"Set AllowDuplicates=true to allow multiple records per key.");
                    continue;
                }

                indices.Add(i);
            }

            foreach (var (compositeString, indices) in keyGroups)
            {
                var keyHash = CompositeKeyHelper.GetDeterministicHashCode(compositeString);
                indexData.AddEntry(keyHash, compositeString, indices.ToArray());
            }

            return indexData;
        }

        private static Func<T, object> CreateGetter<T>(MemberInfo member)
        {
            return member switch
            {
                FieldInfo field => record => field.GetValue(record),
                PropertyInfo property => record => property.GetValue(record),
                _ => null
            };
        }

        private static Type GetMemberType(MemberInfo member)
        {
            return member switch
            {
                FieldInfo field => field.FieldType,
                PropertyInfo property => property.PropertyType,
                _ => null
            };
        }

        /// <summary>
        /// カルチャ非依存の文字列変換を行う。
        /// float/doubleはラウンドトリップフォーマットを使用して精度を保持する。
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
