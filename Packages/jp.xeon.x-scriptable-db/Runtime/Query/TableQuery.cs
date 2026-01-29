using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// TableAssetに対するクエリ拡張メソッド。
    /// </summary>
    public static class TableQuery
    {
        /// <summary>
        /// SecondaryKeyでレコードを検索し、QueryResultとして返す。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <typeparam name="TKey">PrimaryKeyの型</typeparam>
        /// <typeparam name="TSecondaryKey">SecondaryKeyの型</typeparam>
        /// <param name="table">検索対象のテーブル</param>
        /// <param name="indexName">インデックス名</param>
        /// <param name="key">検索するキー</param>
        /// <returns>検索結果</returns>
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
        /// 条件に一致するレコードを検索する。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <typeparam name="TKey">PrimaryKeyの型</typeparam>
        /// <param name="table">検索対象のテーブル</param>
        /// <param name="predicate">条件</param>
        /// <returns>条件に一致するレコードの列挙</returns>
        public static IEnumerable<T> Where<T, TKey>(
            this TableAsset<T, TKey> table,
            Func<T, bool> predicate)
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
        /// 条件に一致する最初のレコードを検索する。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <typeparam name="TKey">PrimaryKeyの型</typeparam>
        /// <param name="table">検索対象のテーブル</param>
        /// <param name="predicate">条件</param>
        /// <returns>条件に一致する最初のレコード、見つからない場合はnull</returns>
        public static T FirstOrDefault<T, TKey>(
            this TableAsset<T, TKey> table,
            Func<T, bool> predicate)
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
        /// 条件に一致するレコードが存在するかどうかを確認する。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <typeparam name="TKey">PrimaryKeyの型</typeparam>
        /// <param name="table">検索対象のテーブル</param>
        /// <param name="predicate">条件</param>
        /// <returns>存在する場合はtrue</returns>
        public static bool Any<T, TKey>(
            this TableAsset<T, TKey> table,
            Func<T, bool> predicate)
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
        /// 全レコードが条件に一致するかどうかを確認する。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <typeparam name="TKey">PrimaryKeyの型</typeparam>
        /// <param name="table">検索対象のテーブル</param>
        /// <param name="predicate">条件</param>
        /// <returns>全て一致する場合はtrue</returns>
        public static bool All<T, TKey>(
            this TableAsset<T, TKey> table,
            Func<T, bool> predicate)
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
        /// 条件に一致するレコードの数を数える。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <typeparam name="TKey">PrimaryKeyの型</typeparam>
        /// <param name="table">検索対象のテーブル</param>
        /// <param name="predicate">条件</param>
        /// <returns>一致するレコードの数</returns>
        public static int Count<T, TKey>(
            this TableAsset<T, TKey> table,
            Func<T, bool> predicate)
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
        /// レコードを変換して返す。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <typeparam name="TKey">PrimaryKeyの型</typeparam>
        /// <typeparam name="TResult">変換後の型</typeparam>
        /// <param name="table">検索対象のテーブル</param>
        /// <param name="selector">変換関数</param>
        /// <returns>変換後の値の列挙</returns>
        public static IEnumerable<TResult> Select<T, TKey, TResult>(
            this TableAsset<T, TKey> table,
            Func<T, TResult> selector)
            where T : class, new()
            where TKey : IComparable<TKey>
        {
            foreach (var record in table.All)
                yield return selector(record);
        }

        /// <summary>
        /// 指定した数のレコードをスキップする。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <typeparam name="TKey">PrimaryKeyの型</typeparam>
        /// <param name="table">検索対象のテーブル</param>
        /// <param name="count">スキップする数</param>
        /// <returns>スキップ後のレコードの列挙</returns>
        public static IEnumerable<T> Skip<T, TKey>(
            this TableAsset<T, TKey> table,
            int count)
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
        /// 指定した数のレコードを取得する。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <typeparam name="TKey">PrimaryKeyの型</typeparam>
        /// <param name="table">検索対象のテーブル</param>
        /// <param name="count">取得する数</param>
        /// <returns>取得したレコードの列挙</returns>
        public static IEnumerable<T> Take<T, TKey>(
            this TableAsset<T, TKey> table,
            int count)
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
