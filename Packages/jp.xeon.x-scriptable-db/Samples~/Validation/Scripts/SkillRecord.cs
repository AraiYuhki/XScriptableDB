using System;
using UnityEngine;
using Xeon.XScriptableDB.IO;
using Xeon.XScriptableDB.Validation;
using Range = Xeon.XScriptableDB.Validation.RangeAttribute;

namespace Xeon.XScriptableDB.Samples.Validation
{
    /// <summary>
    /// スキルデータのレコードクラス。
    /// データの品質を確保するために様々なバリデーション属性を使用しています。
    /// </summary>
    [Serializable]
    public class SkillRecord
    {
        [SerializeField, CsvColumn("ID"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("SkillName"), Required, StringLength(30)]
        private string name;

        [SerializeField, CsvColumn("Description"), StringLength(200)]
        private string description;

        [SerializeField, CsvColumn("MPCost"), Range(0, 999)]
        private int mpCost;

        [SerializeField, CsvColumn("Cooldown"), Range(0f, 300f)]
        private float cooldown;

        [SerializeField, CsvColumn("Power"), Range(0, 9999)]
        private int power;

        [SerializeField, CsvColumn("Element")]
        private ElementType elementType;

        [SerializeField, CsvColumn("Target")]
        private TargetType targetType;

        [SerializeField, CsvColumn("RequiredLevel"), Range(1, 100)]
        [Compare("maxLevel", Operator = CompareOperator.LessThanOrEqual)]
        private int requiredLevel;

        [SerializeField, CsvColumn("MaxLevel"), Range(1, 100)]
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
        /// 説明
        /// </summary>
        public string Description => description;

        /// <summary>
        /// 消費MP
        /// </summary>
        public int MpCost => mpCost;

        /// <summary>
        /// クールダウン（秒）
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
        /// 習得に必要なレベル
        /// </summary>
        public int RequiredLevel => requiredLevel;

        /// <summary>
        /// 最大強化レベル
        /// </summary>
        public int MaxLevel => maxLevel;

        public override string ToString()
        {
            return $"[{id}] {name} (MP:{mpCost}, Power:{power}, {elementType})";
        }
    }
}
