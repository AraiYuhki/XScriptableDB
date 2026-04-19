namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// カスタムレコードバリデーターのインターフェース。
    /// </summary>
    /// <typeparam name="T">レコードの型</typeparam>
    public interface IRecordValidator<T>
    {
        /// <summary>
        /// レコードを検証します。
        /// </summary>
        /// <param name="record">検証するレコード</param>
        /// <returns>検証結果</returns>
        ValidationResult Validate(T record);
    }
}