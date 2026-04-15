namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// Interface for custom record validators.
    /// </summary>
    /// <typeparam name="T">Type of the record</typeparam>
    public interface IRecordValidator<T>
    {
        /// <summary>
        /// Validates a record.
        /// </summary>
        /// <param name="record">Record to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult Validate(T record);
    }
}