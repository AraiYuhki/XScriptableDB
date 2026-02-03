using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;
using Xeon.XScriptableDB.Validation;

namespace Xeon.XScriptableDB.Samples.Validation
{
    /// <summary>
    /// スキルデータのレコードクラス。
    /// 各種バリデーション属性を使用してデータ品質を担保する。
    /// </summary>
    [Serializable]
    public class SkillRecord
    {
        [SerializeField, CsvColumn("ID"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("スキル名"), Required, StringLength(1, 30)]
        private string name;

        [SerializeField, CsvColumn("説明"), StringLength(200)]
        private string description;

        [SerializeField, CsvColumn("MP消費"), Range(0, 999)]
        private int mpCost;

        [SerializeField, CsvColumn("クールダウン"), Range(0f, 300f)]
        private float cooldown;

        [SerializeField, CsvColumn("威力"), Range(0, 9999)]
        private int power;

        [SerializeField, CsvColumn("属性")]
        private ElementType elementType;

        [SerializeField, CsvColumn("対象")]
        private TargetType targetType;

        [SerializeField, CsvColumn("習得Lv"), Range(1, 100)]
        [Compare("maxLevel", CompareOperator.LessThanOrEqual)]
        private int requiredLevel;

        [SerializeField, CsvColumn("最大Lv"), Range(1, 100)]
        private int maxLevel;

        /// <summary>
        /// スキルID（主キー）
        /// </summary>
        public int Id => id;

        /// <summary>
        /// スキル名
        /// </summary>
        public string Name => name;

        /// <summary>
        /// 説明文
        /// </summary>
        public string Description => description;

        /// <summary>
        /// MP消費量
        /// </summary>
        public int MpCost => mpCost;

        /// <summary>
        /// クールダウン秒数
        /// </summary>
        public float Cooldown => cooldown;

        /// <summary>
        /// 威力
        /// </summary>
        public int Power => power;

        /// <summary>
        /// 属性タイプ
        /// </summary>
        public ElementType ElementType => elementType;

        /// <summary>
        /// 対象タイプ
        /// </summary>
        public TargetType TargetType => targetType;

        /// <summary>
        /// 習得レベル
        /// </summary>
        public int RequiredLevel => requiredLevel;

        /// <summary>
        /// 最大強化レベル
        /// </summary>
        public int MaxLevel => maxLevel;

        public override string ToString()
        {
            return $"[{id}] {name} (MP:{mpCost}, 威力:{power}, {elementType})";
        }
    }
}
