using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;
using Xeon.XScriptableDB.Validation;
using Range =  Xeon.XScriptableDB.Validation.RangeAttribute;

namespace Xeon.XScriptableDB.Samples.Validation
{
    /// <summary>
    /// Record class for weapon data.
    /// Sample demonstrating foreign key reference validation.
    /// </summary>
    [Serializable]
    public class WeaponRecord
    {
        [SerializeField, CsvColumn("ID"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("WeaponName"), Required]
        private string name;

        [SerializeField, CsvColumn("SkillID"), ForeignKey(typeof(SkillTable))]
        private int skillId;

        [SerializeField, CsvColumn("Attack"), Range(1, 9999)]
        private int attack;

        [SerializeField, CsvColumn("Rarity"), Range(1, 5)]
        private int rarity;

        /// <summary>
        /// Weapon ID (primary key)
        /// </summary>
        public int Id => id;

        /// <summary>
        /// Weapon name
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Granted skill ID (foreign key to SkillTable)
        /// </summary>
        public int SkillId => skillId;

        /// <summary>
        /// Attack power
        /// </summary>
        public int Attack => attack;

        /// <summary>
        /// Rarity (1-5)
        /// </summary>
        public int Rarity => rarity;

        public override string ToString()
        {
            return $"[{id}] {name} (ATK:{attack}, ★{rarity}, Skill:{skillId})";
        }
    }
}
