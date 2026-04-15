using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// Zero-GC-allocation query result struct.
    /// Uses ref struct to avoid heap allocation.
    /// </summary>
    /// <typeparam name="T">Type of the record</typeparam>
    public ref struct QueryResult<T> where T : class
    {
        private readonly T[] source;
        private readonly int[] indices;
        private readonly int count;

        /// <summary>
        /// Number of records in the result.
        /// </summary>
        public int Count => count;

        /// <summary>
        /// Whether the result is empty.
        /// </summary>
        public bool IsEmpty => count == 0;

        /// <summary>
        /// Returns the first record. Null if the result is empty.
        /// </summary>
        public T First => count > 0 ? source[indices[0]] : null;

        /// <summary>
        /// Creates a QueryResult from a source array and an index array.
        /// </summary>
        /// <param name="source">Source array</param>
        /// <param name="indices">Index array</param>
        public QueryResult(T[] source, int[] indices)
        {
            this.source = source;
            this.indices = indices;
            count = indices?.Length ?? 0;
        }

        /// <summary>
        /// Creates a QueryResult from an entire source array.
        /// </summary>
        /// <param name="source">Source array</param>
        public QueryResult(T[] source)
        {
            this.source = source;
            indices = null;
            count = source?.Length ?? 0;
        }

        /// <summary>
        /// Creates an empty QueryResult.
        /// </summary>
        public static QueryResult<T> Empty => new(Array.Empty<T>(), Array.Empty<int>());

        /// <summary>
        /// Returns the record at the specified index.
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Record</returns>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= count)
                    throw new IndexOutOfRangeException();

                if (indices != null)
                    return source[indices[index]];
                return source[index];
            }
        }

        /// <summary>
        /// Returns an Enumerator.
        /// </summary>
        public Enumerator GetEnumerator() => new(this);

        /// <summary>
        /// Copies the result to an array.
        /// </summary>
        /// <returns>Array of records</returns>
        public T[] ToArray()
        {
            if (count == 0)
                return Array.Empty<T>();

            var result = new T[count];
            for (var i = 0; i < count; i++)
                result[i] = this[i];
            return result;
        }

        /// <summary>
        /// Copies the result to a list.
        /// </summary>
        /// <returns>List of records</returns>
        public List<T> ToList()
        {
            var result = new List<T>(count);
            for (var i = 0; i < count; i++)
                result.Add(this[i]);
            return result;
        }

        /// <summary>
        /// Zero-GC-allocation Enumerator.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly QueryResult<T> result;
            private int index;

            public Enumerator(QueryResult<T> result)
            {
                this.result = result;
                index = -1;
            }

            public T Current => result[index];

            public bool MoveNext()
            {
                index++;
                return index < result.Count;
            }
        }
    }
}
