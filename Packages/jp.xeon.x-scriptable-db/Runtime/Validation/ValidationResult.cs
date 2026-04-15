using System.Collections.Generic;
using System.Linq;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// Type of validation error.
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
    /// Validation error.
    /// </summary>
    public class ValidationError
    {
        /// <summary>Field name</summary>
        public string FieldName { get; set; }

        /// <summary>Error message</summary>
        public string Message { get; set; }

        /// <summary>Error type</summary>
        public ValidationErrorType ErrorType { get; set; }

        /// <summary>Record key (when the error is associated with a specific record)</summary>
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
    /// Validation result.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>Whether validation succeeded</summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>Error list</summary>
        public List<ValidationError> Errors { get; } = new();

        /// <summary>Singleton representing a successful result</summary>
        public static ValidationResult Success { get; } = new();

        /// <summary>
        /// Creates an error result.
        /// </summary>
        public static ValidationResult Error(string fieldName, string message, ValidationErrorType errorType = ValidationErrorType.Custom)
        {
            var result = new ValidationResult();
            result.Errors.Add(new ValidationError(fieldName, message, errorType));
            return result;
        }

        /// <summary>
        /// Adds an error.
        /// </summary>
        public void AddError(string fieldName, string message, ValidationErrorType errorType = ValidationErrorType.Custom)
        {
            Errors.Add(new ValidationError(fieldName, message, errorType));
        }

        /// <summary>
        /// Adds an error.
        /// </summary>
        public void AddError(ValidationError error)
        {
            Errors.Add(error);
        }

        /// <summary>
        /// Merges another result into this one.
        /// </summary>
        public void Merge(ValidationResult other)
        {
            if (other != null && other.Errors.Count > 0)
            {
                Errors.AddRange(other.Errors);
            }
        }

        /// <summary>
        /// Returns all error messages joined into a single string.
        /// </summary>
        public string GetCombinedErrorMessage(string separator = "\n")
        {
            return string.Join(separator, Errors.Select(e => e.ToString()));
        }

        /// <summary>
        /// Returns all errors for the specified field.
        /// </summary>
        public IEnumerable<ValidationError> GetErrorsForField(string fieldName)
        {
            return Errors.Where(e => e.FieldName == fieldName);
        }

        /// <summary>
        /// Returns all errors of the specified type.
        /// </summary>
        public IEnumerable<ValidationError> GetErrorsByType(ValidationErrorType errorType)
        {
            return Errors.Where(e => e.ErrorType == errorType);
        }
    }

    /// <summary>
    /// Validation result for an entire table.
    /// </summary>
    public class TableValidationResult
    {
        /// <summary>Table name</summary>
        public string TableName { get; set; }

        /// <summary>Whether validation succeeded</summary>
        public bool IsValid => RecordResults.All(r => r.IsValid) && TableLevelErrors.Count == 0;

        /// <summary>Per-record validation results</summary>
        public List<RecordValidationResult> RecordResults { get; } = new();

        /// <summary>Table-level errors (e.g. uniqueness constraint violations)</summary>
        public List<ValidationError> TableLevelErrors { get; } = new();

        /// <summary>Number of records with errors</summary>
        public int ErrorRecordCount => RecordResults.Count(r => !r.IsValid);

        /// <summary>Total number of errors</summary>
        public int TotalErrorCount => RecordResults.Sum(r => r.Errors.Count) + TableLevelErrors.Count;

        /// <summary>
        /// Returns a summary string.
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
        /// Returns all errors.
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
    /// Validation result for a single record.
    /// </summary>
    public class RecordValidationResult
    {
        /// <summary>Record key</summary>
        public object RecordKey { get; set; }

        /// <summary>Record index</summary>
        public int RecordIndex { get; set; }

        /// <summary>Whether validation succeeded</summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>Error list</summary>
        public List<ValidationError> Errors { get; } = new();

        /// <summary>
        /// Adds an error.
        /// </summary>
        public void AddError(string fieldName, string message, ValidationErrorType errorType = ValidationErrorType.Custom)
        {
            Errors.Add(new ValidationError(fieldName, message, errorType)
            {
                RecordKey = RecordKey
            });
        }

        /// <summary>
        /// Merges a validation result into this record result.
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
