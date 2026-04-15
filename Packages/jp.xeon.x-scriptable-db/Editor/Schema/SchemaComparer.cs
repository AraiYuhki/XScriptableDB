using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Types of schema differences.
    /// </summary>
    public enum SchemaDifferenceType
    {
        FieldAdded,
        FieldRemoved,
        FieldTypeChanged,
        FieldAttributeChanged,
        PrimaryKeyChanged,
        SecondaryKeyAdded,
        SecondaryKeyRemoved
    }

    /// <summary>
    /// Schema comparison utility.
    /// </summary>
    public static class SchemaComparer
    {
        private static readonly HashSet<Type> NumericTypes = new()
        {
            typeof(byte), typeof(sbyte),
            typeof(short), typeof(ushort),
            typeof(int), typeof(uint),
            typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal)
        };

        /// <summary>
        /// Compares the schemas of two types.
        /// </summary>
        public static SchemaComparisonResult Compare(Type sourceType, Type targetType)
        {
            var result = new SchemaComparisonResult(sourceType, targetType);

            var sourceFields = GetFieldSchemas(sourceType);
            var targetFields = GetFieldSchemas(targetType);

            var sourceFieldNames = new HashSet<string>(sourceFields.Keys);
            var targetFieldNames = new HashSet<string>(targetFields.Keys);

            // Added fields
            foreach (var name in targetFieldNames.Except(sourceFieldNames))
            {
                var field = targetFields[name];
                result.Differences.Add(new(SchemaDifferenceType.FieldAdded, name, null, field.FieldType.Name));
            }

            // Removed fields
            foreach (var name in sourceFieldNames.Except(targetFieldNames))
            {
                var field = sourceFields[name];
                result.Differences.Add(new(SchemaDifferenceType.FieldRemoved, name, field.FieldType.Name, null));
                result.IsCompatible = false;
                result.CompatibilityNote = "Fields have been removed, potential data loss may occur";
            }

            // Compare common fields
            foreach (var name in sourceFieldNames.Intersect(targetFieldNames))
            {
                var sourceField = sourceFields[name];
                var targetField = targetFields[name];

                // Type change
                if (sourceField.FieldType != targetField.FieldType)
                {
                    result.Differences.Add(new(SchemaDifferenceType.FieldTypeChanged, name, sourceField.FieldType.Name, targetField.FieldType.Name));

                    if (!IsTypeConvertible(sourceField.FieldType, targetField.FieldType))
                    {
                        result.IsCompatible = false;
                        result.CompatibilityNote = $"Type conversion for field '{name}' is not possible";
                    }
                }

                // PrimaryKey change
                if (sourceField.IsPrimaryKey != targetField.IsPrimaryKey)
                {
                    result.Differences.Add(new(SchemaDifferenceType.PrimaryKeyChanged, name, sourceField.IsPrimaryKey, targetField.IsPrimaryKey));
                }

                // SecondaryKey change
                if (sourceField.IsSecondaryKey && !targetField.IsSecondaryKey)
                {
                    result.Differences.Add(new(SchemaDifferenceType.SecondaryKeyRemoved, name));
                }
                else if (!sourceField.IsSecondaryKey && targetField.IsSecondaryKey)
                {
                    result.Differences.Add(new(SchemaDifferenceType.SecondaryKeyAdded, name));
                }
            }

            return result;
        }

        /// <summary>
        /// Gets field schema information from a type.
        /// </summary>
        public static Dictionary<string, FieldSchemaInfo> GetFieldSchemas(Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var result = new Dictionary<string, FieldSchemaInfo>();

            foreach (var field in fields)
            {
                result[field.Name] = FieldSchemaInfo.FromFieldInfo(field);
            }

            return result;
        }

        /// <summary>
        /// Determines whether conversion between types is possible.
        /// </summary>
        private static bool IsTypeConvertible(Type from, Type to)
        {
            // Same type
            if (from == to)
                return true;

            // Numeric type conversion
            if (NumericTypes.Contains(from) && NumericTypes.Contains(to))
                return true;

            // Conversion to string is always possible
            if (to == typeof(string))
                return true;

            // Nullable type
            var underlyingFrom = Nullable.GetUnderlyingType(from);
            var underlyingTo = Nullable.GetUnderlyingType(to);

            if (underlyingFrom != null && underlyingTo != null)
                return IsTypeConvertible(underlyingFrom, underlyingTo);

            if (underlyingTo != null && from == underlyingTo)
                return true;

            return false;
        }

        /// <summary>
        /// Compares the schemas between table assets.
        /// </summary>
        public static SchemaComparisonResult Compare(ITableAsset source, ITableAsset target)
        {
            return Compare(source.RecordType, target.RecordType);
        }

        /// <summary>
        /// Generates a schema summary.
        /// </summary>
        public static string GenerateSchemaSummary(Type type)
        {
            var fields = GetFieldSchemas(type);
            var lines = new List<string>
            {
                $"Table: {type.Name}",
                $"Field Count: {fields.Count}",
                ""
            };

            var primaryKey = fields.Values.FirstOrDefault(f => f.IsPrimaryKey);
            if (primaryKey != null)
                lines.Add($"PrimaryKey: {primaryKey.Name} ({primaryKey.FieldType.Name})");

            var secondaryKeys = fields.Values.Where(f => f.IsSecondaryKey).ToList();
            if (secondaryKeys.Count > 0)
            {
                lines.Add($"SecondaryKeys: {string.Join(", ", secondaryKeys.Select(f => f.Name))}");
            }

            lines.Add("");
            lines.Add("Fields:");
            foreach (var field in fields.Values.OrderBy(f => f.Name))
            {
                var attrs = new List<string>();
                if (field.IsPrimaryKey)
                    attrs.Add("PK");
                if (field.IsSecondaryKey)
                    attrs.Add("SK");
                if (field.IsReadOnly)
                    attrs.Add("RO");

                var attrStr = attrs.Count > 0 ? $" [{string.Join(", ", attrs)}]" : "";
                lines.Add($"  {field.Name}: {field.FieldType.Name}{attrStr}");
            }

            return string.Join("\n", lines);
        }
    }
}
