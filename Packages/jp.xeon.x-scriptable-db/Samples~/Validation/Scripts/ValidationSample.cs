using System.Collections.Generic;
using UnityEngine;
using Xeon.XScriptableDB.Validation;

namespace Xeon.XScriptableDB.Samples.Validation
{
    /// <summary>
    /// Validationサンプルのロジック部分。
    /// GUIから呼び出されることを想定。
    /// </summary>
    public class ValidationSample : MonoBehaviour
    {
        [SerializeField]
        private SkillTable skillTable;

        [SerializeField]
        private WeaponTable weaponTable;

        /// <summary>
        /// スキルテーブルのバリデーションを実行する。
        /// </summary>
        /// <returns>バリデーション結果</returns>
        public ValidationResult ValidateSkillTable()
        {
            if (skillTable == null)
            {
                Debug.LogError("SkillTableが設定されていません");
                return null;
            }

            var validator = new TableValidator();
            var result = validator.Validate(skillTable);

            LogValidationResult("SkillTable", result);
            return result;
        }

        /// <summary>
        /// 武器テーブルのバリデーション（外部キー検証含む）を実行する。
        /// </summary>
        /// <returns>バリデーション結果</returns>
        public ValidationResult ValidateWeaponTable()
        {
            if (weaponTable == null)
            {
                Debug.LogError("WeaponTableが設定されていません");
                return null;
            }

            var validator = new TableValidator();

            // 外部キー検証用にSkillTableを登録
            if (skillTable != null)
                validator.RegisterForeignKeyTable(skillTable);

            var result = validator.Validate(weaponTable);

            LogValidationResult("WeaponTable", result);
            return result;
        }

        /// <summary>
        /// 全テーブルの一括バリデーションを実行する。
        /// </summary>
        /// <returns>テーブル名とバリデーション結果のディクショナリ</returns>
        public Dictionary<string, ValidationResult> ValidateAll()
        {
            var results = new Dictionary<string, ValidationResult>();

            var skillResult = ValidateSkillTable();
            if (skillResult != null)
                results["SkillTable"] = skillResult;

            var weaponResult = ValidateWeaponTable();
            if (weaponResult != null)
                results["WeaponTable"] = weaponResult;

            // サマリーログ
            var totalErrors = 0;
            var totalWarnings = 0;
            foreach (var kvp in results)
            {
                totalErrors += kvp.Value.ErrorCount;
                totalWarnings += kvp.Value.WarningCount;
            }

            Debug.Log($"=== 一括バリデーション完了 ===");
            Debug.Log($"テーブル数: {results.Count}");
            Debug.Log($"総エラー数: {totalErrors}");
            Debug.Log($"総警告数: {totalWarnings}");

            return results;
        }

        /// <summary>
        /// 特定のレコードのバリデーションエラーを取得する。
        /// </summary>
        /// <param name="recordId">レコードID</param>
        /// <returns>エラーリスト</returns>
        public List<ValidationError> GetSkillRecordErrors(int recordId)
        {
            var result = ValidateSkillTable();
            if (result == null)
                return new List<ValidationError>();

            return result.GetErrorsForRecord(recordId);
        }

        /// <summary>
        /// バリデーション結果をログ出力する。
        /// </summary>
        private void LogValidationResult(string tableName, ValidationResult result)
        {
            if (result.IsValid)
            {
                Debug.Log($"✓ {tableName}: バリデーション成功");
                return;
            }

            Debug.LogWarning($"⚠ {tableName}: {result.ErrorCount}件のエラー、{result.WarningCount}件の警告");

            foreach (var error in result.Errors)
            {
                var icon = error.Severity == ValidationSeverity.Error ? "❌" : "⚠️";
                Debug.LogError($"{icon} [{tableName}] ID={error.RecordId}, {error.FieldName}: {error.Message}");
            }

            foreach (var warning in result.Warnings)
            {
                Debug.LogWarning($"⚠️ [{tableName}] ID={warning.RecordId}, {warning.FieldName}: {warning.Message}");
            }
        }

        /// <summary>
        /// 現在のSkillTableのレコード数を取得する。
        /// </summary>
        public int SkillRecordCount => skillTable?.RecordCount ?? 0;

        /// <summary>
        /// 現在のWeaponTableのレコード数を取得する。
        /// </summary>
        public int WeaponRecordCount => weaponTable?.RecordCount ?? 0;
    }
}
