using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Samples.QueryResultZeroGC
{
    /// <summary>
    /// 敵データのレコードクラス。
    /// パフォーマンス計測用に大量データを想定。
    /// </summary>
    [Serializable]
    public class EnemyRecord
    {
        [SerializeField, CsvColumn("ID"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("名前")]
        private string name;

        [SerializeField, CsvColumn("HP")]
        private int hp;

        [SerializeField, CsvColumn("攻撃力")]
        private int attack;

        [SerializeField, CsvColumn("防御力")]
        private int defense;

        [SerializeField, CsvColumn("レベル"), SecondaryKey]
        private int level;

        [SerializeField, CsvColumn("エリアID"), SecondaryKey]
        private int areaId;

        [SerializeField, CsvColumn("ボス"), SecondaryKey]
        private bool isBoss;

        [SerializeField, CsvColumn("ドロップ率")]
        private float dropRate;

        /// <summary>
        /// 敵ID（主キー）
        /// </summary>
        public int Id => id;

        /// <summary>
        /// 敵名
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
