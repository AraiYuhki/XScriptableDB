using System;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Column information.
    /// </summary>
    public class ColumnInfo
    {
        public string Name { get; }
        public Type FieldType { get; }
        public bool IsPrimaryKey { get; }
        public bool IsSecondaryKey { get; }
        public string TypeDisplayName { get; }

        public ColumnInfo(string name, Type fieldType, bool isPrimaryKey, bool isSecondaryKey, string typeDisplayName)
        {
            Name = name;
            FieldType = fieldType;
            IsPrimaryKey = isPrimaryKey;
            IsSecondaryKey = isSecondaryKey;
            TypeDisplayName = typeDisplayName;
        }
    }
}
