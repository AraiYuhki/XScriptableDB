using System;
using System.Text;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    [Serializable]
    public class ColumnDefinition
    {
        [SerializeField]
        private string name;
        [SerializeField]
        private string type;
        [SerializeField]
        private bool isNullable;
        [SerializeField]
        private bool isPrimaryKey;

        public string Name
        {
            get => name;
            set => name = value;
        }

        public string Type
        {
            get => type;
            set => type = value;
        }

        public bool IsNullable
        {
            get => isNullable;
            set => isNullable = value;
        }

        public bool IsPrimaryKey
        {
            get => isPrimaryKey;
            set => isPrimaryKey = value;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Name: {name}");
            sb.AppendLine($"Type: {type}");
            if (isNullable)
                sb.Append("Is Nullable");
            if (isPrimaryKey)
                sb.AppendLine("Primary Key");
            return sb.ToString();
        }
    }
}
