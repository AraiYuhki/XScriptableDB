using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Samples.CsvImportExport
{
    /// <summary>
    /// キャラクターデータのレコードクラス。
    /// デザイナーが編集しやすいように英語のCSVカラム名を使用しています。
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
        /// クラス（職業）
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
