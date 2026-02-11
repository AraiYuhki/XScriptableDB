using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;
using Xeon.XScriptableDB.Validation;
using Range =  Xeon.XScriptableDB.Validation.RangeAttribute;

namespace Xeon.XScriptableDB.Samples.Validation
{
    /// <summary>
    /// 武器データのレコードクラス。
    /// 外部キー参照のバリデーションを示すサンプル。
    /// </summary>
    [Serializable]
    public class WeaponRecord
    {
        [SerializeField, CsvColumn("ID"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("武器名"), Required]
        private string name;

        [SerializeField, CsvColumn("スキルID"), ForeignKey(typeof(SkillTable))]
        private int skillId;

        [SerializeField, CsvColumn("攻撃力"), Range(1, 9999)]
        private int attack;

        [SerializeField, CsvColumn("レアリティ"), Range(1, 5)]
        private int rarity;

        /// <summary>
        /// 武器ID（主キー）
        /// </summary>
        public int Id => id;

        /// <summary>
        /// 武器名
        /// </summary>
        public string Name => name;

        /// <summary>
        /// 付与スキルID（SkillTableへの外部キー）
        /// </summary>
        public int SkillId => skillId;

        /// <summary>
        /// 攻撃力
        /// </summary>
        public int Attack => attack;

        /// <summary>
        /// レアリティ（1-5）
        /// </summary>
        public int Rarity => rarity;

        public override string ToString()
        {
            return $"[{id}] {name} (ATK:{attack}, ★{rarity}, Skill:{skillId})";
        }
    }
}
