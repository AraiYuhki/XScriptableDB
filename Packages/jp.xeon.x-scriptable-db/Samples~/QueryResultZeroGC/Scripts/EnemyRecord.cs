using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Samples.QueryResultZeroGC
{
    /// <summary>
    /// エネミーデータのレコードクラス。
    /// パフォーマンス測定用の大規模データセット向けに設計されています。
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
        /// エネミーID（主キー）
        /// </summary>
        public int Id => id;

        /// <summary>
        /// エネミー名
        /// </summary>
        public string Name => name;

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
        /// レベル
        /// </summary>
        public int Level => level;

        /// <summary>
        /// 出現エリアID
        /// </summary>
        public int AreaId => areaId;

        /// <summary>
        /// ボスフラグ
        /// </summary>
        public bool IsBoss => isBoss;

        /// <summary>
        /// ドロップ率
        /// </summary>
        public float DropRate => dropRate;

        public override string ToString()
        {
            var bossFlag = isBoss ? "[BOSS]" : "";
            return $"[{id}] {name} {bossFlag} Lv.{level} HP:{hp} ATK:{attack} DEF:{defense}";
        }
    }
}
