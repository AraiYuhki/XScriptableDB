using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// Query extension methods for TableAsset.
    /// </summary>
    public static class TableQuery
    {
        /// <summary>
        /// Searches for records by SecondaryKey and returns them as a QueryResult.
        /// </summary>
        /// <typeparam name="T">Type of the record</typeparam>
        /// <typeparam name="TKey">Type of the PrimaryKey</typeparam>
        /// <typeparam name="TSecondaryKey">Type of the SecondaryKey</typeparam>
        /// <param name="table">Table to search</param>
        /// <param name="indexName">Index name</param>
        /// <param name="key">Key to search for</param>
        /// <returns>Search result</returns>
        public static QueryResult<T> QueryBySecondaryKey<T, TKey, TSecondaryKey>(
            this TableAsset<T, TKey> table,
            string indexName,
            TSecondaryKey key)
            where T : class, new()
            where TKey : IComparable<TKey>
        {
            var index = table.SecondaryIndices.GetIndex(indexName);
            if (index == null)
                return QueryResult<T>.Empty;

            var recordIndices = index.FindByKey(key);
            if (recordIndices.Length == 0)
                return QueryResult<T>.Empty;

            return new QueryResult<T>(table.RecordsInternal, recordIndices);
        }

        /// <summary>
        /// Searches for records matching the condition.
        /// </summary>
        /// <typeparam name="T">Type of the record</typeparam>
        /// <typeparam name="TKey">Type of the PrimaryKey</typeparam>
        /// <param name="table">Table to search</param>
        /// <param name="predicate">Condition</param>
        /// <returns>Enumeration of matching records</returns>
        public static IEnumerable<T> Where<T, TKey>(this TableAsset<T, TKey> table, Func<T, bool> predicate)
            where T : class, new()
            where TKey : IComparable<TKey>
        {
            foreach (var record in table.All)
            {
                if (predicate(record))
                    yield return record;
            }
        }

        /// <summary>
        /// Returns the first record matching the condition.
        /// </summary>
        /// <typeparam name="T">Type of the record</typeparam>
        /// <typeparam name="TKey">Type of the PrimaryKey</typeparam>
        /// <param name="table">Table to search</param>
        /// <param name="predicate">Condition</param>
        /// <returns>First matching record, or null if not found</returns>
        public static T FirstOrDefault<T, TKey>(this TableAsset<T, TKey> table, Func<T, bool> predicate)
            where T : class, new()
            where TKey : IComparable<TKey>
        {
            foreach (var record in table.All)
            {
                if (predicate(record))
                    return record;
            }
            return null;
        }

        /// <summary>
        /// Checks whether any record matching the condition exists.
        /// </summary>
        /// <typeparam name="T">Type of the record</typeparam>
        /// <typeparam name="TKey">Type of the PrimaryKey</typeparam>
        /// <param name="table">Table to search</param>
        /// <param name="predicate">Condition</param>
        /// <returns>True if a matching record exists</returns>
        public static bool Any<T, TKey>(this TableAsset<T, TKey> table, Func<T, bool> predicate)
            where T : class, new()
            where TKey : IComparable<TKey>
        {
            foreach (var record in table.All)
            {
                if (predicate(record))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether all records match the condition.
        /// </summary>
        /// <typeparam name="T">Type of the record</typeparam>
        /// <typeparam name="TKey">Type of the PrimaryKey</typeparam>
        /// <param name="table">Table to search</param>
        /// <param name="predicate">Condition</param>
        /// <returns>True if all records match</returns>
        public static bool All<T, TKey>(this TableAsset<T, TKey> table, Func<T, bool> predicate)
            where T : class, new()
            where TKey : IComparable<TKey>
        {
            foreach (var record in table.All)
            {
                if (!predicate(record))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Counts the number of records matching the condition.
        /// </summary>
        /// <typeparam name="T">Type of the record</typeparam>
        /// <typeparam name="TKey">Type of the PrimaryKey</typeparam>
        /// <param name="table">Table to search</param>
        /// <param name="predicate">Condition</param>
        /// <returns>Number of matching records</returns>
        public static int Count<T, TKey>(this TableAsset<T, TKey> table, Func<T, bool> predicate)
            where T : class, new()
            where TKey : IComparable<TKey>
        {
            var count = 0;
            foreach (var record in table.All)
            {
                if (predicate(record))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Projects each record to a new form and returns the results.
        /// </summary>
        /// <typeparam name="T">Type of the record</typeparam>
        /// <typeparam name="TKey">Type of the PrimaryKey</typeparam>
        /// <typeparam name="TResult">Type of the projected result</typeparam>
        /// <param name="table">Table to project from</param>
        /// <param name="selector">Projection function</param>
        /// <returns>Enumeration of projected values</returns>
        public static IEnumerable<TResult> Select<T, TKey, TResult>(this TableAsset<T, TKey> table, Func<T, TResult> selector)
            where T : class, new()
            where TKey : IComparable<TKey>
        {
            foreach (var record in table.All)
                yield return selector(record);
        }

        /// <summary>
        /// Skips the specified number of records.
        /// </summary>
        /// <typeparam name="T">Type of the record</typeparam>
        /// <typeparam name="TKey">Type of the PrimaryKey</typeparam>
        /// <param name="table">Table to skip from</param>
        /// <param name="count">Number of records to skip</param>
        /// <returns>Enumeration of records after skipping</returns>
        public static IEnumerable<T> Skip<T, TKey>(this TableAsset<T, TKey> table, int count)
            where T : class, new()
            where TKey : IComparable<TKey>
        {
            var skipped = 0;
            foreach (var record in table.All)
            {
                if (skipped < count)
                {
                    skipped++;
                    continue;
                }
                yield return record;
            }
        }

        /// <summary>
        /// Returns the specified number of records.
        /// </summary>
        /// <typeparam name="T">Type of the record</typeparam>
        /// <typeparam name="TKey">Type of the PrimaryKey</typeparam>
        /// <param name="table">Table to take from</param>
        /// <param name="count">Number of records to take</param>
        /// <returns>Enumeration of taken records</returns>
        public static IEnumerable<T> Take<T, TKey>(this TableAsset<T, TKey> table, int count)
            where T : class, new()
            where TKey : IComparable<TKey>
        {
            var taken = 0;
            foreach (var record in table.All)
            {
                if (taken >= count)
                    yield break;
                yield return record;
                taken++;
            }
        }
    }
}
