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
        public string Name { get; }
        public Type FieldType { get; }
        public bool IsPrimaryKey { get; }
        public bool IsSecondaryKey { get; }
        public bool IsReadOnly { get; }
        public List<Attribute> Attributes { get; } = new();

        public FieldSchemaInfo(FieldInfo field)
        {
            Name = field.Name;
            FieldType = field.FieldType;
            IsPrimaryKey = field.GetCustomAttribute<PrimaryKeyAttribute>() != null;
            IsSecondaryKey = field.GetCustomAttributes<SecondaryKeyAttribute>().Any();
            IsReadOnly = field.GetCustomAttribute<ReadOnlyAttribute>() != null;
            Attributes = field.GetCustomAttributes().ToList();
        }

        public static FieldSchemaInfo FromFieldInfo(FieldInfo field)
        {
            return new FieldSchemaInfo(field);
        }
    }
}
