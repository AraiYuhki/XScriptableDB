using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// レコードのPrimaryKeyにアクセスするためのヘルパークラス。
    /// リフレクションを使用してPrimaryKey属性が付いたメンバーを検出し、値を取得する。
    /// </summary>
    /// <typeparam name="T">レコードの型</typeparam>
    public class PrimaryKeyAccessor<T>
    {
        private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly MemberInfo primaryKeyMember;
        private readonly Type keyType;
        private readonly Func<T, object> getValue;

        /// <summary>
        /// PrimaryKeyの型を取得する。
        /// </summary>
        public Type KeyType => keyType;

        /// <summary>
        /// PrimaryKeyが見つかったかどうか。
        /// </summary>
        public bool HasPrimaryKey => primaryKeyMember != null;

        /// <summary>
        /// PrimaryKeyのメンバー名を取得する。
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
        /// レコードからPrimaryKey値を取得する。
        /// </summary>
        /// <param name="record">レコード</param>
        /// <returns>PrimaryKey値</returns>
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
        /// 型安全にPrimaryKey値を取得する。
        /// </summary>
        /// <typeparam name="TKey">PrimaryKeyの型</typeparam>
        /// <param name="record">レコード</param>
        /// <returns>PrimaryKey値</returns>
        public TKey GetKey<TKey>(T record)
        {
            var value = GetKey(record);
            if (value is TKey typedValue)
                return typedValue;
            return default;
        }

        /// <summary>
        /// レコードのComparerを作成する。
        /// </summary>
        /// <typeparam name="TKey">PrimaryKeyの型</typeparam>
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
