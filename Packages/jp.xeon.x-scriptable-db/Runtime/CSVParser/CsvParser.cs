using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Xeon.XScriptableDB.IO
{
    /// <summary>
    /// CSVパーサー。
    /// 文字列またはファイルからオブジェクトのリストを生成します。
    /// </summary>
    public class CsvParser
    {
        private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static string defaultSeparator = ",";
        private readonly string csv;
        private readonly string separator;
        private readonly Dictionary<string, string> escapedData = new();

        private CsvParser(string csv)
            : this(csv, defaultSeparator) { }

        private CsvParser(string csv, string separator)
        {
            csv = csv.Replace("\r\n", "\n").Replace("\r", "\n");
            this.separator = separator;
            this.csv = CsvUtility.Escape(csv, escapedData);
        }

        public static void SetDefaultSeparator(string separator)
            => defaultSeparator = separator;

        public static List<T> Parse<T>(string csv) where T : CsvData, new()
            => new CsvParser(csv).Parse<T>();

        public static List<T> Parse<T>(string csv, string separator) where T : CsvData, new()
            => new CsvParser(csv, separator).Parse<T>();

        public static List<T> ParseFile<T>(string path) where T : CsvData, new()
            => ParseFile<T>(path, defaultSeparator);

        public static List<T> ParseFile<T>(string path, string separator) where T : CsvData, new()
        {
            var encoding = EncodeHelper.GetJpEncoding(path) ?? Encoding.UTF8;
            using var reader = new StreamReader(path, encoding);
            return new CsvParser(reader.ReadToEnd(), separator).Parse<T>();
        }

        public static List<T> ParseRecord<T>(string csv) where T : class, new()
            => new CsvParser(csv).ParseRecord<T>();

        public static List<T> ParseRecord<T>(string csv, string separator) where T : class, new()
            => new CsvParser(csv, separator).ParseRecord<T>();

        public static List<T> ParseRecordFile<T>(string path) where T : class, new()
            => ParseRecordFile<T>(path, defaultSeparator);

        public static List<T> ParseRecordFile<T>(string path, string separator) where T : class, new()
        {
            var encoding = EncodeHelper.GetJpEncoding(path) ?? Encoding.UTF8;
            using var reader = new StreamReader(path, encoding);
            return new CsvParser(reader.ReadToEnd(), separator).ParseRecord<T>();
        }

        public List<T> Parse<T>() where T : CsvData, new()
        {
            var (attributes, members) = GetMembers<T>();
            var result = new List<T>();
            Dictionary<int, MemberInfo> memberIndexMap = null;

            foreach (var rawLine in csv.Split("\n"))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                var columns = line.Split(separator);
                if (memberIndexMap == null)
                {
                    memberIndexMap = BuildMemberIndexMap(columns, attributes, members);
                    continue;
                }

                var instance = CreateInstance<T>(memberIndexMap, columns);
                result.Add(instance);
            }
            return result;
        }

        public List<T> ParseRecord<T>() where T : class, new()
        {
            var (attributes, members) = GetMembers<T>();
            var result = new List<T>();
            Dictionary<int, MemberInfo> memberIndexMap = null;

            foreach (var rawLine in csv.Split("\n"))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                var columns = line.Split(separator);
                if (memberIndexMap == null)
                {
                    memberIndexMap = BuildMemberIndexMap(columns, attributes, members);
                    continue;
                }

                var instance = CreateRecordInstance<T>(memberIndexMap, columns);
                result.Add(instance);
            }
            return result;
        }

        private static Dictionary<int, MemberInfo> BuildMemberIndexMap(
            string[] headers,
            Dictionary<string, CsvColumn> attributes,
            Dictionary<string, MemberInfo> members)
        {
            var map = new Dictionary<int, MemberInfo>();
            for (var i = 0; i < headers.Length; i++)
            {
                var header = headers[i];
                if (!attributes.ContainsKey(header) || !members.TryGetValue(header, out var member))
                    continue;
                map[i] = member;
            }
            return map;
        }

        private static (Dictionary<string, CsvColumn> attributes, Dictionary<string, MemberInfo> members) GetMembers<T>()
            => GetMembers(typeof(T));

        private static (Dictionary<string, CsvColumn> attributes, Dictionary<string, MemberInfo> members) GetMembers(Type type)
        {
            var attributes = new Dictionary<string, CsvColumn>();
            var members = new Dictionary<string, MemberInfo>();
            var fields = type.GetFields(MemberFlags).Select(field => field as MemberInfo);

            foreach (var member in fields.Concat(type.GetProperties()))
            {
                var csvColumn = member.GetCustomAttribute<CsvColumn>();
                if (csvColumn == null)
                    continue;
                attributes.Add(csvColumn.Name, csvColumn);
                members.Add(csvColumn.Name, member);
            }
            return (attributes, members);
        }

        private T CreateInstance<T>(
            Dictionary<int, MemberInfo> memberIndexMap,
            string[] columns) where T : CsvData, new()
        {
            var instance = new T();
            foreach (var (index, member) in memberIndexMap)
            {
                var rawValue = index >= columns.Length ? string.Empty : columns[index];
                try
                {
                    SetMemberValue(member, instance, RestoreEscapedStrings(rawValue));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            instance.Initialize();
            return instance;
        }

        private T CreateRecordInstance<T>(
            Dictionary<int, MemberInfo> memberIndexMap,
            string[] columns) where T : class, new()
        {
            var instance = new T();
            foreach (var (index, member) in memberIndexMap)
            {
                var rawValue = index >= columns.Length ? string.Empty : columns[index];
                try
                {
                    SetMemberValue(member, instance, RestoreEscapedStrings(rawValue));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            return instance;
        }

        private static readonly Regex EscapedPattern = new(@"<escaped string>\d+</escaped string>", RegexOptions.Compiled);

        private string RestoreEscapedStrings(string text)
        {
            if (escapedData.Count == 0)
                return text;

            return EscapedPattern.Replace(text, match =>
            {
                var key = match.Value;
                return escapedData.TryGetValue(key, out var origin) ? origin : key;
            });
        }

        private void SetMemberValue<T>(MemberInfo member, T instance, string value)
        {
            Type memberType;
            Action<object> setValue;

            if (member is PropertyInfo propertyInfo)
            {
                memberType = propertyInfo.PropertyType;
                setValue = v => propertyInfo.SetValue(instance, v);
            }
            else if (member is FieldInfo fieldInfo)
            {
                memberType = fieldInfo.FieldType;
                setValue = v => fieldInfo.SetValue(instance, v);
            }
            else
            {
                Debug.LogError($"{member.Name} はプロパティまたはフィールドではありません");
                return;
            }

            var convertedValue = ConvertValue(memberType, value, member.Name);
            if (convertedValue != null)
                setValue(convertedValue);
        }

        private object ConvertValue(Type targetType, string value, string memberName)
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
                return IsNullValue(value) ? null : ConvertValue(underlyingType, value, memberName);

            if (TryGetSerializableNullableInnerType(targetType, out var innerType))
                return ConvertSerializableNullable(targetType, innerType, value, memberName);

            if (targetType == typeof(int))
            {
                if (int.TryParse(value, out var intValue))
                    return intValue;
                Debug.LogWarning($"メンバ '{memberName}' の値を int としてパースできませんでした: '{value}'");
                return 0;
            }
            if (targetType == typeof(float))
            {
                if (float.TryParse(value, out var floatValue))
                    return floatValue;
                Debug.LogWarning($"メンバ '{memberName}' の値を float としてパースできませんでした: '{value}'");
                return 0f;
            }
            if (targetType == typeof(double))
            {
                if (double.TryParse(value, out var doubleValue))
                    return doubleValue;
                Debug.LogWarning($"メンバ '{memberName}' の値を double としてパースできませんでした: '{value}'");
                return 0.0;
            }
            if (targetType == typeof(bool))
            {
                if (bool.TryParse(value, out var boolValue))
                    return boolValue;
                Debug.LogWarning($"メンバ '{memberName}' の値を bool としてパースできませんでした: '{value}'");
                return false;
            }
            if (targetType == typeof(string))
                return value.FromCsv();
            if (targetType.IsEnum)
            {
                if (Enum.TryParse(targetType, value, out var enumValue))
                    return enumValue;
                Debug.LogWarning($"メンバ '{memberName}' の値を {targetType.Name} としてパースできませんでした: '{value}'");
                return Activator.CreateInstance(targetType);
            }
            if (targetType == typeof(DateTime))
            {
                if (TryParseDateTime(value, out var dateTimeValue))
                    return dateTimeValue;
                Debug.LogWarning($"メンバ '{memberName}' の値を DateTime としてパースできませんでした: '{value}'");
                return DateTime.MinValue;
            }
            if (targetType == typeof(SerializableDateTime))
            {
                if (TryParseDateTime(value, out var dateTimeValue))
                    return new SerializableDateTime(dateTimeValue);
                Debug.LogWarning($"メンバ '{memberName}' の値を SerializableDateTime としてパースできませんでした: '{value}'");
                return SerializableDateTime.MinValue;
            }

            Debug.LogWarning($"型 '{targetType}' はメンバ '{memberName}' でサポートされていません");
            return null;
        }

        private static bool IsNullValue(string value)
            => string.IsNullOrEmpty(value) || string.Equals(value, "null", StringComparison.OrdinalIgnoreCase);

        private static bool TryGetSerializableNullableInnerType(Type targetType, out Type innerType)
        {
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(SerializableNullable<>))
            {
                innerType = targetType.GetGenericArguments()[0];
                return true;
            }
            innerType = null;
            return false;
        }

        private object ConvertSerializableNullable(Type targetType, Type innerType, string value, string memberName)
        {
            if (IsNullValue(value))
                return Activator.CreateInstance(targetType);

            var innerValue = ConvertValue(innerType, value, memberName);
            if (innerValue == null)
                return Activator.CreateInstance(targetType);
            return Activator.CreateInstance(targetType, innerValue);
        }

        public static string ToCSV<T>(List<T> data)
            => ToCSV(data, defaultSeparator);

        public static string ToCSV<T>(List<T> data, string separator)
            => ToCSV(data.Cast<object>(), typeof(T), separator);

        public static string ToCSV(IEnumerable<object> data, Type recordType)
            => ToCSV(data, recordType, defaultSeparator);

        public static string ToCSV(IEnumerable<object> data, Type recordType, string separator)
        {
            var builder = new StringBuilder();
            var (attributes, members) = GetMembers(recordType);
            builder.AppendLine(string.Join(separator, attributes.Keys));

            foreach (var row in data)
            {
                var values = new List<string>();
                foreach (var (_, member) in members)
                {
                    var (value, memberType) = member switch
                    {
                        PropertyInfo prop => (prop.GetValue(row), prop.PropertyType),
                        FieldInfo field => (field.GetValue(row), field.FieldType),
                        _ => (null, typeof(object))
                    };
                    values.Add(ValueToString(value, memberType));
                }
                builder.AppendLine(string.Join(separator, values));
            }
            return builder.ToString();
        }

        private static string ValueToString(object value, Type memberType)
        {
            if (value == null)
                return Nullable.GetUnderlyingType(memberType) != null ? "null" : "\"\"";
            if (value is ISerializableNullable nullable)
                return nullable.HasValue ? ValueToString(nullable.BoxedValue, nullable.BoxedValue.GetType()) : "null";
            if (value is string s)
                return s.ToCsv();
            if (value is DateTime dt)
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            if (value is SerializableDateTime sdt)
                return sdt.DateTime.ToString("yyyy-MM-dd HH:mm:ss");
            return value.ToString();
        }

        private static bool TryParseDateTime(string value, out DateTime result)
        {
            if (string.IsNullOrEmpty(value))
            {
                result = DateTime.MinValue;
                return true;
            }

            var formats = new[]
            {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy/MM/dd HH:mm:ss",
                "yyyy-MM-dd",
                "yyyy/MM/dd",
                "MM/dd/yyyy HH:mm:ss",
                "MM/dd/yyyy",
                "dd/MM/yyyy HH:mm:ss",
                "dd/MM/yyyy",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ssZ",
                "o"
            };

            return DateTime.TryParseExact(
                value,
                formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out result) ||
                DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out result);
        }
    }
}
