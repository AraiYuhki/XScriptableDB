using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Class that handles SQL field access and type conversion.
    /// </summary>
    public class SqlFieldAccessor
    {
        private const BindingFlags MemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// Gets the value of a field.
        /// </summary>
        public object GetFieldValue(object record, Type recordType, string fieldName)
        {
            var field = recordType.GetField(fieldName, MemberFlags);
            if (field != null)
                return field.GetValue(record);

            field = recordType.GetFields(MemberFlags)
                .FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            if (field != null)
                return field.GetValue(record);

            var property = recordType.GetProperty(fieldName, MemberFlags);
            if (property?.CanRead == true)
                return property.GetValue(record);

            property = recordType.GetProperties(MemberFlags)
                .FirstOrDefault(p => p.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            if (property?.CanRead == true)
                return property.GetValue(record);

            return null;
        }

        /// <summary>
        /// Sets the value of a field.
        /// </summary>
        public void SetFieldValue(object record, Type recordType, string fieldName, object value)
        {
            var field = recordType.GetField(fieldName, MemberFlags);
            if (field == null)
            {
                field = recordType.GetFields(MemberFlags)
                    .FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            }

            if (field != null)
            {
                var convertedValue = ConvertValue(value, field.FieldType);
                field.SetValue(record, convertedValue);
                return;
            }

            var property = recordType.GetProperty(fieldName, MemberFlags);
            if (property == null)
            {
                property = recordType.GetProperties(MemberFlags)
                    .FirstOrDefault(p => p.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            }

            if (property?.CanWrite == true)
            {
                var convertedValue = ConvertValue(value, property.PropertyType);
                property.SetValue(record, convertedValue);
            }
        }

        /// <summary>
        /// Checks whether a field or property exists in the specified type.
        /// </summary>
        public bool HasField(Type recordType, string fieldName)
        {
            var field = recordType.GetField(fieldName, MemberFlags);
            if (field != null)
                return true;

            field = recordType.GetFields(MemberFlags)
                .FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            if (field != null)
                return true;

            var property = recordType.GetProperty(fieldName, MemberFlags);
            if (property?.CanRead == true)
                return true;

            property = recordType.GetProperties(MemberFlags)
                .FirstOrDefault(p => p.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            if (property?.CanRead == true)
                return true;

            return false;
        }

        /// <summary>
        /// Converts a value to the specified type.
        /// </summary>
        public object ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            var valueType = value.GetType();
            if (targetType.IsAssignableFrom(valueType))
                return value;

            if (IsNumeric(value) && IsNumericType(targetType))
                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);

            if (value is string str)
            {
                if (targetType == typeof(int) && int.TryParse(str, out var intVal))
                    return intVal;
                if (targetType == typeof(long) && long.TryParse(str, out var longVal))
                    return longVal;
                if (targetType == typeof(float) && float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatVal))
                    return floatVal;
                if (targetType == typeof(double) && double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleVal))
                    return doubleVal;
                if (targetType == typeof(bool) && bool.TryParse(str, out var boolVal))
                    return boolVal;
            }

            try
            {
                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }

        /// <summary>
        /// Checks whether a value is a numeric type.
        /// </summary>
        public bool IsNumeric(object value)
        {
            return value is byte or sbyte or short or ushort or int or uint
                or long or ulong or float or double or decimal;
        }

        /// <summary>
        /// Checks whether a type is a numeric type.
        /// </summary>
        public bool IsNumericType(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }

        /// <summary>
        /// Checks whether two values are equal.
        /// </summary>
        public bool AreEqual(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            if (a is bool boolA && IsNumeric(b))
                return boolA == (Convert.ToDouble(b) != 0);
            if (b is bool boolB && IsNumeric(a))
                return boolB == (Convert.ToDouble(a) != 0);

            if (IsNumeric(a) && IsNumeric(b))
                return Math.Abs(Convert.ToDouble(a) - Convert.ToDouble(b)) < double.Epsilon;

            if (a is bool && b is bool)
                return a.Equals(b);

            return a.Equals(b) || a.ToString().Equals(b.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Compares two values.
        /// </summary>
        public int Compare(object a, object b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            if (IsNumeric(a) && IsNumeric(b))
                return Convert.ToDouble(a).CompareTo(Convert.ToDouble(b));

            if (a is IComparable ca)
            {
                try
                {
                    return ca.CompareTo(b);
                }
                catch
                {
                    // If types differ, compare as strings
                }
            }

            return string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the name of an expression.
        /// </summary>
        public string GetExpressionName(SqlExpression expr)
        {
            return expr switch
            {
                ColumnExpression col => col.ToString(),
                AggregateExpression agg => agg.ToString(),
                FunctionCallExpression func => func.ToString(),
                _ => expr.ToString()
            };
        }
    }

    /// <summary>
    /// Comparer for object comparison.
    /// </summary>
    public class ObjectComparer : IComparer<object>
    {
        public int Compare(object x, object y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            if (x is IComparable cx)
            {
                try
                {
                    return cx.CompareTo(y);
                }
                catch
                {
                    // If types differ
                }
            }

            return string.Compare(x.ToString(), y.ToString(), StringComparison.Ordinal);
        }
    }
}
