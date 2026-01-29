using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// SecondaryKeyインデックスを構築するビルダークラス。
    /// </summary>
    public static class IndexBuilder
    {
        private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// レコード配列からSecondaryKeyインデックスを構築する。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <param name="records">レコード配列</param>
        /// <returns>構築されたインデックスコンテナ</returns>
        public static IndexContainer BuildIndices<T>(T[] records)
        {
            var container = new IndexContainer();
            var type = typeof(T);
            var secondaryKeys = FindSecondaryKeyMembers(type);

            foreach (var (member, attribute) in secondaryKeys)
            {
                var indexName = attribute.Name ?? member.Name;
                var keyType = GetMemberType(member);
                var indexData = BuildIndex(records, member, indexName, keyType, attribute.AllowDuplicates);
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
            var secondaryKeys = FindSecondaryKeyMembers(type);

            foreach (var (member, attribute) in secondaryKeys)
            {
                var name = attribute.Name ?? member.Name;
                if (name != indexName)
                    continue;

                var keyType = GetMemberType(member);
                return BuildIndex(records, member, name, keyType, attribute.AllowDuplicates);
            }

            Debug.LogWarning($"SecondaryKey '{indexName}' not found in type {type.Name}");
            return null;
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
                var attr = field.GetCustomAttribute<SecondaryKeyAttribute>();
                if (attr != null)
                    result.Add((field, attr));
            }

            foreach (var property in type.GetProperties(MemberFlags))
            {
                var attr = property.GetCustomAttribute<SecondaryKeyAttribute>();
                if (attr != null)
                    result.Add((property, attr));
            }

            return result;
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

        private static IndexData BuildIndex<T>(
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

            // キー値ごとにレコードインデックスをグループ化
            var keyGroups = new Dictionary<int, List<int>>();
            var keyStrings = new Dictionary<int, string>();

            for (var i = 0; i < records.Length; i++)
            {
                var record = records[i];
                if (record == null)
                    continue;

                var keyValue = getValue(record);
                if (keyValue == null)
                    continue;

                var keyHash = keyValue.GetHashCode();
                var keyString = keyValue.ToString();

                if (!keyGroups.TryGetValue(keyHash, out var indices))
                {
                    indices = new List<int>();
                    keyGroups[keyHash] = indices;
                    keyStrings[keyHash] = keyString;
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

            // インデックスデータに追加
            foreach (var (keyHash, indices) in keyGroups)
            {
                var keyString = keyStrings[keyHash];
                indexData.AddEntry(keyHash, keyString, indices.ToArray());
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
    }
}
