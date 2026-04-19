using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Xeon.XScriptableDB.Validation;

namespace Xeon.XScriptableDB.Samples.Validation
{
    /// <summary>
    /// バリデーションサンプルのロジック。
    /// GUIから呼び出されることを想定しています。
    /// </summary>
    public class ValidationSample : MonoBehaviour
    {
        [SerializeField]
        private SkillTable skillTable;

        [SerializeField]
        private WeaponTable weaponTable;

        /// <summary>
        /// スキルテーブルのバリデーションを実行します。
        /// </summary>
        /// <returns>バリデーション結果</returns>
        public TableValidationResult ValidateSkillTable()
        {
            if (skillTable == null)
            {
                Debug.LogError("SkillTable is not set");
                return null;
            }

            var result = RecordValidator.ValidateTable<SkillRecord>(
                skillTable, r => r.Id);

            LogValidationResult(result);
            return result;
        }

        /// <summary>
        /// 武器テーブルのバリデーション（外部キーのバリデーションを含む）を実行します。
        /// </summary>
        /// <returns>バリデーション結果</returns>
        public TableValidationResult ValidateWeaponTable()
        {
            if (weaponTable == null)
            {
                Debug.LogError("WeaponTable is not set");
                return null;
            }

            // レコードのバリデーション
            var result = RecordValidator.ValidateTable<WeaponRecord>(
                weaponTable, r => r.Id);

            LogValidationResult(result);

            // 外部キーのバリデーション
            if (skillTable != null)
            {
                var context = new ForeignKeyValidationContext();
                context.RegisterTable(typeof(SkillTable), skillTable);

                var fkResult = ForeignKeyValidator.ValidateForeignKeys<WeaponRecord>(
                    weaponTable, context, r => r.Id);

                LogValidationResult(fkResult);
            }

            return result;
        }

        /// <summary>
        /// すべてのテーブルに対して一括バリデーションを実行します。
        /// </summary>
        /// <returns>テーブル名とバリデーション結果のディクショナリ</returns>
        public Dictionary<string, TableValidationResult> ValidateAll()
        {
            var results = new Dictionary<string, TableValidationResult>();

            var skillResult = ValidateSkillTable();
            if (skillResult != null)
                results["SkillTable"] = skillResult;

            var weaponResult = ValidateWeaponTable();
            if (weaponResult != null)
                results["WeaponTable"] = weaponResult;

            // サマリーログ
            var totalErrors = results.Values.Sum(r => r.TotalErrorCount);

            Debug.Log("=== Batch Validation Complete ===");
            Debug.Log($"Tables: {results.Count}");
            Debug.Log($"Total errors: {totalErrors}");

            return results;
        }

        /// <summary>
        /// バリデーション結果をログに出力します。
        /// </summary>
        private void LogValidationResult(TableValidationResult result)
        {
            if (result.IsValid)
            {
                Debug.Log($"{result.GetSummary()}");
                return;
            }

            Debug.LogWarning(result.GetSummary());

            foreach (var error in result.GetAllErrors())
            {
                Debug.LogError($"[{result.TableName}] {error}");
            }
        }

        /// <summary>
        /// 現在のSkillTableのレコード数を取得します。
        /// </summary>
        public int SkillRecordCount => skillTable?.Count ?? 0;

        /// <summary>
        /// 現在のWeaponTableのレコード数を取得します。
        /// </summary>
        public int WeaponRecordCount => weaponTable?.Count ?? 0;
    }
}
