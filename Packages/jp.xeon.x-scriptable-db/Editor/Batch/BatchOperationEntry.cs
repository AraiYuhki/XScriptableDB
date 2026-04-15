namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Non-generic batch operation entry.
    /// </summary>
    public class BatchOperationEntry
    {
        /// <summary>Type of operation</summary>
        public BatchOperationType Type { get; set; }

        /// <summary>Target record</summary>
        public object Record { get; set; }

        /// <summary>Operation result</summary>
        public BatchOperationResult Result { get; set; } = BatchOperationResult.Success;

        /// <summary>Error message</summary>
        public string ErrorMessage { get; set; }
    }
}