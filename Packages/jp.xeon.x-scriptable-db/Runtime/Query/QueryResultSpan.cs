using System;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// Span-based version of QueryResult. Provides lower-level access.
    /// </summary>
    /// <typeparam name="T">Type of the record</typeparam>
    public readonly ref struct QueryResultSpan<T> where T : class
    {
        private readonly ReadOnlySpan<T> span;

        /// <summary>
        /// Number of records in the result.
        /// </summary>
        public int Length => span.Length;

        /// <summary>
        /// Whether the result is empty.
        /// </summary>
        public bool IsEmpty => span.IsEmpty;

        /// <summary>
        /// Creates a QueryResultSpan from a Span.
        /// </summary>
        /// <param name="span">Source Span</param>
        public QueryResultSpan(ReadOnlySpan<T> span)
        {
            this.span = span;
        }

        /// <summary>
        /// Creates a QueryResultSpan from an array.
        /// </summary>
        /// <param name="array">Source array</param>
        public QueryResultSpan(T[] array)
        {
            span = array.AsSpan();
        }

        /// <summary>
        /// Creates an empty QueryResultSpan.
        /// </summary>
        public static QueryResultSpan<T> Empty => new(ReadOnlySpan<T>.Empty);

        /// <summary>
        /// Returns the record at the specified index.
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Record</returns>
        public T this[int index] => span[index];

        /// <summary>
        /// Returns an Enumerator.
        /// </summary>
        public ReadOnlySpan<T>.Enumerator GetEnumerator() => span.GetEnumerator();

        /// <summary>
        /// Copies the result to an array.
        /// </summary>
        /// <returns>Array of records</returns>
        public T[] ToArray() => span.ToArray();
    }
}