using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// Reflection-related utility methods.
    /// </summary>
    public static class ReflectionUtility
    {
        private const BindingFlags AllInstanceFields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// Returns all serializable fields.
        /// Public fields are always included.
        /// Private fields are included only if they have the SerializeField attribute.
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
        /// Determines whether a field is serializable.
        /// </summary>
        public static bool IsSerializableField(FieldInfo field)
        {
            // Public fields are always included
            if (field.IsPublic)
                return true;

            // Private fields are included only if they have the SerializeField attribute
            return field.GetCustomAttribute<SerializeField>() != null;
        }

        /// <summary>
        /// Returns the serializable field with the specified name.
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
