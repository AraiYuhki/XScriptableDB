using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// SecondaryKeyインデックスおよび複合インデックスを構築するビルダークラス。
    /// </summary>
    public static class IndexBuilder
    {
        private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags TypeFlags = BindingFlags.Public | BindingFlags.NonPublic;

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

        // ========================================
        // 複合インデックス関連メソッド
        // ========================================

        /// <summary>
        /// レコード配列から複合インデックスを構築する。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <param name="records">レコード配列</param>
        /// <returns>構築された複合インデックスコンテナ</returns>
        public static CompositeIndexContainer BuildCompositeIndices<T>(T[] records)
        {
            var container = new CompositeIndexContainer();
            var type = typeof(T);
            var compositeIndexAttributes = FindCompositeIndexAttributes(type);

            foreach (var attribute in compositeIndexAttributes)
            {
                var indexData = BuildCompositeIndex(records, type, attribute);
                if (indexData != null)
                    container.SetIndex(indexData);
            }

            return container;
        }

        /// <summary>
        /// 特定の複合インデックスを構築する。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <param name="records">レコード配列</param>
        /// <param name="indexName">インデックス名</param>
        /// <returns>構築された複合インデックスデータ、見つからない場合はnull</returns>
        public static CompositeIndexData BuildCompositeIndex<T>(T[] records, string indexName)
        {
            var type = typeof(T);
            var compositeIndexAttributes = FindCompositeIndexAttributes(type);

            foreach (var attribute in compositeIndexAttributes)
            {
                if (attribute.Name == indexName)
                    return BuildCompositeIndex(records, type, attribute);
            }

            Debug.LogWarning($"CompositeIndex '{indexName}' not found in type {type.Name}");
            return null;
        }

        /// <summary>
        /// 型からCompositeIndex属性を検索する。
        /// </summary>
        /// <param name="type">検索する型</param>
        /// <returns>CompositeIndex属性のリスト</returns>
        public static List<CompositeIndexAttribute> FindCompositeIndexAttributes(Type type)
        {
            var result = new List<CompositeIndexAttribute>();
            var attributes = type.GetCustomAttributes<CompositeIndexAttribute>(true);

            foreach (var attr in attributes)
            {
                result.Add(attr);
            }

            return result;
        }

        /// <summary>
        /// 型が複合インデックスを持つかどうかを確認する。
        /// </summary>
        /// <param name="type">確認する型</param>
        /// <returns>複合インデックスを持つ場合はtrue</returns>
        public static bool HasCompositeIndices(Type type)
        {
            return type.GetCustomAttribute<CompositeIndexAttribute>(true) != null;
        }

        private static CompositeIndexData BuildCompositeIndex<T>(
            T[] records,
            Type type,
            CompositeIndexAttribute attribute)
        {
            // メンバー情報を収集
            var members = new List<(MemberInfo member, string name, Type keyType)>();
            foreach (var memberName in attribute.MemberNames)
            {
                var member = FindMember(type, memberName);
                if (member == null)
                {
                    Debug.LogError($"Member '{memberName}' not found in type {type.Name} for CompositeIndex '{attribute.Name}'");
                    return null;
                }

                var keyType = GetMemberType(member);
                members.Add((member, memberName, keyType));
            }

            // インデックスデータを作成
            var memberInfoList = new List<(string memberName, Type keyType)>();
            foreach (var (_, name, keyType) in members)
            {
                memberInfoList.Add((name, keyType));
            }
            var indexData = new CompositeIndexData(attribute.Name, memberInfoList);

            // ゲッターを作成
            var getters = new List<Func<T, object>>();
            foreach (var (member, _, _) in members)
            {
                var getter = CreateGetter<T>(member);
                if (getter == null)
                {
                    Debug.LogError($"Failed to create getter for member {member.Name}");
                    return null;
                }
                getters.Add(getter);
            }

            // キー値ごとにレコードインデックスをグループ化
            // 文字列キーを使用してハッシュ衝突を回避
            var keyGroups = new Dictionary<string, List<int>>();
            var keyValueGroups = new Dictionary<string, string[]>();

            for (var i = 0; i < records.Length; i++)
            {
                var record = records[i];
                if (record == null)
                    continue;

                // 各キー値を取得
                var keyValues = new object[getters.Count];
                var keyStrings = new string[getters.Count];
                var hasNullKey = false;

                for (var j = 0; j < getters.Count; j++)
                {
                    var value = getters[j](record);
                    keyValues[j] = value;
                    keyStrings[j] = value?.ToString() ?? "null";

                    if (value == null)
                        hasNullKey = true;
                }

                // null キーを含む場合はスキップ
                // 注: null を含むキーはインデックスに追加されません
                if (hasNullKey)
                    continue;

                var compositeString = CompositeIndexData.ComputeCompositeString(keyValues);

                if (!keyGroups.TryGetValue(compositeString, out var indices))
                {
                    indices = new List<int>();
                    keyGroups[compositeString] = indices;
                    keyValueGroups[compositeString] = keyStrings;
                }

                if (!attribute.AllowDuplicates && indices.Count > 0)
                {
                    Debug.LogWarning(
                        $"Duplicate CompositeIndex '{attribute.Name}' value '{CompositeIndexData.ComputeReadableString(keyValues)}' at index {i}. " +
                        $"Set AllowDuplicates=true to allow multiple records per key combination.");
                    continue;
                }

                indices.Add(i);
            }

            // インデックスデータに追加
            foreach (var (compositeString, indices) in keyGroups)
            {
                var keyValues = keyValueGroups[compositeString];
                // ハッシュは後方互換性のため計算するが、検索には使用しない
                var keyObjects = new object[keyValues.Length];
                for (var i = 0; i < keyValues.Length; i++)
                    keyObjects[i] = keyValues[i];
                var compositeHash = CompositeIndexData.ComputeCompositeHash(keyObjects);
                indexData.AddEntry(compositeHash, compositeString, keyValues, indices.ToArray());
            }

            return indexData;
        }

        private static MemberInfo FindMember(Type type, string memberName)
        {
            // フィールドを検索
            var field = type.GetField(memberName, MemberFlags);
            if (field != null)
                return field;

            // プロパティを検索
            var property = type.GetProperty(memberName, MemberFlags);
            if (property != null)
                return property;

            return null;
        }
    }
}
