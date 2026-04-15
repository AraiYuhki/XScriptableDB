namespace Xeon.XScriptableDB
{
    /// <summary>
    /// Base interface for records managed by TableAsset.
    /// When combined with the [PrimaryKey] attribute, automatic key management becomes available.
    /// </summary>
    public interface IRecord
    {
        /// <summary>
        /// Returns the PrimaryKey value of the record as an object.
        /// Implement this when fast access without reflection is required.
        /// If not implemented, the [PrimaryKey] attribute is used to access the value via reflection.
        /// </summary>
        object GetPrimaryKeyValue() => null;
    }

    /// <summary>
    /// Record interface that provides type-safe PrimaryKey access.
    /// Equivalent to IPrimaryKey&lt;TKey&gt; but inherits from IRecord.
    /// </summary>
    /// <typeparam name="TKey">Type of the PrimaryKey</typeparam>
    public interface IRecord<TKey> : IRecord
    {
        /// <summary>
        /// PrimaryKey value of the record.
        /// </summary>
        TKey PrimaryKey { get; }

        object IRecord.GetPrimaryKeyValue() => PrimaryKey;
    }
}
