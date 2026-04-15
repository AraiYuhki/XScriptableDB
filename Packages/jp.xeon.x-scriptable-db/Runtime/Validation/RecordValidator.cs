using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// Record validator.
    /// </summary>
    public static class RecordValidator
    {
        private const BindingFlags MemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// Validates a single record.
        /// </summary>
        /// <typeparam name="T">Type of the record</typeparam>
        /// <param name="record">Record to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateRecord<T>(T record) where T : class
        {
            if (record == null)
            {
                return ValidationResult.Error("record", "Record is null.");
            }

            var result = new ValidationResult();
            var type = typeof(T);

            // Validate fields
            foreach (var field in type.GetFields(MemberFlags))
            {
                var value = field.GetValue(record);
                ValidateMember(field, value, field.Name, result);
            }

            // Validate properties
            foreach (var property in type.GetProperties(MemberFlags))
            {
                if (!property.CanRead) continue;
                var value = property.GetValue(record);
                ValidateMember(property, value, property.Name, result);
            }

            return result;
        }

        /// <summary>
        /// Validates a member.
        /// </summary>
        private static void ValidateMember(MemberInfo member, object value, string fieldName, ValidationResult result)
        {
            var attributes = member.GetCustomAttributes<ValidationAttribute>();

            foreach (var attr in attributes)
            {
                var validationResult = attr.Validate(value, fieldName);
                result.Merge(validationResult);
            }
        }

        /// <summary>
        /// Validates an entire table.
        /// </summary>
        /// <typeparam name="T">Type of the record</typeparam>
        /// <param name="tableAsset">Table to validate</param>
        /// <param name="keySelector">Key selector</param>
        /// <returns>Validation result</returns>
        public static TableValidationResult ValidateTable<T>(
            ITableAsset tableAsset,
            Func<T, object> keySelector = null) where T : class
        {
            var result = new TableValidationResult
            {
                TableName = tableAsset?.GetType().Name ?? "Unknown"
            };

            if (tableAsset == null)
            {
                result.TableLevelErrors.Add(new ValidationError("table", "Table is null."));
                return result;
            }

            var records = new List<T>();
            var index = 0;

            // Validate each record
            foreach (var record in tableAsset.Records)
            {
                if (record is T typedRecord)
                {
                    records.Add(typedRecord);

                    var recordResult = new RecordValidationResult
                    {
                        RecordIndex = index,
                        RecordKey = keySelector?.Invoke(typedRecord)
                    };

                    var validationResult = ValidateRecord(typedRecord);
                    recordResult.Merge(validationResult);

                    result.RecordResults.Add(recordResult);
                }
                index++;
            }

            // Table-level validation
            ValidateTableLevelConstraints(records, result, keySelector);

            return result;
        }

        /// <summary>
        /// Validates table-level constraints.
        /// </summary>
        private static void ValidateTableLevelConstraints<T>(
            List<T> records,
            TableValidationResult result,
            Func<T, object> keySelector) where T : class
        {
            if (records.Count == 0) return;

            var type = typeof(T);

            // Check Unique constraints
            foreach (var field in type.GetFields(MemberFlags))
            {
                if (field.GetCustomAttribute<UniqueAttribute>() != null)
                {
                    ValidateUniqueness(records, field.Name, f => field.GetValue(f), result, keySelector);
                }
            }

            foreach (var property in type.GetProperties(MemberFlags))
            {
                if (!property.CanRead) continue;
                if (property.GetCustomAttribute<UniqueAttribute>() != null)
                {
                    ValidateUniqueness(records, property.Name, p => property.GetValue(p), result, keySelector);
                }
            }

            // Check Compare constraints
            ValidateCompareConstraints(records, result, keySelector);
        }

        /// <summary>
        /// Validates uniqueness.
        /// </summary>
        private static void ValidateUniqueness<T>(
            List<T> records,
            string fieldName,
            Func<T, object> valueSelector,
            TableValidationResult result,
            Func<T, object> keySelector) where T : class
        {
            var valueGroups = records
                .Select((r, i) => new { Record = r, Index = i, Value = valueSelector(r) })
                .Where(x => x.Value != null)
                .GroupBy(x => x.Value)
                .Where(g => g.Count() > 1);

            foreach (var group in valueGroups)
            {
                var duplicateKeys = group.Select(x => keySelector?.Invoke(x.Record) ?? x.Index).ToList();
                var error = new ValidationError(
                    fieldName,
                    $"The value '{group.Key}' of {fieldName} is duplicated. Keys: {string.Join(", ", duplicateKeys)}",
                    ValidationErrorType.Unique);
                result.TableLevelErrors.Add(error);
            }
        }

        /// <summary>
        /// Validates Compare constraints.
        /// </summary>
        private static void ValidateCompareConstraints<T>(
            List<T> records,
            TableValidationResult result,
            Func<T, object> keySelector) where T : class
        {
            var type = typeof(T);

            foreach (var field in type.GetFields(MemberFlags))
            {
                var compareAttr = field.GetCustomAttribute<CompareAttribute>();
                if (compareAttr != null)
                {
                    ValidateCompareField(records, field.Name, compareAttr, type, result, keySelector);
                }
            }
        }

        /// <summary>
        /// Validates comparison between fields.
        /// </summary>
        private static void ValidateCompareField<T>(
            List<T> records,
            string fieldName,
            CompareAttribute attr,
            Type type,
            TableValidationResult result,
            Func<T, object> keySelector) where T : class
        {
            var thisField = type.GetField(fieldName, MemberFlags);
            var otherField = type.GetField(attr.OtherField, MemberFlags);

            if (thisField == null || otherField == null) return;

            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                var thisValue = thisField.GetValue(record);
                var otherValue = otherField.GetValue(record);

                if (!CompareValues(thisValue, otherValue, attr.Operator))
                {
                    var recordKey = keySelector?.Invoke(record) ?? i;
                    result.RecordResults[i].AddError(
                        fieldName,
                        attr.ErrorMessage ?? GetCompareErrorMessage(fieldName, attr.OtherField, attr.Operator),
                        ValidationErrorType.Compare);
                }
            }
        }

        /// <summary>
        /// Compares two values.
        /// </summary>
        private static bool CompareValues(object left, object right, CompareOperator op)
        {
            if (left == null || right == null)
            {
                return op switch
                {
                    CompareOperator.Equal => left == null && right == null,
                    CompareOperator.NotEqual => left != right,
                    _ => false
                };
            }

            if (left is IComparable comparable)
            {
                return CompareValues(comparable, right, op);
            }

            return op == CompareOperator.Equal ? left.Equals(right) : !left.Equals(right);
        }

        private static bool CompareValues(IComparable left, object right, CompareOperator op)
        {
            try
            {
                var comparison = left.CompareTo(right);
                return op switch
                {
                    CompareOperator.Equal => comparison == 0,
                    CompareOperator.NotEqual => comparison != 0,
                    CompareOperator.LessThan => comparison < 0,
                    CompareOperator.LessThanOrEqual => comparison <= 0,
                    CompareOperator.GreaterThan => comparison > 0,
                    CompareOperator.GreaterThanOrEqual => comparison >= 0,
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the comparison error message.
        /// </summary>
        private static string GetCompareErrorMessage(string fieldName, string otherField, CompareOperator op)
        {
            var opStr = op switch
            {
                CompareOperator.Equal => "equal to",
                CompareOperator.NotEqual => "not equal to",
                CompareOperator.LessThan => "less than",
                CompareOperator.LessThanOrEqual => "less than or equal to",
                CompareOperator.GreaterThan => "greater than",
                CompareOperator.GreaterThanOrEqual => "greater than or equal to",
                _ => "?"
            };
            return $"{fieldName} must be {opStr} {otherField}.";
        }
    }
}
