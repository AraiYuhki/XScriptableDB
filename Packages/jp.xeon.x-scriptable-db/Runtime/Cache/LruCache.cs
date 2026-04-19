using System;
using System.Collections.Generic;

namespace Xeon.XScriptableDB.Cache
{
    /// <summary>
    /// LRU (Least Recently Used) キャッシュ。
    /// 容量を超えた場合、最も最近使用されていないアイテムを削除します。
    /// </summary>
    /// <typeparam name="TKey">キーの型</typeparam>
    /// <typeparam name="TValue">値の型</typeparam>
    public class LruCache<TKey, TValue>
    {
        private readonly int capacity;
        private readonly Dictionary<TKey, LinkedListNode<CacheItem>> cache;
        private readonly LinkedList<CacheItem> lruList;
        private readonly object syncLock = new();

        /// <summary>キャッシュの最大容量</summary>
        public int Capacity => capacity;

        /// <summary>現在のアイテム数</summary>
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

        /// <summary>キャッシュヒット数</summary>
        public long HitCount { get; private set; }

        /// <summary>キャッシュミス数</summary>
        public long MissCount { get; private set; }

        /// <summary>ヒット率</summary>
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
        /// キャッシュから値を取得します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="value">値（見つかった場合）</param>
        /// <returns>キーが存在する場合はtrue</returns>
        public bool TryGet(TKey key, out TValue value)
        {
            lock (syncLock)
            {
                if (cache.TryGetValue(key, out var node))
                {
                    // アクセスされたため、リストの先頭に移動します
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
        /// 値を取得します。キーが存在しない場合は例外をスローします。
        /// </summary>
        public TValue Get(TKey key)
        {
            if (TryGet(key, out var value))
                return value;

            throw new KeyNotFoundException($"Key '{key}' not found in cache.");
        }

        /// <summary>
        /// キャッシュに値を設定します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="value">値</param>
        public void Set(TKey key, TValue value)
        {
            lock (syncLock)
            {
                if (cache.TryGetValue(key, out var existingNode))
                {
                    // 既存のアイテムを更新します
                    existingNode.Value.Value = value;
                    lruList.Remove(existingNode);
                    lruList.AddFirst(existingNode);
                    return;
                }

                // 容量を超えている場合、最も最近使用されていないアイテムを削除します
                if (cache.Count >= capacity)
                {
                    RemoveLeastRecentlyUsed();
                }

                // 新しいアイテムを追加します
                var item = new CacheItem { Key = key, Value = value };
                var node = new LinkedListNode<CacheItem>(item);
                lruList.AddFirst(node);
                cache[key] = node;
            }
        }

        /// <summary>
        /// 値を取得します。存在しない場合は、ファクトリ関数を使用して作成し、追加します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="factory">値を生成する関数</param>
        /// <returns>値</returns>
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
        /// キーがキャッシュに存在するかどうかを確認します。
        /// </summary>
        public bool Contains(TKey key)
        {
            lock (syncLock)
            {
                return cache.ContainsKey(key);
            }
        }

        /// <summary>
        /// 指定されたキーのアイテムを削除します。
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
        /// キャッシュをクリアします。
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
        /// 統計カウンターをリセットします。
        /// </summary>
        public void ResetStatistics()
        {
            HitCount = 0;
            MissCount = 0;
        }

        /// <summary>
        /// 最も最近使用されていないアイテムを削除します。
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
