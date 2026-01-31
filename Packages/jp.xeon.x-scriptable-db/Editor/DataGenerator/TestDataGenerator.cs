using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// データ生成ルール。
    /// </summary>
    public enum GeneratorRule
    {
        Sequential,     // 連番
        Random,         // ランダム
        RandomRange,    // 範囲指定ランダム
        RandomChoice,   // 選択肢からランダム
        Pattern,        // パターン文字列
        Fixed           // 固定値
    }

    /// <summary>
    /// フィールド生成設定。
    /// </summary>
    [Serializable]
    public class FieldGeneratorConfig
    {
        public string FieldName;
        public GeneratorRule Rule = GeneratorRule.Random;
        public string MinValue = "0";
        public string MaxValue = "100";
        public List<string> Choices = new();
        public string Pattern = "{0}";
        public string FixedValue = "";
        public int StartValue = 1;
    }

    /// <summary>
    /// テーブル生成設定。
    /// </summary>
    [Serializable]
    public class TableGeneratorConfig
    {
        public string TableName;
        public int RecordCount = 100;
        public List<FieldGeneratorConfig> FieldConfigs = new();
    }

    /// <summary>
    /// テストデータ生成器。
    /// </summary>
    public static class TestDataGenerator
    {
        private static readonly string[] SampleNames = { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry", "Ivy", "Jack" };
        private static readonly string[] SampleAdjectives = { "Fast", "Slow", "Big", "Small", "Hot", "Cold", "New", "Old", "Good", "Bad" };
        private static readonly string[] SampleNouns = { "Sword", "Shield", "Potion", "Armor", "Ring", "Book", "Gem", "Key", "Map", "Scroll" };

        /// <summary>
        /// テーブルにテストデータを生成する。
        /// </summary>
        public static int Generate(ITableAsset table, int count, TableGeneratorConfig config = null)
        {
            var recordType = table.RecordType;
            var fields = recordType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var generatedCount = 0;

            // 既存のPrimaryKey値を取得
            var existingKeys = new HashSet<object>();
            var primaryKeyField = fields.FirstOrDefault(f => f.GetCustomAttribute<PrimaryKeyAttribute>() != null);

            if (primaryKeyField != null)
            {
                foreach (var record in table.Records)
                {
                    if (record != null)
                    {
                        var keyValue = primaryKeyField.GetValue(record);
                        existingKeys.Add(keyValue);
                    }
                }
            }

            var nextId = existingKeys.Count > 0 && primaryKeyField?.FieldType == typeof(int)
                ? existingKeys.Cast<int>().Max() + 1
                : table.Count + 1;

            for (var i = 0; i < count; i++)
            {
                var record = Activator.CreateInstance(recordType);

                foreach (var field in fields)
                {
                    var fieldConfig = config?.FieldConfigs?.FirstOrDefault(fc => fc.FieldName == field.Name);
                    var value = GenerateFieldValue(field, nextId + i, fieldConfig);
                    field.SetValue(record, value);
                }

                table.AddRecordObject(record);
                generatedCount++;
            }

            var unityObject = table as UnityEngine.Object;
            if (unityObject != null)
                EditorUtility.SetDirty(unityObject);
            return generatedCount;
        }

        /// <summary>
        /// フィールド値を生成する。
        /// </summary>
        private static object GenerateFieldValue(FieldInfo field, int index, FieldGeneratorConfig config)
        {
            var fieldType = field.FieldType;

            // PrimaryKeyは連番
            if (field.GetCustomAttribute<PrimaryKeyAttribute>() != null)
            {
                if (fieldType == typeof(int))
                    return index;
                if (fieldType == typeof(long))
                    return (long)index;
                if (fieldType == typeof(string))
                    return $"ID_{index:D6}";
            }

            // 設定がある場合はそれに従う
            if (config != null)
                return GenerateFromConfig(config, fieldType, index);

            // デフォルト生成
            return GenerateDefaultValue(fieldType, field.Name, index);
        }

        private static object GenerateFromConfig(FieldGeneratorConfig config, Type fieldType, int index)
        {
            switch (config.Rule)
            {
                case GeneratorRule.Sequential:
                    var seqValue = config.StartValue + index - 1;
                    return ConvertToType(seqValue, fieldType);

                case GeneratorRule.Random:
                    return GenerateRandomValue(fieldType);

                case GeneratorRule.RandomRange:
                    if (fieldType == typeof(int))
                    {
                        var min = int.Parse(config.MinValue);
                        var max = int.Parse(config.MaxValue);
                        return Random.Range(min, max + 1);
                    }
                    if (fieldType == typeof(float))
                    {
                        var min = float.Parse(config.MinValue);
                        var max = float.Parse(config.MaxValue);
                        return Random.Range(min, max);
                    }
                    if (fieldType == typeof(double))
                    {
                        var min = double.Parse(config.MinValue);
                        var max = double.Parse(config.MaxValue);
                        return min + Random.value * (max - min);
                    }
                    return GenerateRandomValue(fieldType);

                case GeneratorRule.RandomChoice:
                    if (config.Choices.Count > 0)
                    {
                        var choice = config.Choices[Random.Range(0, config.Choices.Count)];
                        return ConvertToType(choice, fieldType);
                    }
                    return GetDefaultValue(fieldType);

                case GeneratorRule.Pattern:
                    return string.Format(config.Pattern, index);

                case GeneratorRule.Fixed:
                    return ConvertToType(config.FixedValue, fieldType);

                default:
                    return GenerateRandomValue(fieldType);
            }
        }

        private static object GenerateDefaultValue(Type fieldType, string fieldName, int index)
        {
            var nameLower = fieldName.ToLower();

            // フィールド名から推測
            if (nameLower.Contains("name"))
            {
                if (nameLower.Contains("item") || nameLower.Contains("product"))
                    return $"{SampleAdjectives[Random.Range(0, SampleAdjectives.Length)]} {SampleNouns[Random.Range(0, SampleNouns.Length)]}";
                return SampleNames[index % SampleNames.Length] + (index >= SampleNames.Length ? $"_{index / SampleNames.Length}" : "");
            }

            if (nameLower.Contains("price") || nameLower.Contains("cost"))
            {
                if (fieldType == typeof(int))
                    return Random.Range(10, 10000);
                if (fieldType == typeof(float))
                    return (float)Math.Round(Random.Range(10f, 10000f), 2);
                if (fieldType == typeof(double))
                    return Math.Round(Random.Range(10f, 10000f), 2);
            }

            if (nameLower.Contains("count") || nameLower.Contains("quantity") || nameLower.Contains("amount"))
            {
                return Random.Range(1, 100);
            }

            if (nameLower.Contains("level") || nameLower.Contains("rank"))
            {
                return Random.Range(1, 10);
            }

            if (nameLower.Contains("rate") || nameLower.Contains("percent"))
            {
                if (fieldType == typeof(float))
                    return (float)Math.Round(Random.Range(0f, 1f), 2);
                if (fieldType == typeof(double))
                    return Math.Round(Random.Range(0f, 1f), 2);
                return Random.Range(0, 101);
            }

            if (nameLower.Contains("description") || nameLower.Contains("desc"))
            {
                return $"Description for item {index}";
            }

            if (nameLower.Contains("active") || nameLower.Contains("enabled") || nameLower.Contains("flag"))
            {
                return Random.value > 0.3f;
            }

            if (nameLower.Contains("category") || nameLower.Contains("type") || nameLower.Contains("group"))
            {
                if (fieldType == typeof(int))
                    return Random.Range(1, 6);
                return $"Category_{Random.Range(1, 6)}";
            }

            return GenerateRandomValue(fieldType);
        }

        private static object GenerateRandomValue(Type fieldType)
        {
            if (fieldType == typeof(int))
                return Random.Range(1, 1000);

            if (fieldType == typeof(long))
                return (long)Random.Range(1, 1000000);

            if (fieldType == typeof(float))
                return (float)Math.Round(Random.Range(0f, 1000f), 2);

            if (fieldType == typeof(double))
                return Math.Round(Random.Range(0f, 1000f), 2);

            if (fieldType == typeof(bool))
                return Random.value > 0.5f;

            if (fieldType == typeof(string))
                return $"Value_{Random.Range(1000, 9999)}";

            if (fieldType.IsEnum)
            {
                var values = Enum.GetValues(fieldType);
                return values.GetValue(Random.Range(0, values.Length));
            }

            return GetDefaultValue(fieldType);
        }

        private static object ConvertToType(object value, Type targetType)
        {
            if (value == null)
                return GetDefaultValue(targetType);

            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            var strValue = value.ToString();

            if (targetType == typeof(int))
                return int.TryParse(strValue, out var intVal) ? intVal : 0;

            if (targetType == typeof(long))
                return long.TryParse(strValue, out var longVal) ? longVal : 0L;

            if (targetType == typeof(float))
                return float.TryParse(strValue, out var floatVal) ? floatVal : 0f;

            if (targetType == typeof(double))
                return double.TryParse(strValue, out var doubleVal) ? doubleVal : 0d;

            if (targetType == typeof(bool))
                return bool.TryParse(strValue, out var boolVal) && boolVal;

            if (targetType == typeof(string))
                return strValue;

            if (targetType.IsEnum)
            {
                if (Enum.TryParse(targetType, strValue, true, out var enumVal))
                    return enumVal;
                return Enum.GetValues(targetType).GetValue(0);
            }

            return GetDefaultValue(targetType);
        }

        private static object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            if (type == typeof(string))
                return "";
            return null;
        }

        /// <summary>
        /// テーブルの全レコードをクリアする。
        /// </summary>
        public static void ClearTable(ITableAsset table)
        {
            if (table == null)
                return;

            while (table.Count > 0)
            {
                table.RemoveRecordAt(0);
            }

            var unityObject = table as UnityEngine.Object;
            if (unityObject != null)
                EditorUtility.SetDirty(unityObject);
        }

        /// <summary>
        /// デフォルト設定を生成する。
        /// </summary>
        public static TableGeneratorConfig CreateDefaultConfig(ITableAsset table)
        {
            var config = new TableGeneratorConfig
            {
                TableName = table.GetType().Name,
                RecordCount = 100
            };

            var fields = table.RecordType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var fieldConfig = new FieldGeneratorConfig
                {
                    FieldName = field.Name
                };

                if (field.GetCustomAttribute<PrimaryKeyAttribute>() != null)
                {
                    fieldConfig.Rule = GeneratorRule.Sequential;
                    fieldConfig.StartValue = table.Count + 1;
                }
                else
                {
                    fieldConfig.Rule = GeneratorRule.Random;
                }

                config.FieldConfigs.Add(fieldConfig);
            }

            return config;
        }
    }
}
