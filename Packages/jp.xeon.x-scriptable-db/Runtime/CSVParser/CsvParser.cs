using System;
using System.Collections;
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
        private const BindingFlags ProperyFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly Type[] VectorTypes = new Type[]
        {
            typeof(Vector2), typeof(Vector3), typeof(Vector4),
            typeof(Vector2Int), typeof(Vector3Int), typeof(Quaternion)
        };

        private static string defaultSeparator = ",";
        private string csv = null;
        private string separator = null;
        private Dictionary<string, string> escapedData = new();

        private CsvParser(string csv)
            => Initialize(csv, defaultSeparator);

        private CsvParser(string csv, string separator)
            => Initialize(csv, separator);

        private void Initialize(string csv, string separator)
        {
            csv = csv.Replace("\r\n", "\n").Replace("\r", "\n");
            this.separator = separator;
            csv = CsvUtility.Escape(csv, escapedData);
            this.csv = csv;
        }

        public static void SetDefaultSeparator(string separator)
            => defaultSeparator = separator;

        public static List<T> Parse<T>(string csv) where T : CsvData, new()
            => new CsvParser(csv).Parse<T>();
        public static List<T> Parse<T>(string csv, string separator) where T : CsvData, new()
            => new CsvParser(csv, separator).Parse<T>();

        public static List<T> ParseFile<T>(string path) where T : CsvData, new()
        {
            return ParseFile<T>(path, defaultSeparator);
        }
        public static List<T> ParseFile<T>(string path, string separator) where T : CsvData, new()
        {
            var encoding = EncodeHelper.GetJpEncoding(path) ?? Encoding.UTF8;
            using (var reader = new StreamReader(path, encoding))
            {
                var parser = new CsvParser(reader.ReadToEnd(), separator);
                return parser.Parse<T>();
            }
        }

        public List<T> Parse<T>() where T : CsvData, new()
        {
            var type = typeof(T);
            var (attributes, members) = GetProperties<T>();
            var result = new List<T>();
            var headers = new List<string>();
            bool isFirst = true;
            foreach (var line in csv.Split("\n").Select(line => line.Trim()))
            {
                if (string.IsNullOrEmpty(line)) continue;
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
                    try
                    {
                        parsed[key] = index >= columns.Length ? string.Empty : Restore(columns[index], "string");
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        Debug.LogError($"{key} {index} {columns.Length}");
                    }
                }
                var instance = CreateInstance<T>(attributes, members, type, parsed);
                result.Add(instance);
            }
            return result;
        }

        private static (Dictionary<string, CsvColumn> attributes, Dictionary<string, MemberInfo> members) GetProperties<T>()
        {
            var type = typeof(T);
            var attributes = new Dictionary<string, CsvColumn>();
            var members = new Dictionary<string, MemberInfo>();
            var fields = type.GetFields(ProperyFlags).Select(field => field as MemberInfo);
            foreach (var member in fields.Concat(type.GetProperties()))
            {
                var csvColumn = member.GetCustomAttribute<CsvColumn>();
                if (csvColumn == null) continue;
                attributes.Add(csvColumn.Name, csvColumn);
                members.Add(csvColumn.Name, member);
            }
            return (attributes, members);
        }

        private T CreateInstance<T>(Dictionary<string, CsvColumn> attributes, Dictionary<string, MemberInfo> members,
            Type type, Dictionary<string, string> row)
            where T : CsvData, new()
        {
            var instance = new T();
            foreach (var (key, value) in row)
            {
                var text = value;
                if (!attributes.ContainsKey(key) || !members.ContainsKey(key)) continue;
                var member = members[key];
                try
                {
                    if (member.MemberType == MemberTypes.Property)
                        SetPropertyValue(type, member.Name, instance, text);
                    else if (member.MemberType == MemberTypes.Field)
                        SetValue(type, member.Name, instance, text);
                    else
                        Debug.LogError($"{key} is not property or field");
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            instance.Initialize();
            return instance;
        }

        private string RestoreEscape(string text)
        {
            while(true)
            {
                var isReplaced = false;
                foreach (var (escaped, origin) in escapedData)
                {
                    if (text.Contains(escaped))
                    {
                        text = text.Replace(escaped, origin);
                        isReplaced = true;
                    }
                }
                if (!isReplaced)
                    return text;
            }
        }

        private string Restore(string text, params string[] escapeTargets)
        {
            foreach (var escapeTarget in escapeTargets)
                text = Restore(text, escapeTarget);
            return text;
        }

        private string Restore(string text, string escapeTarget)
        {
            while(true)
            {
                var isReplaced = false;
                foreach (var (escaped, origin) in escapedData)
                {
                    if (!escaped.Contains(escapeTarget)) continue;
                    if (text.Contains(escaped))
                    {
                        text = text.Replace(escaped, origin);
                        isReplaced = true;
                    }
                }
                if (!isReplaced)
                    return text;
            }
        }

        private void SetPropertyValue<T>(Type type, string memberName, T instance, string value)
        {
            var propertyInfo = type.GetProperty(memberName, ProperyFlags);
            if (propertyInfo == null)
            {
                Debug.LogWarning($"Property '{memberName}' not found on type '{type.Name}'");
                return;
            }
            var convertedValue = ConvertValue(propertyInfo.PropertyType, value, memberName);
            if (convertedValue != null)
                propertyInfo.SetValue(instance, convertedValue);
        }

        private void SetValue<T>(Type type, string memberName, T instance, string value)
        {
            var fieldInfo = type.GetField(memberName, ProperyFlags);
            if (fieldInfo == null)
            {
                Debug.LogWarning($"Field '{memberName}' not found on type '{type.Name}'");
                return;
            }
            if (fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                value = Restore(value, "list");
                SetArrayValue(fieldInfo, instance, value);
                return;
            }
            var convertedValue = ConvertValue(fieldInfo.FieldType, value, memberName);
            if (convertedValue != null)
                fieldInfo.SetValue(instance, convertedValue);
        }

        private object ConvertValue(Type targetType, string value, string memberName)
        {
            if (VectorTypes.Contains(targetType))
                value = Restore(value, "vector");

            if (targetType.GetInterface(nameof(ICsvSupport)) != null)
            {
                var data = (ICsvSupport)Activator.CreateInstance(targetType);
                value = Restore(value, "list", "object", "vector", "string");
                data.FromCsv(value);
                return data;
            }
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
            if (targetType == typeof(bool))
            {
                if (bool.TryParse(value, out var boolValue))
                    return boolValue;
                Debug.LogWarning($"Failed to parse '{value}' as bool for member '{memberName}'");
                return false;
            }
            if (targetType == typeof(string))
            {
                value = Restore(value, "string");
                return value.FromCsv();
            }
            if (targetType.IsEnum)
            {
                if (Enum.TryParse(targetType, value, out var enumValue))
                    return enumValue;
                Debug.LogWarning($"Failed to parse '{value}' as {targetType.Name} for member '{memberName}'");
                return Activator.CreateInstance(targetType);
            }
            if (targetType == typeof(Vector2))
                return value.ToVector2();
            if (targetType == typeof(Vector2Int))
                return value.ToVector2Int();
            if (targetType == typeof(Vector3))
                return value.ToVector3();
            if (targetType == typeof(Vector3Int))
                return value.ToVector3Int();
            if (targetType == typeof(Vector4))
                return value.ToVector4();
            if (targetType == typeof(Quaternion))
                return value.ToQuaternion();

            Debug.LogWarning($"Type '{targetType}' is not supported for member '{memberName}'");
            return null;
        }

        private void SetArrayValue<T>(FieldInfo fieldInfo, T instance, string value)
        {
            value = value.Trim('[', ']');
            if (fieldInfo.FieldType.GenericTypeArguments.Length > 1)
                throw new InvalidCastException("Multi generic arguments type is not support");
            var type = fieldInfo.FieldType.GenericTypeArguments[0];
            var splited = value.Split(",").Where(splited => !string.IsNullOrEmpty(splited)).ToList();
            for (var index = 0; index < splited.Count; index++)
                splited[index] = Restore(splited[index], "object", "vector", "string");
            if (type == typeof(int))
                fieldInfo.SetValue(instance, splited.Select(data => int.Parse(data)).ToList());
            else if (type == typeof(float))
                fieldInfo.SetValue(instance, splited.Select(data => float.Parse(data)).ToList());
            else if (type == typeof(bool))
                fieldInfo.SetValue(instance, splited.Select(data => bool.Parse(data)).ToList());
            else if (type == typeof(string))
                fieldInfo.SetValue(instance, splited.Select(data => data.FromCsv()).ToList());
            else if (type.IsEnum)
            {
                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type));
                foreach (var text in splited)
                    list.Add(Enum.Parse(type, text));
                fieldInfo.SetValue(instance, list);
            }
            else
                throw new InvalidCastException("This type's List is not support");
        }

        public static string ToCSV<T>(List<T> data)
            => ToCSV<T>(data, defaultSeparator);

        public static string ToCSV<T>(List<T> data, string separator)
        {
            var builder = new StringBuilder();
            (var attributes, var members) = GetProperties<T>();
            builder.AppendLine(string.Join(separator, attributes.Keys));
            var type = typeof(T);
            foreach (var row in data)
            {
                var values = new List<string>();
                foreach ((_, var member) in members)
                {
                    object value = null;
                    if (member.MemberType == MemberTypes.Property)
                        value = type.GetProperty(member.Name).GetValue(row);
                    else if (member.MemberType == MemberTypes.Field)
                        value = type.GetField(member.Name, ProperyFlags).GetValue(row);
                    else
                        continue;
                    values.Add(CsvSupport.ToString(value));
                }
                builder.AppendLine(string.Join(separator, values));
            }
            return builder.ToString();
        }
    }
}