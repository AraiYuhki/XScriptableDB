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

        /// <summary>エラータイプ</summary>
        public ValidationErrorType ErrorType { get; set; }

        /// <summary>レコードキー（エラーが特定のレコードに関連付けられている場合）</summary>
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
    /// 検証結果。
    /// </summary>
    public class ValidationResult
    {
        /// <summary>検証が成功したかどうか</summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>エラーリスト</summary>
        public List<ValidationError> Errors { get; } = new();

        /// <summary>成功した結果を表すシングルトン</summary>
        public static ValidationResult Success { get; } = new();

        /// <summary>
        /// エラー結果を作成します。
        /// </summary>
        public static ValidationResult Error(string fieldName, string message, ValidationErrorType errorType = ValidationErrorType.Custom)
        {
            var result = new ValidationResult();
            result.Errors.Add(new ValidationError(fieldName, message, errorType));
            return result;
        }

        /// <summary>
        /// エラーを追加します。
        /// </summary>
        public void AddError(string fieldName, string message, ValidationErrorType errorType = ValidationErrorType.Custom)
        {
            Errors.Add(new ValidationError(fieldName, message, errorType));
        }

        /// <summary>
        /// エラーを追加します。
        /// </summary>
        public void AddError(ValidationError error)
        {
            Errors.Add(error);
        }

        /// <summary>
        /// 別の結果をマージします。
        /// </summary>
        public void Merge(ValidationResult other)
        {
            if (other != null && other.Errors.Count > 0)
            {
                Errors.AddRange(other.Errors);
            }
        }

        /// <summary>
        /// すべてのエラーメッセージを1つの文字列に結合して返します。
        /// </summary>
        public string GetCombinedErrorMessage(string separator = "\n")
        {
            return string.Join(separator, Errors.Select(e => e.ToString()));
        }

        /// <summary>
        /// 指定されたフィールドのすべてのエラーを返します。
        /// </summary>
        public IEnumerable<ValidationError> GetErrorsForField(string fieldName)
        {
            return Errors.Where(e => e.FieldName == fieldName);
        }

        /// <summary>
        /// 指定されたタイプのすべてのエラーを返します。
        /// </summary>
        public IEnumerable<ValidationError> GetErrorsByType(ValidationErrorType errorType)
        {
            return Errors.Where(e => e.ErrorType == errorType);
        }
    }

    /// <summary>
    /// テーブル全体の検証結果。
    /// </summary>
    public class TableValidationResult
    {
        /// <summary>テーブル名</summary>
        public string TableName { get; set; }

        /// <summary>検証が成功したかどうか</summary>
        public bool IsValid => RecordResults.All(r => r.IsValid) && TableLevelErrors.Count == 0;

        /// <summary>レコードごとの検証結果</summary>
        public List<RecordValidationResult> RecordResults { get; } = new();

        /// <summary>テーブルレベルのエラー（例：一意性制約違反）</summary>
        public List<ValidationError> TableLevelErrors { get; } = new();

        /// <summary>エラーのあるレコード数</summary>
        public int ErrorRecordCount => RecordResults.Count(r => !r.IsValid);

        /// <summary>総エラー数</summary>
        public int TotalErrorCount => RecordResults.Sum(r => r.Errors.Count) + TableLevelErrors.Count;

        /// <summary>
        /// サマリー文字列を返します。
        /// </summary>
        public string GetSummary()
        {
            if (IsValid)
            {
                return $"{TableName}: 検証に成功しました";
            }

            return $"{TableName}: エラーがあるレコード {ErrorRecordCount} 件、合計 {TotalErrorCount} 件のエラー";
        }

        /// <summary>
        /// すべてのエラーを返します。
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
    /// 単一レコードの検証結果。
    /// </summary>
    public class RecordValidationResult
    {
        /// <summary>レコードキー</summary>
        public object RecordKey { get; set; }

        /// <summary>レコードインデックス</summary>
        public int RecordIndex { get; set; }

        /// <summary>検証が成功したかどうか</summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>エラーリスト</summary>
        public List<ValidationError> Errors { get; } = new();

        /// <summary>
        /// エラーを追加します。
        /// </summary>
        public void AddError(string fieldName, string message, ValidationErrorType errorType = ValidationErrorType.Custom)
        {
            Errors.Add(new ValidationError(fieldName, message, errorType)
            {
                RecordKey = RecordKey
            });
        }

        /// <summary>
        /// 検証結果をこのレコード結果にマージします。
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
