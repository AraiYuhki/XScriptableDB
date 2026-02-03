using System;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// QueryResultのSpan版。より低レベルなアクセスを提供する。
    /// </summary>
    /// <typeparam name="T">レコードの型</typeparam>
    public readonly ref struct QueryResultSpan<T> where T : class
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