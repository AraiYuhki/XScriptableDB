using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SecondaryKeyインデックスの定義。
    /// 単一カラムまたは複数カラムの複合インデックスをサポート。
    /// </summary>
    [Serializable]
    public class IndexDefinition
    {
        [SerializeField]
        private string name;

        [SerializeField]
        private List<string> columns = new();

        [SerializeField]
        private bool allowDuplicates = true;

        /// <summary>
        /// インデックス名。
        /// Editor操作を想定しているため、編集用にsetを公開する。
        /// </summary>
        public string Name
        {
            get => name;
            set => name = value;
        }

        /// <summary>
        /// インデックスに含まれるカラム名（順序付き）。
        /// 1つの場合は単一インデックス、2つ以上の場合は複合インデックス。
        /// Editor操作を想定しているため、編集用にsetを公開する。
        /// </summary>
        public List<string> Columns
        {
            get => columns;
            set => columns = value ?? new List<string>();
        }

        /// <summary>
        /// 同じキー値の複数レコードを許可するかどうか。
        /// Editor操作を想定しているため、編集用にsetを公開する。
        /// </summary>
        public bool AllowDuplicates
        {
            get => allowDuplicates;
            set => allowDuplicates = value;
        }

        /// <summary>
        /// 複合インデックスかどうか。
        /// </summary>
        public bool IsComposite => columns.Count > 1;

        public IndexDefinition()
        {
            columns = new List<string>();
        }

        public IndexDefinition(string name, params string[] columnNames)
        {
            this.name = name;
            columns = new List<string>(columnNames);
        }

        public override string ToString()
        {
            var columnsStr = string.Join(", ", columns);
            return IsComposite
                ? $"{name} ({columnsStr}) [Composite]"
                : $"{name} ({columnsStr})";
        }
    }
}
