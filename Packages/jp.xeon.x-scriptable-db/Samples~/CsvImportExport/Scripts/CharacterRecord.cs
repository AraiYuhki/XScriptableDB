using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Samples.CsvImportExport
{
    /// <summary>
    /// キャラクターデータのレコードクラス。
    /// 日本語のCSV列名を使用して、企画担当者が編集しやすいようにしている。
    /// </summary>
    [Serializable]
    public class CharacterRecord : CsvData
    {
        [SerializeField, CsvColumn("ID"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("名前")]
        private string name;

        [SerializeField, CsvColumn("レベル")]
        private int level;

        [SerializeField, CsvColumn("HP")]
        private int hp;

        [SerializeField, CsvColumn("攻撃力")]
        private int attack;

        [SerializeField, CsvColumn("防御力")]
        private int defense;

        [SerializeField, CsvColumn("職業"), SecondaryKey]
        private string characterClass;

        [SerializeField, CsvColumn("プレイアブル"), SecondaryKey]
        private bool isPlayable;

        /// <summary>
        /// キャラクターID（主キー）
        /// </summary>
        public int Id => id;

        /// <summary>
        /// キャラクター名
        /// </summary>
        public string Name => name;

        /// <summary>
        /// レベル
        /// </summary>
        public int Level => level;

        /// <summary>
        /// HP
        /// </summary>
        public int Hp => hp;

        /// <summary>
        /// 攻撃力
        /// </summary>
        public int Attack => attack;

        /// <summary>
        /// 防御力
        /// </summary>
        public int Defense => defense;

        /// <summary>
        /// 職業
        /// </summary>
        public string CharacterClass => characterClass;

        /// <summary>
        /// プレイアブルキャラクターかどうか
        /// </summary>
        public bool IsPlayable => isPlayable;

        public override string ToString()
        {
            return $"[{id}] {name} Lv.{level} ({characterClass}) HP:{hp} ATK:{attack} DEF:{defense}";
        }
    }
}
