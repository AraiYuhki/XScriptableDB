using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// リフレクション関連のユーティリティ。
    /// </summary>
    public static class ReflectionUtility
    {
        private const BindingFlags AllInstanceFields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// シリアライズ可能なフィールドを取得する。
        /// publicフィールドは常に対象。
        /// privateフィールドはSerializeField属性がある場合のみ対象。
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
        /// フィールドがシリアライズ可能かどうかを判定する。
        /// </summary>
        public static bool IsSerializableField(FieldInfo field)
        {
            // publicフィールドは常に対象
            if (field.IsPublic)
                return true;

            // privateフィールドはSerializeField属性がある場合のみ対象
            return field.GetCustomAttribute<SerializeField>() != null;
        }

        /// <summary>
        /// 指定した名前のシリアライズ可能なフィールドを取得する。
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
