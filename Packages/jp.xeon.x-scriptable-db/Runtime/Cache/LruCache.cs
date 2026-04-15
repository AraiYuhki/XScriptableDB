using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Cache
{
    /// <summary>
    /// LRU (Least Recently Used) cache.
    /// Evicts the least recently used item when the capacity is exceeded.
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TValue">Value type</typeparam>
    public class LruCache<TKey, TValue>
    {
        private readonly int capacity;
        private readonly Dictionary<TKey, LinkedListNode<CacheItem>> cache;
        private readonly LinkedList<CacheItem> lruList;
        private readonly object syncLock = new();

        /// <summary>Maximum capacity of the cache</summary>
        public int Capacity => capacity;

        /// <summary>Current number of items</summary>
        public int Count
        {
            get
            {
                lock (syncLock)
                {
                    return cache.Count;
                }
            }
        }

        /// <summary>Number of cache hits</summary>
        public long HitCount { get; private set; }

        /// <summary>Number of cache misses</summary>
        public long MissCount { get; private set; }

        /// <summary>Hit rate</summary>
        public double HitRate
        {
            get
            {
                var total = HitCount + MissCount;
                return total > 0 ? (double)HitCount / total : 0;
            }
        }

        public LruCache(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");

            this.capacity = capacity;
            cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
            lruList = new LinkedList<CacheItem>();
        }

        /// <summary>
        /// Gets a value from the cache.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value (if found)</param>
        /// <returns>True if the key exists</returns>
        public bool TryGet(TKey key, out TValue value)
        {
            lock (syncLock)
            {
                if (cache.TryGetValue(key, out var node))
                {
                    // Move to the front of the list since it was accessed
                    lruList.Remove(node);
                    lruList.AddFirst(node);
                    value = node.Value.Value;
                    HitCount++;
                    return true;
                }

                value = default;
                MissCount++;
                return false;
            }
        }

        /// <summary>
        /// Gets a value. Throws an exception if the key does not exist.
        /// </summary>
        public TValue Get(TKey key)
        {
            if (TryGet(key, out var value))
                return value;

            throw new KeyNotFoundException($"Key '{key}' not found in cache.");
        }

        /// <summary>
        /// Sets a value in the cache.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        public void Set(TKey key, TValue value)
        {
            lock (syncLock)
            {
                if (cache.TryGetValue(key, out var existingNode))
                {
                    // Update the existing item
                    existingNode.Value.Value = value;
                    lruList.Remove(existingNode);
                    lruList.AddFirst(existingNode);
                    return;
                }

                // If over capacity, remove the least recently used item
                if (cache.Count >= capacity)
                {
                    RemoveLeastRecentlyUsed();
                }

                // Add the new item
                var item = new CacheItem { Key = key, Value = value };
                var node = new LinkedListNode<CacheItem>(item);
                lruList.AddFirst(node);
                cache[key] = node;
            }
        }

        /// <summary>
        /// Gets a value, or creates and adds it using the factory function if it does not exist.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="factory">Function that produces the value</param>
        /// <returns>Value</returns>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        {
            lock (syncLock)
            {
                if (TryGet(key, out var value))
                    return value;

                value = factory(key);
                Set(key, value);
                return value;
            }
        }

        /// <summary>
        /// Checks whether a key exists in the cache.
        /// </summary>
        public bool Contains(TKey key)
        {
            lock (syncLock)
            {
                return cache.ContainsKey(key);
            }
        }

        /// <summary>
        /// Removes the item with the specified key.
        /// </summary>
        public bool Remove(TKey key)
        {
            lock (syncLock)
            {
                if (cache.TryGetValue(key, out var node))
                {
                    lruList.Remove(node);
                    cache.Remove(key);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Clears the cache.
        /// </summary>
        public void Clear()
        {
            lock (syncLock)
            {
                cache.Clear();
                lruList.Clear();
            }
        }

        /// <summary>
        /// Resets the statistics counters.
        /// </summary>
        public void ResetStatistics()
        {
            HitCount = 0;
            MissCount = 0;
        }

        /// <summary>
        /// Removes the least recently used item.
        /// </summary>
        private void RemoveLeastRecentlyUsed()
        {
            var lastNode = lruList.Last;
            if (lastNode != null)
            {
                cache.Remove(lastNode.Value.Key);
                lruList.RemoveLast();
            }
        }

        private class CacheItem
        {
            public TKey Key;
            public TValue Value;
        }
    }
}
