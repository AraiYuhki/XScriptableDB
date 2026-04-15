using System;
using UnityEngine;
using Xeon.XScriptableDB.IO;
using Xeon.XScriptableDB.Validation;
using Range = Xeon.XScriptableDB.Validation.RangeAttribute;

namespace Xeon.XScriptableDB.Samples.Validation
{
    /// <summary>
    /// Record class for skill data.
    /// Uses various validation attributes to ensure data quality.
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
        /// Skill ID (primary key)
        /// </summary>
        public int Id => id;

        /// <summary>
        /// Skill name
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Description
        /// </summary>
        public string Description => description;

        /// <summary>
        /// MP cost
        /// </summary>
        public int MpCost => mpCost;

        /// <summary>
        /// Cooldown in seconds
        /// </summary>
        public float Cooldown => cooldown;

        /// <summary>
        /// Power
        /// </summary>
        public int Power => power;

        /// <summary>
        /// Element type
        /// </summary>
        public ElementType ElementType => elementType;

        /// <summary>
        /// Target type
        /// </summary>
        public TargetType TargetType => targetType;

        /// <summary>
        /// Required level to learn
        /// </summary>
        public int RequiredLevel => requiredLevel;

        /// <summary>
        /// Maximum upgrade level
        /// </summary>
        public int MaxLevel => maxLevel;

        public override string ToString()
        {
            return $"[{id}] {name} (MP:{mpCost}, Power:{power}, {elementType})";
        }
    }
}
