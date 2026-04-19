using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Editor
{
    public static class ClassGenerator
    {
        private static readonly HashSet<string> PrimitiveTypeList = new() {
            "byte", "sbyte", "short", "ushort",
            "int", "uint", "long", "ulong",
            "float", "double", "decimal",
            "bool", "char", "SerializableDateTime"
        };

        public static string GenerateTable(TableDefinition definition, string namespaceName = "Xeon.XScriptableDB.Generated")
        {
            var className = definition.TableName.SnakeToPascalCase();
            var primaryKey = definition.Columns.FirstOrDefault(column => column.IsPrimaryKey) ?? definition.Columns[0];
            var primaryKeyType = ConvertType(primaryKey.Type, false);
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

        public static string GenerateRecord(TableDefinition definition, string namespaceName = "Xeon.XScriptableDB.Generated")
        {
            var className = definition.TableName.SnakeToPascalCase();
            var fields = GenerateFields(definition);
            var properties = GenerateProperties(definition.Columns, definition.IsReadOnly);
            return $@"using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

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
            var csvColumnName = column.Name;
            var fieldName = column.Name.SnakeToCamelCase();
            var sb = new StringBuilder();

            var attributes = new List<string>()
            {
                "SerializeField",
                $"CsvColumn(\"{csvColumnName}\")"
            };

            if (column.IsPrimaryKey)
                attributes.Add("PrimaryKey");

            attributes.AddRange(BuildSecondaryKeyAttributes(tableDefinition, column));

            sb.Append($"        [{string.Join(", ", attributes)}]\n");
            sb.Append($"        private {type} _{fieldName};");

            return sb.ToString();
        }

        private static IEnumerable<string> BuildSecondaryKeyAttributes(TableDefinition tableDefinition, ColumnDefinition column)
        {
            foreach (var indexDef in tableDefinition.Indices)
            {
                var columnIndex = indexDef.Columns.IndexOf(column.Name);
                if (columnIndex < 0)
                    continue;

                if (indexDef.IsComposite)
                {
                    yield return BuildCompositeSecondaryKeyAttribute(indexDef, columnIndex);
                    continue;
                }

                yield return BuildSingleSecondaryKeyAttribute(indexDef, column.Name);
            }
        }

        private static string BuildCompositeSecondaryKeyAttribute(IndexDefinition indexDef, int columnIndex)
        {
            var attrParts = new List<string> { $"\"{indexDef.Name}\"", columnIndex.ToString() };
            if (!indexDef.AllowDuplicates)
                attrParts.Add("AllowDuplicates = false");
            return $"SecondaryKey({string.Join(", ", attrParts)})";
        }

        private static string BuildSingleSecondaryKeyAttribute(IndexDefinition indexDef, string columnName)
        {
            if (indexDef.Name == columnName)
                return indexDef.AllowDuplicates ? "SecondaryKey" : "SecondaryKey(AllowDuplicates = false)";

            var attrParts = new List<string> { $"\"{indexDef.Name}\"" };
            if (!indexDef.AllowDuplicates)
                attrParts.Add("AllowDuplicates = false");
            return $"SecondaryKey({string.Join(", ", attrParts)})";
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
            var fieldName = column.Name.SnakeToCamelCase();
            var propertyName = column.Name.SnakeToPascalCase();
            if (!isReadOnly)
            {
                return $@"        public {type} {propertyName}
        {{
            get => _{fieldName};
            set => _{fieldName} = value;
        }}";
            }

            return $@"        public {type} {propertyName}
        {{
            get => _{fieldName};
#if UNITY_EDITOR
            set => _{fieldName} = value;
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
                    result = "SerializableDateTime";
                    break;
                default:
                    // enum型やユーザー定義型をPascalCaseに変換します
                    result = typeName.SnakeToPascalCase();
                    break;
            }

            if (IsPrimitiveOrValueType(result) && isNullable)
                result += "?";

            return result;
        }

        private static bool IsPrimitiveOrValueType(string typeName) => PrimitiveTypeList.Contains(typeName);
    }
}
