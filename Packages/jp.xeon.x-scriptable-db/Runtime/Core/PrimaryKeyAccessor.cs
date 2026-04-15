using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// Helper class for accessing the PrimaryKey of a record.
    /// Uses reflection to detect members marked with the PrimaryKey attribute and retrieve their values.
    /// </summary>
    /// <typeparam name="T">Type of the record</typeparam>
    public class PrimaryKeyAccessor<T>
    {
        private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly MemberInfo primaryKeyMember;
        private readonly Type keyType;
        private readonly Func<T, object> getValue;

        /// <summary>
        /// Type of the PrimaryKey.
        /// </summary>
        public Type KeyType => keyType;

        /// <summary>
        /// Whether a PrimaryKey was found.
        /// </summary>
        public bool HasPrimaryKey => primaryKeyMember != null;

        /// <summary>
        /// Member name of the PrimaryKey.
        /// </summary>
        public string MemberName => primaryKeyMember?.Name;

        public PrimaryKeyAccessor()
        {
            var type = typeof(T);
            primaryKeyMember = FindPrimaryKeyMember(type);

            if (primaryKeyMember == null)
            {
                Debug.LogWarning($"Type {type.Name} does not have a [PrimaryKey] attribute");
                return;
            }

            keyType = GetMemberType(primaryKeyMember);
            getValue = CreateGetter(primaryKeyMember);
        }

        /// <summary>
        /// Retrieves the PrimaryKey value from a record.
        /// </summary>
        /// <param name="record">The record</param>
        /// <returns>PrimaryKey value</returns>
        public object GetKey(T record)
        {
            if (record == null)
                return null;

            if (record is IRecord xRecord)
            {
                var value = xRecord.GetPrimaryKeyValue();
                if (value != null)
                    return value;
            }

            return getValue?.Invoke(record);
        }

        /// <summary>
        /// Retrieves the PrimaryKey value from a record in a type-safe manner.
        /// </summary>
        /// <typeparam name="TKey">Type of the PrimaryKey</typeparam>
        /// <param name="record">The record</param>
        /// <returns>PrimaryKey value</returns>
        public TKey GetKey<TKey>(T record)
        {
            var value = GetKey(record);
            if (value is TKey typedValue)
                return typedValue;
            return default;
        }

        /// <summary>
        /// Creates a Comparer for records.
        /// </summary>
        /// <typeparam name="TKey">Type of the PrimaryKey</typeparam>
        /// <returns>Comparer</returns>
        public IComparer<T> CreateComparer<TKey>() where TKey : IComparable<TKey>
        {
            return Comparer<T>.Create((a, b) =>
            {
                var keyA = GetKey<TKey>(a);
                var keyB = GetKey<TKey>(b);
                return keyA.CompareTo(keyB);
            });
        }

        private static MemberInfo FindPrimaryKeyMember(Type type)
        {
            var members = new List<(MemberInfo member, int order)>();

            foreach (var field in type.GetFields(MemberFlags))
            {
                var attr = field.GetCustomAttribute<PrimaryKeyAttribute>();
                if (attr != null)
                    members.Add((field, attr.Order));
            }

            foreach (var property in type.GetProperties(MemberFlags))
            {
                var attr = property.GetCustomAttribute<PrimaryKeyAttribute>();
                if (attr != null)
                    members.Add((property, attr.Order));
            }

            if (members.Count == 0)
                return null;

            members.Sort((a, b) => a.order.CompareTo(b.order));
            return members[0].member;
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

        private static Func<T, object> CreateGetter(MemberInfo member)
        {
            return member switch
            {
                FieldInfo field => record => field.GetValue(record),
                PropertyInfo property => record => property.GetValue(record),
                _ => null
            };
        }
    }
}
