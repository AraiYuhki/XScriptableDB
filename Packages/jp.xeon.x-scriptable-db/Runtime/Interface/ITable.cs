using System.Collections.Generic;

namespace Xeon.XScriptableDB
{
    public interface ITable<T>
    {
        IReadOnlyList<T> All { get; }
        int Count { get; }
    }

    public interface ILookupTable<T, TKey> : ITable<T>
        where T : IPrimaryKey<TKey>
    {
        T FindByPrimaryKey(TKey key);
        bool TryFindByPrimaryKey(TKey key, out T record);
    }
}
