using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Xeon.XScriptableDB.IO
{
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

        public List<T> Parse<T>() where T : CsvData, new()
        {
            var type = typeof(T);
            var (attributes, members) = GetMembers<T>();
            var result = new List<T>();
            var headers = new List<string>();
            var isFirst = true;

            foreach (var line in csv.Split("\n").Select(line => line.Trim()))
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                var columns = line.Split(separator);
                if (isFirst)
                {
                    headers = columns.ToList();
                    isFirst = false;
                    continue;
                }

                var parsed = new Dictionary<string, string>();
                foreach (var (key, index) in headers.Select((key, index) => (key, index)))
                {
                    var rawValue = index >= columns.Length ? string.Empty : columns[index];
                    parsed[key] = RestoreEscapedStrings(rawValue);
                }

                var instance = CreateInstance<T>(attributes, members, type, parsed);
                result.Add(instance);
            }
            return result;
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
            Dictionary<string, CsvColumn> attributes,
            Dictionary<string, MemberInfo> members,
            Type type,
            Dictionary<string, string> row) where T : CsvData, new()
        {
            var instance = new T();
            foreach (var (key, value) in row)
            {
                if (!attributes.ContainsKey(key) || !members.ContainsKey(key))
                    continue;

                var member = members[key];
                try
                {
                    SetMemberValue(type, member, instance, value);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            instance.Initialize();
            return instance;
        }

        private string RestoreEscapedStrings(string text)
        {
            var result = text;
            var hasReplacement = true;
            while (hasReplacement)
            {
                hasReplacement = false;
                foreach (var (escaped, origin) in escapedData)
                {
                    if (!result.Contains(escaped))
                        continue;
                    result = result.Replace(escaped, origin);
                    hasReplacement = true;
                }
            }
            return result;
        }

        private void SetMemberValue<T>(Type type, MemberInfo member, T instance, string value)
        {
            Type memberType;
            Action<object> setValue;

            if (member.MemberType == MemberTypes.Property)
            {
                var propertyInfo = type.GetProperty(member.Name, MemberFlags);
                if (propertyInfo == null)
                    return;
                memberType = propertyInfo.PropertyType;
                setValue = v => propertyInfo.SetValue(instance, v);
            }
            else if (member.MemberType == MemberTypes.Field)
            {
                var fieldInfo = type.GetField(member.Name, MemberFlags);
                if (fieldInfo == null)
                    return;
                memberType = fieldInfo.FieldType;
                setValue = v => fieldInfo.SetValue(instance, v);
            }
            else
            {
                Debug.LogError($"{member.Name} is not property or field");
                return;
            }

            var convertedValue = ConvertValue(memberType, value, member.Name);
            if (convertedValue != null)
                setValue(convertedValue);
        }

        private object ConvertValue(Type targetType, string value, string memberName)
        {
            if (targetType == typeof(int))
            {
                if (int.TryParse(value, out var intValue))
                    return intValue;
                Debug.LogWarning($"Failed to parse '{value}' as int for member '{memberName}'");
                return 0;
            }
            if (targetType == typeof(float))
            {
                if (float.TryParse(value, out var floatValue))
                    return floatValue;
                Debug.LogWarning($"Failed to parse '{value}' as float for member '{memberName}'");
                return 0f;
            }
            if (targetType == typeof(double))
            {
                if (double.TryParse(value, out var doubleValue))
                    return doubleValue;
                Debug.LogWarning($"Failed to parse '{value}' as double for member '{memberName}'");
                return 0.0;
            }
            if (targetType == typeof(bool))
            {
                if (bool.TryParse(value, out var boolValue))
                    return boolValue;
                Debug.LogWarning($"Failed to parse '{value}' as bool for member '{memberName}'");
                return false;
            }
            if (targetType == typeof(string))
                return value.FromCsv();
            if (targetType.IsEnum)
            {
                if (Enum.TryParse(targetType, value, out var enumValue))
                    return enumValue;
                Debug.LogWarning($"Failed to parse '{value}' as {targetType.Name} for member '{memberName}'");
                return Activator.CreateInstance(targetType);
            }

            Debug.LogWarning($"Type '{targetType}' is not supported for member '{memberName}'");
            return null;
        }

        public static string ToCSV<T>(List<T> data)
            => ToCSV(data, defaultSeparator);

        public static string ToCSV<T>(List<T> data, string separator)
            => ToCSV((IEnumerable<object>)data.Cast<object>(), typeof(T), separator);

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
                    object value = member.MemberType switch
                    {
                        MemberTypes.Property => recordType.GetProperty(member.Name)?.GetValue(row),
                        MemberTypes.Field => recordType.GetField(member.Name, MemberFlags)?.GetValue(row),
                        _ => null
                    };
                    values.Add(ValueToString(value));
                }
                builder.AppendLine(string.Join(separator, values));
            }
            return builder.ToString();
        }

        private static string ValueToString(object value)
        {
            if (value == null)
                return "\"\"";
            if (value is string s)
                return s.ToCsv();
            return value.ToString();
        }
    }
}