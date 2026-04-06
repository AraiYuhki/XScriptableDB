using System.Collections.Generic;
using System.Linq;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// バリデーションエラーの種類。
    /// </summary>
    public enum ValidationErrorType
    {
        Required,
        Range,
        StringLength,
        Pattern,
        Unique,
        ForeignKey,
        Compare,
        Custom
    }

    /// <summary>
    /// バリデーションエラー。
    /// </summary>
    public class ValidationError
    {
        /// <summary>フィールド名</summary>
        public string FieldName { get; set; }

        /// <summary>エラーメッセージ</summary>
        public string Message { get; set; }

        /// <summary>エラーの種類</summary>
        public ValidationErrorType ErrorType { get; set; }

        /// <summary>レコードのキー（特定のレコードに関連する場合）</summary>
        public object RecordKey { get; set; }

        public ValidationError(string fieldName, string message, ValidationErrorType errorType = ValidationErrorType.Custom)
        {
            FieldName = fieldName;
            Message = message;
            ErrorType = errorType;
        }

        public override string ToString()
        {
            var keyInfo = RecordKey != null ? $"[Key={RecordKey}] " : "";
            return $"{keyInfo}{FieldName}: {Message}";
        }
    }

    /// <summary>
    /// バリデーション結果。
    /// </summary>
    public class ValidationResult
    {
        /// <summary>バリデーションが成功したか</summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>エラーリスト</summary>
        public List<ValidationError> Errors { get; } = new();

        /// <summary>成功結果のシングルトン</summary>
        public static ValidationResult Success { get; } = new();

        /// <summary>
        /// エラー結果を作成する。
        /// </summary>
        public static ValidationResult Error(string fieldName, string message, ValidationErrorType errorType = ValidationErrorType.Custom)
        {
            var result = new ValidationResult();
            result.Errors.Add(new ValidationError(fieldName, message, errorType));
            return result;
        }

        /// <summary>
        /// エラーを追加する。
        /// </summary>
        public void AddError(string fieldName, string message, ValidationErrorType errorType = ValidationErrorType.Custom)
        {
            Errors.Add(new ValidationError(fieldName, message, errorType));
        }

        /// <summary>
        /// エラーを追加する。
        /// </summary>
        public void AddError(ValidationError error)
        {
            Errors.Add(error);
        }

        /// <summary>
        /// 他の結果をマージする。
        /// </summary>
        public void Merge(ValidationResult other)
        {
            if (other != null && other.Errors.Count > 0)
            {
                Errors.AddRange(other.Errors);
            }
        }

        /// <summary>
        /// エラーメッセージを結合して取得する。
        /// </summary>
        public string GetCombinedErrorMessage(string separator = "\n")
        {
            return string.Join(separator, Errors.Select(e => e.ToString()));
        }

        /// <summary>
        /// 指定フィールドのエラーを取得する。
        /// </summary>
        public IEnumerable<ValidationError> GetErrorsForField(string fieldName)
        {
            return Errors.Where(e => e.FieldName == fieldName);
        }

        /// <summary>
        /// 指定種類のエラーを取得する。
        /// </summary>
        public IEnumerable<ValidationError> GetErrorsByType(ValidationErrorType errorType)
        {
            return Errors.Where(e => e.ErrorType == errorType);
        }
    }

    /// <summary>
    /// テーブル全体のバリデーション結果。
    /// </summary>
    public class TableValidationResult
    {
        /// <summary>テーブル名</summary>
        public string TableName { get; set; }

        /// <summary>バリデーションが成功したか</summary>
        public bool IsValid => RecordResults.All(r => r.IsValid) && TableLevelErrors.Count == 0;

        /// <summary>レコード単位の結果</summary>
        public List<RecordValidationResult> RecordResults { get; } = new();

        /// <summary>テーブルレベルのエラー（一意性制約違反など）</summary>
        public List<ValidationError> TableLevelErrors { get; } = new();

        /// <summary>エラーがあるレコード数</summary>
        public int ErrorRecordCount => RecordResults.Count(r => !r.IsValid);

        /// <summary>総エラー数</summary>
        public int TotalErrorCount => RecordResults.Sum(r => r.Errors.Count) + TableLevelErrors.Count;

        /// <summary>
        /// サマリーを取得する。
        /// </summary>
        public string GetSummary()
        {
            if (IsValid)
            {
                return $"{TableName}: Validation succeeded";
            }

            return $"{TableName}: {ErrorRecordCount} record(s) with errors, {TotalErrorCount} error(s) in total";
        }

        /// <summary>
        /// 全エラーを取得する。
        /// </summary>
        public IEnumerable<ValidationError> GetAllErrors()
        {
            foreach (var error in TableLevelErrors)
            {
                yield return error;
            }

            foreach (var recordResult in RecordResults)
            {
                foreach (var error in recordResult.Errors)
                {
                    yield return error;
                }
            }
        }
    }

    /// <summary>
    /// レコード単位のバリデーション結果。
    /// </summary>
    public class RecordValidationResult
    {
        /// <summary>レコードのキー</summary>
        public object RecordKey { get; set; }

        /// <summary>レコードのインデックス</summary>
        public int RecordIndex { get; set; }

        /// <summary>バリデーションが成功したか</summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>エラーリスト</summary>
        public List<ValidationError> Errors { get; } = new();

        /// <summary>
        /// エラーを追加する。
        /// </summary>
        public void AddError(string fieldName, string message, ValidationErrorType errorType = ValidationErrorType.Custom)
        {
            Errors.Add(new ValidationError(fieldName, message, errorType)
            {
                RecordKey = RecordKey
            });
        }

        /// <summary>
        /// バリデーション結果をマージする。
        /// </summary>
        public void Merge(ValidationResult result)
        {
            if (result == null || result.IsValid) return;

            foreach (var error in result.Errors)
            {
                error.RecordKey = RecordKey;
                Errors.Add(error);
            }
        }
    }
}
