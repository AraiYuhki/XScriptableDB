using System;

namespace Xeon.XScriptableDB.Cache
{
    /// <summary>
    /// クエリキャッシュのキー。
    /// </summary>
    public readonly struct QueryCacheKey : IEquatable<QueryCacheKey>
    {
        public readonly Type TableType;
        public readonly string QueryType;
        public readonly object KeyValue;

        public QueryCacheKey(Type tableType, string queryType, object keyValue)
        {
            TableType = tableType;
            QueryType = queryType;
            KeyValue = keyValue;
        }

        public bool Equals(QueryCacheKey other)
        {
            return TableType == other.TableType &&
                   QueryType == other.QueryType &&
                   Equals(KeyValue, other.KeyValue);
        }

        public override bool Equals(object obj)
        {
            return obj is QueryCacheKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TableType, QueryType, KeyValue);
        }

        public override string ToString()
        {
            return $"{TableType.Name}.{QueryType}({KeyValue})";
        }
    }
}