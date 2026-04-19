using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// GCアロケーションなしのクエリ結果構造体。
    /// ヒープ割り当てを避けるためにref structを使用します。
    /// </summary>
    /// <typeparam name="T">レコードの型</typeparam>
    public ref struct QueryResult<T> where T : class
    {
        private readonly T[] source;
        private readonly int[] indices;
        private readonly int count;

        /// <summary>
        /// 結果のレコード数。
        /// </summary>
        public int Count => count;

        /// <summary>
        /// 結果が空かどうか。
        /// </summary>
        public bool IsEmpty => count == 0;

        /// <summary>
        /// 最初のレコードを返します。結果が空の場合はnullになります。
        /// </summary>
        public T First => count > 0 ? source[indices[0]] : null;

        /// <summary>
        /// ソース配列とインデックス配列からQueryResultを作成します。
        /// </summary>
        /// <param name="source">ソース配列</param>
        /// <param name="indices">インデックス配列</param>
        public QueryResult(T[] source, int[] indices)
        {
            this.source = source;
            this.indices = indices;
            count = indices?.Length ?? 0;
        }

        /// <summary>
        /// ソース配列全体からQueryResultを作成します。
        /// </summary>
        /// <param name="source">ソース配列</param>
        public QueryResult(T[] source)
        {
            this.source = source;
            indices = null;
            count = source?.Length ?? 0;
        }

        /// <summary>
        /// 空のQueryResultを作成します。
        /// </summary>
        public static QueryResult<T> Empty => new(Array.Empty<T>(), Array.Empty<int>());

        /// <summary>
        /// 指定されたインデックスのレコードを返します。
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns>レコード</returns>
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
        /// Enumeratorを返します。
        /// </summary>
        public Enumerator GetEnumerator() => new(this);

        /// <summary>
        /// 結果を配列にコピーします。
        /// </summary>
        /// <returns>レコードの配列</returns>
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
        /// 結果をリストにコピーします。
        /// </summary>
        /// <returns>レコードのリスト</returns>
        public List<T> ToList()
        {
            var result = new List<T>(count);
            for (var i = 0; i < count; i++)
                result.Add(this[i]);
            return result;
        }

        /// <summary>
        /// GCアロケーションなしのEnumerator。
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
