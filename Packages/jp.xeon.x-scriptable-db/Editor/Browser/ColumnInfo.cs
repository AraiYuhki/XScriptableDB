using System;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// カラム情報。
    /// </summary>
    public class ColumnInfo
    {
        public string Name { get; set; }
        public Type FieldType { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsSecondaryKey { get; set; }
        public string TypeDisplayName { get; set; }
    }
}
