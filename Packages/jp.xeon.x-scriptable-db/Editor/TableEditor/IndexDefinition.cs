using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SecondaryKeyインデックスの定義。
    /// 単一フィールドインデックスと複合インデックスの両方を表現できます。
    /// </summary>
    /// <remarks>
    /// このクラスはエディタでの使用を目的としているため、編集用にセッターが公開されています。
    /// </remarks>
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
        /// インデックスの名前。
        /// </summary>
        public string Name
        {
            get => name;
            set => name = value;
        }

        /// <summary>
        /// インデックスを構成する列名のリスト。
        /// 複数の列が存在する場合、それは複合インデックスです。
        /// </summary>
        public List<string> Columns
        {
            get => columns;
            set => columns = value ?? new List<string>();
        }

        /// <summary>
        /// 同じキー値を持つ複数のレコードを許可するかどうか。
        /// </summary>
        public bool AllowDuplicates
        {
            get => allowDuplicates;
            set => allowDuplicates = value;
        }

        /// <summary>
        /// これが複合インデックスかどうか。
        /// </summary>
        public bool IsComposite => columns.Count > 1;

        public IndexDefinition()
        {
        }

        public IndexDefinition(string name)
        {
            this.name = name;
            columns = new List<string> { name };
        }

        public IndexDefinition(string name, params string[] columnNames)
        {
            this.name = name;
            columns = new List<string>(columnNames);
        }

        /// <summary>
        /// 単一の列からインデックス定義を作成します。
        /// </summary>
        public static IndexDefinition FromSingleColumn(string columnName)
        {
            return new IndexDefinition(columnName) { columns = new List<string> { columnName } };
        }

        /// <summary>
        /// 複合インデックス定義を作成します。
        /// </summary>
        public static IndexDefinition FromComposite(string name, params string[] columnNames)
        {
            return new IndexDefinition(name, columnNames);
        }

        /// <summary>
        /// レガシーフォーマット（文字列）から移行します。
        /// </summary>
        public static IndexDefinition FromLegacy(string legacyIndexName)
        {
            return FromSingleColumn(legacyIndexName);
        }

        public override string ToString()
        {
            var columnsStr = string.Join(", ", columns);
            var duplicateStr = allowDuplicates ? "" : " (unique)";
            return $"{name}: [{columnsStr}]{duplicateStr}";
        }
    }
}
