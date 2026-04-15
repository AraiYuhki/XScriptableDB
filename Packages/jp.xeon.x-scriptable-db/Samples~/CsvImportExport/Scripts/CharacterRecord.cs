using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Samples.CsvImportExport
{
    /// <summary>
    /// Record class for character data.
    /// Uses English CSV column names for easy editing by designers.
    /// </summary>
    [Serializable]
    public class CharacterRecord : CsvData
    {
        [SerializeField, CsvColumn("ID"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("Name")]
        private string name;

        [SerializeField, CsvColumn("Level")]
        private int level;

        [SerializeField, CsvColumn("HP")]
        private int hp;

        [SerializeField, CsvColumn("Attack")]
        private int attack;

        [SerializeField, CsvColumn("Defense")]
        private int defense;

        [SerializeField, CsvColumn("Class"), SecondaryKey]
        private string characterClass;

        [SerializeField, CsvColumn("Playable"), SecondaryKey]
        private bool isPlayable;

        /// <summary>
        /// Character ID (primary key)
        /// </summary>
        public int Id => id;

        /// <summary>
        /// Character name
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Level
        /// </summary>
        public int Level => level;

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
        /// Character class
        /// </summary>
        public string CharacterClass => characterClass;

        /// <summary>
        /// Whether this is a playable character
        /// </summary>
        public bool IsPlayable => isPlayable;

        public override string ToString()
        {
            return $"[{id}] {name} Lv.{level} ({characterClass}) HP:{hp} ATK:{attack} DEF:{defense}";
        }
    }
}
