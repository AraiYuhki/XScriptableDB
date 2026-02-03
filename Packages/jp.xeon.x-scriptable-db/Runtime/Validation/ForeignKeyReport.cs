using System.Collections.Generic;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// 外部キー参照レポート。
    /// </summary>
    public class ForeignKeyReport
    {
        /// <summary>ソーステーブル名</summary>
        public string SourceTableName { get; set; }

        /// <summary>ターゲットテーブル名</summary>
        public string TargetTableName { get; set; }

        /// <summary>外部キーフィールド名</summary>
        public string ForeignKeyField { get; set; }

        /// <summary>総参照数</summary>
        public int TotalReferences { get; set; }

        /// <summary>無効な参照値リスト</summary>
        public List<object> InvalidReferences { get; } = new();

        /// <summary>エラーメッセージリスト</summary>
        public List<string> Errors { get; } = new();

        /// <summary>有効かどうか</summary>
        public bool IsValid => InvalidReferences.Count == 0 && Errors.Count == 0;

        /// <summary>無効な参照数</summary>
        public int InvalidReferenceCount => InvalidReferences.Count;

        /// <summary>
        /// サマリーを取得する。
        /// </summary>
        public string GetSummary()
        {
            if (IsValid)
            {
                return $"{SourceTableName}.{ForeignKeyField} -> {TargetTableName}: " +
                       $"全 {TotalReferences} 件の参照が有効です。";
            }

            return $"{SourceTableName}.{ForeignKeyField} -> {TargetTableName}: " +
                   $"{InvalidReferenceCount}/{TotalReferences} 件の無効な参照があります。";
        }
    }
}