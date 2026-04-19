using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// レコードバリデーター。
    /// </summary>
    public static class RecordValidator
    {
        private const BindingFlags MemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// 単一レコードを検証します。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <param name="record">検証するレコード</param>
        /// <returns>検証結果</returns>
        public static ValidationResult ValidateRecord<T>(T record) where T : class
        {
            if (record == null)
            {
                return ValidationResult.Error("record", "レコードがnullです。");
            }

            var result = new ValidationResult();
            var type = typeof(T);

            // フィールドの検証
            foreach (var field in type.GetFields(MemberFlags))
            {
                var value = field.GetValue(record);
                ValidateMember(field, value, field.Name, result);
            }

            // プロパティの検証
            foreach (var property in type.GetProperties(MemberFlags))
            {
                if (!property.CanRead) continue;
                var value = property.GetValue(record);
                ValidateMember(property, value, property.Name, result);
            }

            return result;
        }

        /// <summary>
        /// メンバーを検証します。
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
        /// テーブル全体を検証します。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <param name="tableAsset">検証するテーブル</param>
        /// <param name="keySelector">キーセレクター</param>
        /// <returns>検証結果</returns>
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
                result.TableLevelErrors.Add(new ValidationError("table", "テーブルがnullです。"));
                return result;
            }

            var records = new List<T>();
            var index = 0;

            // 各レコードの検証
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

            // テーブルレベルの検証
            ValidateTableLevelConstraints(records, result, keySelector);

            return result;
        }

        /// <summary>
        /// テーブルレベルの制約を検証します。
        /// </summary>
        private static void ValidateTableLevelConstraints<T>(
            List<T> records,
            TableValidationResult result,
            Func<T, object> keySelector) where T : class
        {
            if (records.Count == 0) return;

            var type = typeof(T);

            // Unique制約のチェック
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

            // Compare制約のチェック
            ValidateCompareConstraints(records, result, keySelector);
        }

        /// <summary>
        /// 一意性を検証します。
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
                    $"{fieldName} の値 '{group.Key}' が重複しています。キー: {string.Join(", ", duplicateKeys)}",
                    ValidationErrorType.Unique);
                result.TableLevelErrors.Add(error);
            }
        }

        /// <summary>
        /// Compare制約を検証します。
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
        /// フィールド間の比較を検証します。
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
        /// 2つの値を比較します。
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
        /// 比較エラーメッセージを返します。
        /// </summary>
        private static string GetCompareErrorMessage(string fieldName, string otherField, CompareOperator op)
        {
            var opStr = op switch
            {
                CompareOperator.Equal => "と等しい",
                CompareOperator.NotEqual => "と等しくない",
                CompareOperator.LessThan => "より小さい",
                CompareOperator.LessThanOrEqual => "以下",
                CompareOperator.GreaterThan => "より大きい",
                CompareOperator.GreaterThanOrEqual => "以上",
                _ => "?"
            };
            return $"{fieldName} は {otherField} {opStr}必要があります。";
        }
    }
}
