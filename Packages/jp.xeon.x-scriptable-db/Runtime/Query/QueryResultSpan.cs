using System;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// QueryResultのSpanベースバージョン。より低レベルなアクセスを提供します。
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
        /// SpanからQueryResultSpanを作成します。
        /// </summary>
        /// <param name="span">ソースのSpan</param>
        public QueryResultSpan(ReadOnlySpan<T> span)
        {
            this.span = span;
        }

        /// <summary>
        /// 配列からQueryResultSpanを作成します。
        /// </summary>
        /// <param name="array">ソース配列</param>
        public QueryResultSpan(T[] array)
        {
            span = array.AsSpan();
        }

        /// <summary>
        /// 空のQueryResultSpanを作成します。
        /// </summary>
        public static QueryResultSpan<T> Empty => new(ReadOnlySpan<T>.Empty);

        /// <summary>
        /// 指定されたインデックスのレコードを返します。
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns>レコード</returns>
        public T this[int index] => span[index];

        /// <summary>
        /// Enumeratorを返します。
        /// </summary>
        public ReadOnlySpan<T>.Enumerator GetEnumerator() => span.GetEnumerator();

        /// <summary>
        /// 結果を配列にコピーします。
        /// </summary>
        /// <returns>レコードの配列</returns>
        public T[] ToArray() => span.ToArray();
    }
}