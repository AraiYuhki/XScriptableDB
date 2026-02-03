using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// カテゴリマスタのレコードクラス。
    /// アイテムの分類に使用される。
    /// </summary>
    [Serializable]
    public class CategoryRecord
    {
        [SerializeField, CsvColumn("ID"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("カテゴリ名")]
        private string name;

        [SerializeField, CsvColumn("説明")]
        private string description;

        [SerializeField, CsvColumn("表示順")]
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
