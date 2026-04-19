using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;
using Xeon.XScriptableDB.Validation;

namespace Xeon.XScriptableDB.Samples.ForeignKeyReference
{
    /// <summary>
    /// レシピマスターのレコードクラス。
    /// アイテムへの複数の外部キー参照を保持します。
    /// </summary>
    [Serializable]
    public class RecipeRecord
    {
        [SerializeField, CsvColumn("ID"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("RecipeName")]
        private string name;

        [SerializeField, CsvColumn("ResultItemID"), ForeignKey(typeof(ItemTable))]
        private int resultItemId;

        [SerializeField, CsvColumn("ResultCount")]
        private int resultCount;

        [SerializeField, CsvColumn("Material1ID")]
        private int material1Id;

        [SerializeField, CsvColumn("Material1Count")]
        private int material1Count;

        [SerializeField, CsvColumn("Material2ID")]
        private int material2Id;

        [SerializeField, CsvColumn("Material2Count")]
        private int material2Count;

        [SerializeField, CsvColumn("Material3ID")]
        private int material3Id;

        [SerializeField, CsvColumn("Material3Count")]
        private int material3Count;

        /// <summary>
        /// レシピID（主キー）
        /// </summary>
        public int Id => id;

        /// <summary>
        /// レシピ名
        /// </summary>
        public string Name => name;

        /// <summary>
        /// 完成品アイテムID（外部キー）
        /// </summary>
        public int ResultItemId => resultItemId;

        /// <summary>
        /// 完成個数
        /// </summary>
        public int ResultCount => resultCount;

        /// <summary>
        /// 素材1のアイテムID（0は未設定）
        /// </summary>
        public int Material1Id => material1Id;

        /// <summary>
        /// 素材1の必要数
        /// </summary>
        public int Material1Count => material1Count;

        /// <summary>
        /// 素材2のアイテムID（0は未設定）
        /// </summary>
        public int Material2Id => material2Id;

        /// <summary>
        /// 素材2の必要数
        /// </summary>
        public int Material2Count => material2Count;

        /// <summary>
        /// 素材3のアイテムID（0は未設定）
        /// </summary>
        public int Material3Id => material3Id;

        /// <summary>
        /// 素材3の必要数
        /// </summary>
        public int Material3Count => material3Count;

        /// <summary>
        /// 素材1が設定されているかどうか
        /// </summary>
        public bool HasMaterial1 => material1Id > 0;

        /// <summary>
        /// 素材2が設定されているかどうか
        /// </summary>
        public bool HasMaterial2 => material2Id > 0;

        /// <summary>
        /// 素材3が設定されているかどうか
        /// </summary>
        public bool HasMaterial3 => material3Id > 0;

        public override string ToString() => $"[{id}] {name}";
    }
}
