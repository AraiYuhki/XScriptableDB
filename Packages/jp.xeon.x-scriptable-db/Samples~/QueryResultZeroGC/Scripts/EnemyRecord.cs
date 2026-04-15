using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Samples.QueryResultZeroGC
{
    /// <summary>
    /// Record class for enemy data.
    /// Designed for large datasets intended for performance measurement.
    /// </summary>
    [Serializable]
    public class EnemyRecord
    {
        [SerializeField, CsvColumn("ID"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("Name")]
        private string name;

        [SerializeField, CsvColumn("HP")]
        private int hp;

        [SerializeField, CsvColumn("Attack")]
        private int attack;

        [SerializeField, CsvColumn("Defense")]
        private int defense;

        [SerializeField, CsvColumn("Level"), SecondaryKey]
        private int level;

        [SerializeField, CsvColumn("AreaID"), SecondaryKey]
        private int areaId;

        [SerializeField, CsvColumn("Boss"), SecondaryKey]
        private bool isBoss;

        [SerializeField, CsvColumn("DropRate")]
        private float dropRate;

        /// <summary>
        /// Enemy ID (primary key)
        /// </summary>
        public int Id => id;

        /// <summary>
        /// Enemy name
        /// </summary>
        public string Name => name;

        /// <summary>
        /// HP
        /// </summary>
        public int Hp => hp;

        /// <summary>
        /// Attack power
        /// </summary>
        public int Attack => attack;

        /// <summary>
        /// Defense power
        /// </summary>
        public int Defense => defense;

        /// <summary>
        /// Level
        /// </summary>
        public int Level => level;

        /// <summary>
        /// Spawn area ID
        /// </summary>
        public int AreaId => areaId;

        /// <summary>
        /// Boss flag
        /// </summary>
        public bool IsBoss => isBoss;

        /// <summary>
        /// Drop rate
        /// </summary>
        public float DropRate => dropRate;

        public override string ToString()
        {
            var bossFlag = isBoss ? "[BOSS]" : "";
            return $"[{id}] {name} {bossFlag} Lv.{level} HP:{hp} ATK:{attack} DEF:{defense}";
        }
    }
}
