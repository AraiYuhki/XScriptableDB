namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Type of batch operation.
    /// </summary>
    public enum BatchOperationType
    {
        /// <summary>Add</summary>
        Add,

        /// <summary>Update</summary>
        Update,

        /// <summary>Delete</summary>
        Delete,

        /// <summary>Upsert (update if exists, add if not)</summary>
        Upsert
    }

    /// <summary>
    /// Batch operation result.
    /// </summary>
    public enum BatchOperationResult
    {
        /// <summary>Success</summary>
        Success,

        /// <summary>Skipped</summary>
        Skipped,

        /// <summary>Failed</summary>
        Failed
    }

    /// <summary>
    /// Batch operation entry.
    /// </summary>
    /// <typeparam name="TRecord">Record type</typeparam>
    public class BatchOperationEntry<TRecord>
    {
        /// <summary>Type of operation</summary>
        public BatchOperationType Type { get; set; }

        /// <summary>Target record</summary>
        public TRecord Record { get; set; }

        /// <summary>Operation result</summary>
        public BatchOperationResult Result { get; set; } = BatchOperationResult.Success;

        /// <summary>Error message</summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Creates an add operation entry.
        /// </summary>
        public static BatchOperationEntry<TRecord> CreateAdd(TRecord record)
        {
            return new BatchOperationEntry<TRecord>
            {
                Type = BatchOperationType.Add,
                Record = record
            };
        }

        /// <summary>
        /// Creates an update operation entry.
        /// </summary>
        public static BatchOperationEntry<TRecord> CreateUpdate(TRecord record)
        {
            return new BatchOperationEntry<TRecord>
            {
                Type = BatchOperationType.Update,
                Record = record
            };
        }

        /// <summary>
        /// Creates a delete operation entry.
        /// </summary>
        public static BatchOperationEntry<TRecord> CreateDelete(TRecord record)
        {
            return new BatchOperationEntry<TRecord>
            {
                Type = BatchOperationType.Delete,
                Record = record
            };
        }

        /// <summary>
        /// Creates an upsert operation entry.
        /// </summary>
        public static BatchOperationEntry<TRecord> CreateUpsert(TRecord record)
        {
            return new BatchOperationEntry<TRecord>
            {
                Type = BatchOperationType.Upsert,
                Record = record
            };
        }
    }
}
