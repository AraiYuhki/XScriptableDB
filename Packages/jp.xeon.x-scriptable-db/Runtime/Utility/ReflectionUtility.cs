using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// リフレクション関連のユーティリティメソッド。
    /// </summary>
    public static class ReflectionUtility
    {
        private const BindingFlags AllInstanceFields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// シリアライズ可能なすべてのフィールドを返します。
        /// パブリックフィールドは常に含まれます。
        /// プライベートフィールドは、SerializeField属性がある場合のみ含まれます。
        /// </summary>
        public static IEnumerable<FieldInfo> GetSerializableFields(Type type)
        {
            foreach (var field in type.GetFields(AllInstanceFields))
            {
                if (IsSerializableField(field))
                    yield return field;
            }
        }

        /// <summary>
        /// フィールドがシリアライズ可能かどうかを判定します。
        /// </summary>
        public static bool IsSerializableField(FieldInfo field)
        {
            // パブリックフィールドは常に含まれる
            if (field.IsPublic)
                return true;

            // プライベートフィールドはSerializeField属性がある場合のみ含まれる
            return field.GetCustomAttribute<SerializeField>() != null;
        }

        /// <summary>
        /// 指定された名前のシリアライズ可能なフィールドを返します。
        /// </summary>
        public static FieldInfo GetSerializableField(Type type, string fieldName)
        {
            var field = type.GetField(fieldName, AllInstanceFields);
            if (field == null)
                return null;

            return IsSerializableField(field) ? field : null;
        }
    }
}
