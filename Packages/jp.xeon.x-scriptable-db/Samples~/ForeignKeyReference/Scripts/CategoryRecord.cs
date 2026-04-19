using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// カテゴリマスターのレコードクラス。
    /// アイテムの分類に使用されます。
    /// </summary>
    [Serializable]
    public class CategoryRecord
    {
        [SerializeField, CsvColumn("ID"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("CategoryName")]
        private string name;

        [SerializeField, CsvColumn("Description")]
        private string description;

        [SerializeField, CsvColumn("SortOrder")]
        private int sortOrder;

        /// <summary>
        /// カテゴリID（主キー）
        /// </summary>
        public int Id => id;

        /// <summary>
        /// カテゴリ名
        /// </summary>
        public string Name => name;

        /// <summary>
        /// 説明
        /// </summary>
        public string Description => description;

        /// <summary>
        /// 表示順
        /// </summary>
        public int SortOrder => sortOrder;

        public override string ToString() => $"[{id}] {name}";
    }
}
