using NUnit.Framework;
using System;
using Xeon.XScriptableDB.Performance;

namespace Xeon.XScriptableDB.Tests.Performance
{
    /// <summary>
    /// パフォーマンスプロファイラーのテスト。
    /// </summary>
    public class PerformanceTests
    {
        #region QueryProfiler Tests

        [Test]
        public void QueryProfiler_Constructor_SetsMaxProfiles()
        {
            var profiler = new QueryProfiler(100);

            Assert.AreEqual(0, profiler.ProfileCount);
        }

        [Test]
        public void QueryProfiler_Profile_RecordsExecution()
        {
            var profiler = new QueryProfiler();

            var result = profiler.Profile("TestQuery", typeof(TestTable), () =>
            {
                return new int[] { 1, 2, 3 };
            });

            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(1, profiler.ProfileCount);

            var profiles = profiler.GetProfiles();
            Assert.AreEqual(1, profiles.Count);
            Assert.AreEqual("TestQuery", profiles[0].QueryName);
            Assert.AreEqual(typeof(TestTable), profiles[0].TableType);
            Assert.AreEqual(3, profiles[0].ResultCount);
        }

        [Test]
        public void QueryProfiler_Profile_WhenDisabled_SkipsRecording()
        {
            var profiler = new QueryProfiler();
            profiler.IsEnabled = false;

            var result = profiler.Profile("TestQuery", typeof(TestTable), () => 42);

            Assert.AreEqual(42, result);
            Assert.AreEqual(0, profiler.ProfileCount);
        }

        [Test]
        public void QueryProfiler_RecordProfile_AddsProfile()
        {
            var profiler = new QueryProfiler();
            var profile = new QueryProfile
            {
                QueryName = "ManualQuery",
                TableType = typeof(TestTable),
                ElapsedMilliseconds = 1.5,
                ResultCount = 10,
                Timestamp = DateTime.Now
            };

            profiler.RecordProfile(profile);

            Assert.AreEqual(1, profiler.ProfileCount);
            var profiles = profiler.GetProfiles();
            Assert.AreEqual("ManualQuery", profiles[0].QueryName);
        }

        [Test]
        public void QueryProfiler_MaxProfiles_EvictsOldest()
        {
            var profiler = new QueryProfiler(3);

            profiler.Profile("Query1", typeof(TestTable), () => 1);
            profiler.Profile("Query2", typeof(TestTable), () => 2);
            profiler.Profile("Query3", typeof(TestTable), () => 3);
            profiler.Profile("Query4", typeof(TestTable), () => 4);

            Assert.AreEqual(3, profiler.ProfileCount);

            var profiles = profiler.GetProfiles();
            Assert.AreEqual("Query2", profiles[0].QueryName);
            Assert.AreEqual("Query4", profiles[2].QueryName);
        }

        [Test]
        public void QueryProfiler_GetStatistics_ReturnsCorrectData()
        {
            var profiler = new QueryProfiler();
            profiler.Profile("Query1", typeof(TestTable), () => new int[5]);
            profiler.Profile("Query2", typeof(TestTable), () => new int[3]);

            var stats = profiler.GetStatistics();

            Assert.AreEqual(2, stats.QueryCount);
            Assert.GreaterOrEqual(stats.TotalMilliseconds, 0);
            Assert.GreaterOrEqual(stats.AverageMilliseconds, 0);
        }

        [Test]
        public void QueryProfiler_GetStatistics_Empty_ReturnsDefault()
        {
            var profiler = new QueryProfiler();

            var stats = profiler.GetStatistics();

            Assert.AreEqual(0, stats.QueryCount);
            Assert.AreEqual(0, stats.TotalMilliseconds);
        }

        [Test]
        public void QueryProfiler_GetSlowQueries_FiltersCorrectly()
        {
            var profiler = new QueryProfiler();

            // プロファイルを手動で追加（実行時間を制御するため）
            profiler.RecordProfile(new QueryProfile
            {
                QueryName = "FastQuery",
                ElapsedMilliseconds = 0.5
            });
            profiler.RecordProfile(new QueryProfile
            {
                QueryName = "SlowQuery",
                ElapsedMilliseconds = 5.0
            });
            profiler.RecordProfile(new QueryProfile
            {
                QueryName = "VerySlowQuery",
                ElapsedMilliseconds = 10.0
            });

            var slowQueries = profiler.GetSlowQueries(2.0);

            Assert.AreEqual(2, slowQueries.Count);
            Assert.AreEqual("SlowQuery", slowQueries[0].QueryName);
            Assert.AreEqual("VerySlowQuery", slowQueries[1].QueryName);
        }

        [Test]
        public void QueryProfiler_Clear_RemovesAllProfiles()
        {
            var profiler = new QueryProfiler();
            profiler.Profile("Query1", typeof(TestTable), () => 1);
            profiler.Profile("Query2", typeof(TestTable), () => 2);

            profiler.Clear();

            Assert.AreEqual(0, profiler.ProfileCount);
        }

        #endregion

        #region QueryProfile Tests

        [Test]
        public void QueryProfile_ToString_FormatsCorrectly()
        {
            var profile = new QueryProfile
            {
                QueryName = "FindById",
                TableType = typeof(TestTable),
                ElapsedMilliseconds = 1.234,
                ResultCount = 5,
                WasCached = false
            };

            var str = profile.ToString();

            Assert.IsTrue(str.Contains("FindById"));
            Assert.IsTrue(str.Contains("TestTable"));
            Assert.IsTrue(str.Contains("1.234"));
            Assert.IsTrue(str.Contains("5 results"));
        }

        [Test]
        public void QueryProfile_ToString_ShowsCachedFlag()
        {
            var profile = new QueryProfile
            {
                QueryName = "FindById",
                TableType = typeof(TestTable),
                WasCached = true
            };

            var str = profile.ToString();

            Assert.IsTrue(str.Contains("cached"));
        }

        #endregion

        #region ProfileStatistics Tests

        [Test]
        public void ProfileStatistics_ToString_FormatsCorrectly()
        {
            var stats = new ProfileStatistics
            {
                QueryCount = 100,
                TotalMilliseconds = 50.5,
                AverageMilliseconds = 0.505,
                MinMilliseconds = 0.1,
                MaxMilliseconds = 2.5,
                CachedQueryCount = 30
            };

            var str = stats.ToString();

            Assert.IsTrue(str.Contains("100"));
            Assert.IsTrue(str.Contains("50.50"));
            Assert.IsTrue(str.Contains("Cached: 30"));
        }

        #endregion

        #region MemoryProfiler Tests

        [Test]
        public void MemoryProfiler_EstimateTypeSize_Primitives()
        {
            Assert.AreEqual(1, MemoryProfiler.EstimateTypeSize(typeof(bool)));
            Assert.AreEqual(1, MemoryProfiler.EstimateTypeSize(typeof(byte)));
            Assert.AreEqual(4, MemoryProfiler.EstimateTypeSize(typeof(int)));
            Assert.AreEqual(8, MemoryProfiler.EstimateTypeSize(typeof(long)));
            Assert.AreEqual(4, MemoryProfiler.EstimateTypeSize(typeof(float)));
            Assert.AreEqual(8, MemoryProfiler.EstimateTypeSize(typeof(double)));
        }

        [Test]
        public void MemoryProfiler_EstimateTypeSize_String()
        {
            var size = MemoryProfiler.EstimateTypeSize(typeof(string));

            // 文字列は推定サイズ（オブジェクトヘッダー + 平均文字数）
            Assert.Greater(size, 0);
        }

        [Test]
        public void MemoryProfiler_EstimateTypeSize_Enum()
        {
            var size = MemoryProfiler.EstimateTypeSize(typeof(TestEnum));

            Assert.AreEqual(4, size);
        }

        [Test]
        public void MemoryProfiler_EstimateTypeSize_Null()
        {
            var size = MemoryProfiler.EstimateTypeSize(null);

            Assert.AreEqual(0, size);
        }

        [Test]
        public void MemoryProfiler_GetTotalMemory_ReturnsPositiveValue()
        {
            var memory = MemoryProfiler.GetTotalMemory();

            Assert.Greater(memory, 0);
        }

        [Test]
        public void MemoryProfiler_TakeSnapshot_ReturnsValidData()
        {
            var snapshot = MemoryProfiler.TakeSnapshot();

            Assert.Greater(snapshot.TotalMemory, 0);
            Assert.GreaterOrEqual(snapshot.GCCollectionCount0, 0);
            Assert.GreaterOrEqual(snapshot.GCCollectionCount1, 0);
            Assert.GreaterOrEqual(snapshot.GCCollectionCount2, 0);
            Assert.AreNotEqual(default(DateTime), snapshot.Timestamp);
        }

        #endregion

        #region MemorySnapshot Tests

        [Test]
        public void MemorySnapshot_ToString_FormatsCorrectly()
        {
            var snapshot = new MemorySnapshot
            {
                TotalMemory = 1024 * 1024 * 50,
                GCCollectionCount0 = 10,
                GCCollectionCount1 = 5,
                GCCollectionCount2 = 1
            };

            var str = snapshot.ToString();

            Assert.IsTrue(str.Contains("Memory:"));
            Assert.IsTrue(str.Contains("GC:"));
            Assert.IsTrue(str.Contains("[10, 5, 1]"));
        }

        #endregion

        #region TableMemoryInfo Tests

        [Test]
        public void TableMemoryInfo_ToString_FormatsCorrectly()
        {
            var info = new TableMemoryInfo
            {
                TableName = "ItemTable",
                RecordCount = 100,
                EstimatedTotalSize = 5120 // 5 KB
            };

            var str = info.ToString();

            Assert.IsTrue(str.Contains("ItemTable"));
            Assert.IsTrue(str.Contains("100 records"));
            Assert.IsTrue(str.Contains("KB"));
        }

        #endregion

        // テスト用のダミー型
        private class TestTable { }
        private enum TestEnum { Value1, Value2 }
    }
}
