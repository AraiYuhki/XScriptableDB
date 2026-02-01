using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xeon.XScriptableDB.Editor
{
    public static class ClassGenerator
    {
        private static readonly HashSet<string> PrimitiveTypeList = new() {
            "byte", "sbyte", "short", "ushort",
            "int", "uint", "long", "ulong",
            "float", "double", "decimal",
            "bool", "char", "DateTime"
        };

        public static string GenerateTable(TableDefinition definition)
        {
            var namespaceName = "Xeon.XScriptableDB.Generated";
            var className = definition.TableName.ToPascalCase();
            var primaryKey = definition.Columns.FirstOrDefault(column => column.IsPrimaryKey) ?? definition.Columns[0];
            var primaryKeyType = primaryKey.Type;
            return $@"using UnityEngine;
using Xeon.XScriptableDB;
namespace {namespaceName}
{{
    [CreateAssetMenu(fileName = ""{className}"", menuName = ""Xeon/XScriptableDB/{className}"")]
    public class {className}Table : TableAsset<{className}Record, {primaryKeyType}>
    {{
    }}
}}
";
        }

        public static string GenerateRecord(TableDefinition definition)
        {
            var namespaceName = "Xeon.XScriptableDB.Generated";
            var className = definition.TableName.ToPascalCase();
            var fields = GenerateFields(definition);
            var properties = GenerateProperties(definition.Columns, definition.IsReadOnly);
            return $@"using System;
using UnityEngine;
using Xeon.XScriptableDB;

namespace {namespaceName}
{{
    [Serializable]
    public partial class {className}Record
    {{
{fields}

{properties}
    }}
}}";
        }

        private static string GenerateFields(TableDefinition tableDefinition)
        {
            var columns = tableDefinition.Columns;
            var fieldList = new List<string>(columns.Count);

            foreach (var column in columns)
                fieldList.Add(GenerateField(tableDefinition, column));

            return string.Join("\n\n", fieldList);
        }

        private static string GenerateField(TableDefinition tableDefinition, ColumnDefinition column)
        {
            var type = ConvertType(column.Type, column.IsNullable);
            var fieldName = column.Name.ToCamelCase();
            var sb = new StringBuilder();

            var attributes = new List<string>() { "SerializeField" };

            if (column.IsPrimaryKey)
                attributes.Add("PrimaryKey");
            if (tableDefinition.Indices.Contains(column.Name))
                attributes.Add("SecondaryKey");

            sb.Append($"        [{string.Join(", ", attributes)}]\n");
            sb.Append($"        private {type} {fieldName};");

            return sb.ToString();
        }

        private static string GenerateProperties(List<ColumnDefinition> columns, bool isReadOnly)
        {
            var propertyList = new List<string>(columns.Count);

            foreach (var column in columns)
                propertyList.Add(GenerateProperty(column, isReadOnly));

            return string.Join("\n\n", propertyList);
        }

        private static string GenerateProperty(ColumnDefinition column, bool isReadOnly)
        {
            var type = ConvertType(column.Type, column.IsNullable);
            var fieldName = column.Name.ToCamelCase();
            var propertyName = column.Name.ToPascalCase();
            if (!isReadOnly)
            {
                return $@"        public {type} {propertyName}
        {{
            get => {fieldName};
            set => {fieldName} = value;
        }}";
            }

            return $@"        public {type} {propertyName}
        {{
            get => {fieldName};
#if UNITY_EDITOR
            set => {fieldName} = value;
#endif
        }}";
        }

        private static string ConvertType(string typeName, bool isNullable)
        {
            var result = typeName.ToLower();

            switch (result)
            {
                case "tinyint":
                    result = "byte";
                    break;
                case "smallint":
                    result = "short";
                    break;
                case "int":
                case "integer":
                case "mediumint":
                    result = "int";
                    break;
                case "bigint":
                    result = "long";
                    break;
                case "float":
                case "real":
                    result = "float";
                    break;
                case "double":
                    result = "double";
                    break;
                case "decimal":
                case "numeric":
                    result = "decimal";
                    break;
                case "bool":
                case "boolean":
                    result = "bool";
                    break;
                case "char":
                    result = "char";
                    break;
                case "varchar":
                case "text":
                case "longtext":
                case "mediumtext":
                case "tinytext":
                    result = "string";
                    break;
                case "datetime":
                case "timestamp":
                case "date":
                    result = "DateTime";
                    break;
                default:
                    result = typeName;
                    break;
            }

            if (IsPrimitiveOrValueType(result) && isNullable)
                result += "?";

            return result;
        }

        private static bool IsPrimitiveOrValueType(string typeName) => PrimitiveTypeList.Contains(typeName);
    }
}
