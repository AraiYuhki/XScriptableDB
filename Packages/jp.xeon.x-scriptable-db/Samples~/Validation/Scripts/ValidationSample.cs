using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Xeon.XScriptableDB.Validation;

namespace Xeon.XScriptableDB.Samples.Validation
{
    /// <summary>
    /// Logic for the Validation sample.
    /// Intended to be called from a GUI.
    /// </summary>
    public class ValidationSample : MonoBehaviour
    {
        [SerializeField]
        private SkillTable skillTable;

        [SerializeField]
        private WeaponTable weaponTable;

        /// <summary>
        /// Runs validation on the skill table.
        /// </summary>
        /// <returns>Validation result</returns>
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
        /// Runs validation on the weapon table (including foreign key validation).
        /// </summary>
        /// <returns>Validation result</returns>
        public TableValidationResult ValidateWeaponTable()
        {
            if (weaponTable == null)
            {
                Debug.LogError("WeaponTable is not set");
                return null;
            }

            // Record validation
            var result = RecordValidator.ValidateTable<WeaponRecord>(
                weaponTable, r => r.Id);

            LogValidationResult(result);

            // Foreign key validation
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
        /// Runs batch validation on all tables.
        /// </summary>
        /// <returns>Dictionary of table names and validation results</returns>
        public Dictionary<string, TableValidationResult> ValidateAll()
        {
            var results = new Dictionary<string, TableValidationResult>();

            var skillResult = ValidateSkillTable();
            if (skillResult != null)
                results["SkillTable"] = skillResult;

            var weaponResult = ValidateWeaponTable();
            if (weaponResult != null)
                results["WeaponTable"] = weaponResult;

            // Summary log
            var totalErrors = results.Values.Sum(r => r.TotalErrorCount);

            Debug.Log("=== Batch Validation Complete ===");
            Debug.Log($"Tables: {results.Count}");
            Debug.Log($"Total errors: {totalErrors}");

            return results;
        }

        /// <summary>
        /// Logs the validation result.
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
        /// Gets the number of records in the current SkillTable.
        /// </summary>
        public int SkillRecordCount => skillTable?.Count ?? 0;

        /// <summary>
        /// Gets the number of records in the current WeaponTable.
        /// </summary>
        public int WeaponRecordCount => weaponTable?.Count ?? 0;
    }
}
