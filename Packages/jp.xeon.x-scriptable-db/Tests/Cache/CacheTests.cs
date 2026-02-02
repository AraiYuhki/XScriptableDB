using NUnit.Framework;
using System;
using System.Collections.Generic;
using Xeon.XScriptableDB.Cache;

namespace Xeon.XScriptableDB.Tests.Cache
{
    /// <summary>
    /// キャッシュシステムのテスト。
    /// </summary>
    public class CacheTests
    {
        #region LruCache Tests

        [Test]
        public void LruCache_Constructor_SetsCapacity()
        {
            var cache = new LruCache<string, int>(100);

            Assert.AreEqual(100, cache.Capacity);
            Assert.AreEqual(0, cache.Count);
        }

        [Test]
        public void LruCache_Constructor_InvalidCapacity_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LruCache<string, int>(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LruCache<string, int>(-1));
        }

        [Test]
        public void LruCache_SetAndGet_ReturnsValue()
        {
            var cache = new LruCache<string, int>(10);
            cache.Set("key1", 100);

            var found = cache.TryGet("key1", out var value);

            Assert.IsTrue(found);
            Assert.AreEqual(100, value);
        }

        [Test]
        public void LruCache_Get_NonExistentKey_ReturnsFalse()
        {
            var cache = new LruCache<string, int>(10);

            var found = cache.TryGet("nonexistent", out var value);

            Assert.IsFalse(found);
            Assert.AreEqual(0, value);
        }

        [Test]
        public void LruCache_Set_UpdatesExistingValue()
        {
            var cache = new LruCache<string, int>(10);
            cache.Set("key1", 100);
            cache.Set("key1", 200);

            cache.TryGet("key1", out var value);

            Assert.AreEqual(200, value);
            Assert.AreEqual(1, cache.Count);
        }

        [Test]
        public void LruCache_EvictsLeastRecentlyUsed_WhenCapacityExceeded()
        {
            var cache = new LruCache<string, int>(3);
            cache.Set("key1", 1);
            cache.Set("key2", 2);
            cache.Set("key3", 3);

            // key1が最も古い
            cache.Set("key4", 4);

            Assert.IsFalse(cache.Contains("key1"));
            Assert.IsTrue(cache.Contains("key2"));
            Assert.IsTrue(cache.Contains("key3"));
            Assert.IsTrue(cache.Contains("key4"));
        }

        [Test]
        public void LruCache_AccessUpdatesLruOrder()
        {
            var cache = new LruCache<string, int>(3);
            cache.Set("key1", 1);
            cache.Set("key2", 2);
            cache.Set("key3", 3);

            // key1にアクセスして最新にする
            cache.TryGet("key1", out _);

            // key2が最も古くなる
            cache.Set("key4", 4);

            Assert.IsTrue(cache.Contains("key1"));
            Assert.IsFalse(cache.Contains("key2"));
            Assert.IsTrue(cache.Contains("key3"));
            Assert.IsTrue(cache.Contains("key4"));
        }

        [Test]
        public void LruCache_Remove_RemovesEntry()
        {
            var cache = new LruCache<string, int>(10);
            cache.Set("key1", 100);

            var removed = cache.Remove("key1");

            Assert.IsTrue(removed);
            Assert.IsFalse(cache.Contains("key1"));
            Assert.AreEqual(0, cache.Count);
        }

        [Test]
        public void LruCache_Remove_NonExistent_ReturnsFalse()
        {
            var cache = new LruCache<string, int>(10);

            var removed = cache.Remove("nonexistent");

            Assert.IsFalse(removed);
        }

        [Test]
        public void LruCache_Clear_RemovesAllEntries()
        {
            var cache = new LruCache<string, int>(10);
            cache.Set("key1", 1);
            cache.Set("key2", 2);
            cache.Set("key3", 3);

            cache.Clear();

            Assert.AreEqual(0, cache.Count);
            Assert.IsFalse(cache.Contains("key1"));
        }

        [Test]
        public void LruCache_Statistics_TracksHitsAndMisses()
        {
            var cache = new LruCache<string, int>(10);
            cache.Set("key1", 100);

            cache.TryGet("key1", out _); // hit
            cache.TryGet("key1", out _); // hit
            cache.TryGet("key2", out _); // miss

            Assert.AreEqual(2, cache.HitCount);
            Assert.AreEqual(1, cache.MissCount);
            Assert.AreEqual(2.0 / 3.0, cache.HitRate, 0.001);
        }

        [Test]
        public void LruCache_ResetStatistics_ClearsCounters()
        {
            var cache = new LruCache<string, int>(10);
            cache.Set("key1", 100);
            cache.TryGet("key1", out _);
            cache.TryGet("key2", out _);

            cache.ResetStatistics();

            Assert.AreEqual(0, cache.HitCount);
            Assert.AreEqual(0, cache.MissCount);
            Assert.AreEqual(0, cache.HitRate);
        }

        [Test]
        public void LruCache_GetOrAdd_ReturnsExistingValue()
        {
            var cache = new LruCache<string, int>(10);
            cache.Set("key1", 100);
            var factoryCalled = false;

            var value = cache.GetOrAdd("key1", _ =>
            {
                factoryCalled = true;
                return 999;
            });

            Assert.AreEqual(100, value);
            Assert.IsFalse(factoryCalled);
        }

        [Test]
        public void LruCache_GetOrAdd_CreatesAndCachesNewValue()
        {
            var cache = new LruCache<string, int>(10);

            var value = cache.GetOrAdd("key1", _ => 100);

            Assert.AreEqual(100, value);
            Assert.IsTrue(cache.Contains("key1"));
            cache.TryGet("key1", out var cachedValue);
            Assert.AreEqual(100, cachedValue);
        }

        [Test]
        public void LruCache_Get_ThrowsOnMissingKey()
        {
            var cache = new LruCache<string, int>(10);

            Assert.Throws<KeyNotFoundException>(() => cache.Get("nonexistent"));
        }

        #endregion

        #region QueryCache Tests

        [Test]
        public void QueryCache_Constructor_SetsCapacity()
        {
            var cache = new QueryCache(500);

            Assert.AreEqual(500, cache.Capacity);
            Assert.AreEqual(0, cache.Count);
        }

        [Test]
        public void QueryCache_SetAndGet_ReturnsValue()
        {
            var cache = new QueryCache();
            cache.Set(typeof(TestTable), "FindById", 1, "TestValue");

            var found = cache.TryGet<string>(typeof(TestTable), "FindById", 1, out var value);

            Assert.IsTrue(found);
            Assert.AreEqual("TestValue", value);
        }

        [Test]
        public void QueryCache_Get_NonExistent_ReturnsFalse()
        {
            var cache = new QueryCache();

            var found = cache.TryGet<string>(typeof(TestTable), "FindById", 999, out var value);

            Assert.IsFalse(found);
            Assert.IsNull(value);
        }

        [Test]
        public void QueryCache_InvalidateTable_InvalidatesAllTableEntries()
        {
            var cache = new QueryCache();
            cache.Set(typeof(TestTable), "FindById", 1, "Value1");
            cache.Set(typeof(TestTable), "FindById", 2, "Value2");
            cache.Set(typeof(OtherTable), "FindById", 1, "OtherValue");

            cache.InvalidateTable(typeof(TestTable));

            Assert.IsFalse(cache.TryGet<string>(typeof(TestTable), "FindById", 1, out _));
            Assert.IsFalse(cache.TryGet<string>(typeof(TestTable), "FindById", 2, out _));
            Assert.IsTrue(cache.TryGet<string>(typeof(OtherTable), "FindById", 1, out _));
        }

        [Test]
        public void QueryCache_Invalidate_RemovesSpecificEntry()
        {
            var cache = new QueryCache();
            cache.Set(typeof(TestTable), "FindById", 1, "Value1");
            cache.Set(typeof(TestTable), "FindById", 2, "Value2");

            cache.Invalidate(typeof(TestTable), "FindById", 1);

            Assert.IsFalse(cache.TryGet<string>(typeof(TestTable), "FindById", 1, out _));
            Assert.IsTrue(cache.TryGet<string>(typeof(TestTable), "FindById", 2, out _));
        }

        [Test]
        public void QueryCache_Clear_RemovesAllEntries()
        {
            var cache = new QueryCache();
            cache.Set(typeof(TestTable), "FindById", 1, "Value1");
            cache.Set(typeof(OtherTable), "FindById", 1, "OtherValue");

            cache.Clear();

            Assert.AreEqual(0, cache.Count);
        }

        [Test]
        public void QueryCache_Statistics_TracksUsage()
        {
            var cache = new QueryCache();
            cache.Set(typeof(TestTable), "FindById", 1, "Value1");

            cache.TryGet<string>(typeof(TestTable), "FindById", 1, out _); // hit
            cache.TryGet<string>(typeof(TestTable), "FindById", 2, out _); // miss

            Assert.AreEqual(1, cache.HitCount);
            Assert.AreEqual(1, cache.MissCount);
            Assert.AreEqual(0.5, cache.HitRate, 0.001);
        }

        [Test]
        public void QueryCache_GetOrAdd_ReturnsExistingValue()
        {
            var cache = new QueryCache();
            cache.Set(typeof(TestTable), "FindById", 1, "Original");
            var factoryCalled = false;

            var value = cache.GetOrAdd<string>(typeof(TestTable), "FindById", 1, () =>
            {
                factoryCalled = true;
                return "New";
            });

            Assert.AreEqual("Original", value);
            Assert.IsFalse(factoryCalled);
        }

        [Test]
        public void QueryCache_GetOrAdd_CreatesNewValue()
        {
            var cache = new QueryCache();

            var value = cache.GetOrAdd<string>(typeof(TestTable), "FindById", 1, () => "Created");

            Assert.AreEqual("Created", value);
            Assert.IsTrue(cache.TryGet<string>(typeof(TestTable), "FindById", 1, out _));
        }

        [Test]
        public void QueryCache_GetStatistics_ReturnsCorrectData()
        {
            var cache = new QueryCache(200);
            cache.Set(typeof(TestTable), "FindById", 1, "Value1");
            cache.TryGet<string>(typeof(TestTable), "FindById", 1, out _);

            var stats = cache.GetStatistics();

            Assert.AreEqual(200, stats.Capacity);
            Assert.AreEqual(1, stats.Count);
            Assert.AreEqual(1, stats.HitCount);
        }

        #endregion

        #region QueryCacheKey Tests

        [Test]
        public void QueryCacheKey_Equals_SameValues_ReturnsTrue()
        {
            var key1 = new QueryCacheKey(typeof(TestTable), "FindById", 1);
            var key2 = new QueryCacheKey(typeof(TestTable), "FindById", 1);

            Assert.IsTrue(key1.Equals(key2));
            Assert.AreEqual(key1.GetHashCode(), key2.GetHashCode());
        }

        [Test]
        public void QueryCacheKey_Equals_DifferentValues_ReturnsFalse()
        {
            var key1 = new QueryCacheKey(typeof(TestTable), "FindById", 1);
            var key2 = new QueryCacheKey(typeof(TestTable), "FindById", 2);
            var key3 = new QueryCacheKey(typeof(TestTable), "FindByName", 1);
            var key4 = new QueryCacheKey(typeof(OtherTable), "FindById", 1);

            Assert.IsFalse(key1.Equals(key2));
            Assert.IsFalse(key1.Equals(key3));
            Assert.IsFalse(key1.Equals(key4));
        }

        [Test]
        public void QueryCacheKey_ToString_ReturnsFormattedString()
        {
            var key = new QueryCacheKey(typeof(TestTable), "FindById", 42);

            var str = key.ToString();

            Assert.AreEqual("TestTable.FindById(42)", str);
        }

        #endregion

        #region CacheManager Tests

        [Test]
        public void CacheManager_QueryCache_ReturnsInstance()
        {
            var cache = CacheManager.QueryCache;

            Assert.IsNotNull(cache);
        }

        [Test]
        public void CacheManager_Clear_ClearsQueryCache()
        {
            CacheManager.QueryCache.Set(typeof(TestTable), "FindById", 1, "Value");

            CacheManager.Clear();

            Assert.AreEqual(0, CacheManager.QueryCache.Count);
        }

        [Test]
        public void CacheManager_IsEnabled_WhenDisabled_ClearsCache()
        {
            CacheManager.IsEnabled = true;
            CacheManager.QueryCache.Set(typeof(TestTable), "FindById", 1, "Value");

            CacheManager.IsEnabled = false;

            Assert.AreEqual(0, CacheManager.QueryCache.Count);
            Assert.IsFalse(CacheManager.IsEnabled);

            // テスト後のリセット
            CacheManager.IsEnabled = true;
        }

        [Test]
        public void CacheManager_Initialize_CreatesNewCache()
        {
            CacheManager.Initialize(500);
            var cache = CacheManager.QueryCache;

            Assert.AreEqual(500, cache.Capacity);
        }

        [TearDown]
        public void TearDown()
        {
            // テスト後にキャッシュをリセット
            CacheManager.IsEnabled = true;
            CacheManager.Clear();
            CacheManager.ResetStatistics();
        }

        #endregion

        // テスト用のダミー型
        private class TestTable { }
        private class OtherTable { }
    }
}
