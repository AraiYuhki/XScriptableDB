using System.Collections.Generic;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// 外部キー参照のレポート。
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

        /// <summary>無効な参照値のリスト</summary>
        public List<object> InvalidReferences { get; } = new();

        /// <summary>エラーメッセージのリスト</summary>
        public List<string> Errors { get; } = new();

        /// <summary>レポートが有効かどうか</summary>
        public bool IsValid => InvalidReferences.Count == 0 && Errors.Count == 0;

        /// <summary>無効な参照の数</summary>
        public int InvalidReferenceCount => InvalidReferences.Count;

        /// <summary>
        /// サマリー文字列を返します。
        /// </summary>
        public string GetSummary()
        {
            if (IsValid)
            {
                return $"{SourceTableName}.{ForeignKeyField} -> {TargetTableName}: " +
                       $"すべての{TotalReferences}件の参照が有効です。";
            }

            return $"{SourceTableName}.{ForeignKeyField} -> {TargetTableName}: " +
                   $"{TotalReferences}件中{InvalidReferenceCount}件の無効な参照が見つかりました。";
        }
    }
}