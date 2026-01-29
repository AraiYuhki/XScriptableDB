using System;
using System.Collections;
using System.Collections.Generic;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// GC Alloc 0のクエリ結果構造体。
    /// ref structによりヒープアロケーションを回避する。
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
        /// 最初のレコードを取得する。結果が空の場合はnull。
        /// </summary>
        public T First => count > 0 ? source[indices[0]] : null;

        /// <summary>
        /// 配列とインデックス配列から QueryResult を作成する。
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
        /// 配列全体から QueryResult を作成する。
        /// </summary>
        /// <param name="source">ソース配列</param>
        public QueryResult(T[] source)
        {
            this.source = source;
            indices = null;
            count = source?.Length ?? 0;
        }

        /// <summary>
        /// 空の QueryResult を作成する。
        /// </summary>
        public static QueryResult<T> Empty => new(Array.Empty<T>(), Array.Empty<int>());

        /// <summary>
        /// 指定したインデックスのレコードを取得する。
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
        /// Enumerator を取得する。
        /// </summary>
        public Enumerator GetEnumerator() => new(this);

        /// <summary>
        /// 結果を配列にコピーする。
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
        /// 結果をリストにコピーする。
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
        /// GC Alloc 0 の Enumerator。
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

    /// <summary>
    /// QueryResultのSpan版。より低レベルなアクセスを提供する。
    /// </summary>
    /// <typeparam name="T">レコードの型</typeparam>
    public ref struct QueryResultSpan<T> where T : class
    {
        private readonly ReadOnlySpan<T> span;

        /// <summary>
        /// 結果のレコード数。
        /// </summary>
        public int Length => span.Length;

        /// <summary>
        /// 結果が空かどうか。
        /// </summary>
        public bool IsEmpty => span.IsEmpty;

        /// <summary>
        /// Spanから QueryResultSpan を作成する。
        /// </summary>
        /// <param name="span">ソースSpan</param>
        public QueryResultSpan(ReadOnlySpan<T> span)
        {
            this.span = span;
        }

        /// <summary>
        /// 配列から QueryResultSpan を作成する。
        /// </summary>
        /// <param name="array">ソース配列</param>
        public QueryResultSpan(T[] array)
        {
            span = array.AsSpan();
        }

        /// <summary>
        /// 空の QueryResultSpan を作成する。
        /// </summary>
        public static QueryResultSpan<T> Empty => new(ReadOnlySpan<T>.Empty);

        /// <summary>
        /// 指定したインデックスのレコードを取得する。
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns>レコード</returns>
        public T this[int index] => span[index];

        /// <summary>
        /// Enumerator を取得する。
        /// </summary>
        public ReadOnlySpan<T>.Enumerator GetEnumerator() => span.GetEnumerator();

        /// <summary>
        /// 結果を配列にコピーする。
        /// </summary>
        /// <returns>レコードの配列</returns>
        public T[] ToArray() => span.ToArray();
    }
}
