using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// フィールド情報。
    /// </summary>
    public class FieldSchemaInfo
    {
        public string Name { get; set; }
        public Type FieldType { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsSecondaryKey { get; set; }
        public bool IsReadOnly { get; set; }
        public List<Attribute> Attributes { get; set; } = new();

        public static FieldSchemaInfo FromFieldInfo(FieldInfo field)
        {
            var info = new FieldSchemaInfo
            {
                Name = field.Name,
                FieldType = field.FieldType,
                IsPrimaryKey = field.GetCustomAttribute<PrimaryKeyAttribute>() != null,
                IsSecondaryKey = field.GetCustomAttribute<SecondaryKeyAttribute>() != null,
                IsReadOnly = field.GetCustomAttribute<ReadOnlyAttribute>() != null,
                Attributes = field.GetCustomAttributes().ToList()
            };
            return info;
        }
    }
}
