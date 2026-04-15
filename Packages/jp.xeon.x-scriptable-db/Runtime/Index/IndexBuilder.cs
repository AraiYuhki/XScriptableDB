using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// Builder class for constructing SecondaryKey indexes.
    /// Supports both single-field indexes and composite indexes.
    /// </summary>
    public static class IndexBuilder
    {
        private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// Builds SecondaryKey indexes from a record array.
        /// Multiple fields sharing the same name are built as a composite index.
        /// </summary>
        /// <typeparam name="T">Record type</typeparam>
        /// <param name="records">Record array</param>
        /// <returns>The built index container</returns>
        public static IndexContainer BuildIndices<T>(T[] records)
        {
            var container = new IndexContainer();
            var type = typeof(T);
            var groups = GroupSecondaryKeyMembers(type);

            foreach (var (indexName, members) in groups)
            {
                var allowDuplicates = ResolveAllowDuplicates(indexName, members);
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
        /// Builds an index for a specific SecondaryKey.
        /// </summary>
        /// <typeparam name="T">Record type</typeparam>
        /// <param name="records">Record array</param>
        /// <param name="indexName">Index name</param>
        /// <returns>The built index data, or null if not found</returns>
        public static IndexData BuildIndex<T>(T[] records, string indexName)
        {
            var type = typeof(T);
            var groups = GroupSecondaryKeyMembers(type);

            if (!groups.TryGetValue(indexName, out var members))
            {
                Debug.LogWarning($"SecondaryKey '{indexName}' not found in type {type.Name}");
                return null;
            }

            var allowDuplicates = ResolveAllowDuplicates(indexName, members);
            if (members.Count == 1)
            {
                var (member, _) = members[0];
                var keyType = GetMemberType(member);
                return BuildSingleFieldIndex(records, member, indexName, keyType, allowDuplicates);
            }

            return BuildCompositeIndex(records, indexName, members, allowDuplicates);
        }

        /// <summary>
        /// Finds SecondaryKey members on a type.
        /// </summary>
        /// <param name="type">Type to search</param>
        /// <returns>List of SecondaryKey members and their attributes</returns>
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
        /// Gets SecondaryKey members grouped by index name.
        /// Members sharing the same index name are treated as a composite index.
        /// </summary>
        /// <param name="type">Type to search</param>
        /// <returns>Dictionary of grouped members keyed by index name</returns>
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
            {
                // Use OrderBy + ThenBy with member name as tiebreaker (ensures deterministic order)
                result[key] = result[key]
                    .OrderBy(item => item.attribute.Order)
                    .ThenBy(item => item.member.Name)
                    .ToList();

                // Warn if any members share the same Order value
                var orders = result[key].Select(item => item.attribute.Order).ToList();
                if (orders.Distinct().Count() != orders.Count)
                {
                    Debug.LogWarning(
                        $"Composite index '{key}' has members with duplicate Order values. " +
                        $"Member name is used as tie-breaker, but explicit unique Order values are recommended.");
                }
            }

            return result;
        }

        /// <summary>
        /// Determines whether an index is a composite key.
        /// </summary>
        /// <param name="indexName">Index name</param>
        /// <param name="type">Record type</param>
        /// <returns>True if it is a composite index</returns>
        public static bool IsCompositeIndex(string indexName, Type type)
        {
            var groups = GroupSecondaryKeyMembers(type);
            return groups.TryGetValue(indexName, out var members) && members.Count > 1;
        }

        /// <summary>
        /// Checks whether a type has any SecondaryKeys.
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if the type has at least one SecondaryKey</returns>
        public static bool HasSecondaryKeys(Type type)
        {
            foreach (var field in type.GetFields(MemberFlags))
            {
                if (field.GetCustomAttributes<SecondaryKeyAttribute>().Any())
                    return true;
            }

            foreach (var property in type.GetProperties(MemberFlags))
            {
                if (property.GetCustomAttributes<SecondaryKeyAttribute>().Any())
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Resolves the AllowDuplicates setting for composite index members.
        /// Enforces a consistent setting across all members; if inconsistent, logs a warning and uses the most restrictive value (false).
        /// </summary>
        /// <param name="indexName">Index name</param>
        /// <param name="members">List of members and their attributes</param>
        /// <returns>The resolved AllowDuplicates value</returns>
        private static bool ResolveAllowDuplicates(
            string indexName,
            List<(MemberInfo member, SecondaryKeyAttribute attribute)> members)
        {
            if (members.Count == 0)
                return true;

            if (members.Count == 1)
                return members[0].attribute.AllowDuplicates;

            var firstValue = members[0].attribute.AllowDuplicates;
            var hasInconsistency = false;

            for (var i = 1; i < members.Count; i++)
            {
                if (members[i].attribute.AllowDuplicates != firstValue)
                {
                    hasInconsistency = true;
                    break;
                }
            }

            if (hasInconsistency)
            {
                var memberSettings = string.Join(", ",
                    members.Select(m => $"{m.member.Name}={m.attribute.AllowDuplicates}"));
                Debug.LogWarning(
                    $"Composite index '{indexName}' has inconsistent AllowDuplicates settings: {memberSettings}. " +
                    $"Using the most restrictive setting (false). " +
                    $"Set all members to the same value to avoid this warning.");
                return false;
            }

            return firstValue;
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

            // Use string keys as the primary key (avoids hash collisions)
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

            // Use string keys as the primary key (fully avoids hash collisions)
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
        /// Converts a value to a culture-invariant string.
        /// float/double use round-trip format to preserve precision.
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
