using System;
using System.Collections;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// Non-generic interface for TableAsset.
    /// Used when operating on tables without knowing the concrete type, such as in Editor code.
    /// </summary>
    public interface ITableAsset
    {
        /// <summary>
        /// Returns all records.
        /// </summary>
        IEnumerable Records { get; }

        /// <summary>
        /// Number of records.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Type of the record.
        /// </summary>
        Type RecordType { get; }

        /// <summary>
        /// Type of the PrimaryKey.
        /// </summary>
        Type KeyType { get; }

#if UNITY_EDITOR
        /// <summary>
        /// Creates a new empty record.
        /// </summary>
        object CreateNewRecord();

        /// <summary>
        /// Adds a record.
        /// </summary>
        void AddRecordObject(object record);

        /// <summary>
        /// Removes the record at the specified index.
        /// </summary>
        void RemoveRecordAt(int index);

        /// <summary>
        /// Checks for duplicate PrimaryKeys.
        /// </summary>
        /// <returns>List of duplicate keys (as object type)</returns>
        IList FindDuplicateKeysAsObjects();
#endif
    }
}
