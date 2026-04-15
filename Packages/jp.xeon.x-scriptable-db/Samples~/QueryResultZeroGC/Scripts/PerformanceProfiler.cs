using System;
using System.Diagnostics;
using UnityEngine;

namespace Xeon.XScriptableDB.Samples.QueryResultZeroGC
{
    /// <summary>
    /// Performance measurement utility.
    /// </summary>
    public static class PerformanceProfiler
    {
        /// <summary>
        /// Measures execution time and GC allocation.
        /// </summary>
        /// <param name="action">Action to measure</param>
        /// <param name="iterations">Number of iterations</param>
        /// <returns>Measurement result</returns>
        public static ProfileResult Measure(Action action, int iterations = 1)
        {
            // Warmup
            action();

            // Run GC to start from a clean state
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var gcCountsBefore = new int[3];
            for (int i = 0; i < 3; i++)
                gcCountsBefore[i] = GC.CollectionCount(i);

            long memoryBefore = GC.GetTotalMemory(false);

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
                action();

            sw.Stop();

            long memoryAfter = GC.GetTotalMemory(false);

            var gcCountsAfter = new int[3];
            for (int i = 0; i < 3; i++)
                gcCountsAfter[i] = GC.CollectionCount(i);

            return new ProfileResult
            {
                Iterations = iterations,
                TotalMilliseconds = sw.Elapsed.TotalMilliseconds,
                AverageMilliseconds = sw.Elapsed.TotalMilliseconds / iterations,
                MemoryAllocated = memoryAfter - memoryBefore,
                Gen0Collections = gcCountsAfter[0] - gcCountsBefore[0],
                Gen1Collections = gcCountsAfter[1] - gcCountsBefore[1],
                Gen2Collections = gcCountsAfter[2] - gcCountsBefore[2]
            };
        }

        /// <summary>
        /// Compares measurement of two operations.
        /// </summary>
        public static ComparisonResult Compare(
            string name1, Action action1,
            string name2, Action action2,
            int iterations = 1000)
        {
            var result1 = Measure(action1, iterations);
            var result2 = Measure(action2, iterations);

            return new ComparisonResult
            {
                Name1 = name1,
                Result1 = result1,
                Name2 = name2,
                Result2 = result2
            };
        }
    }

    /// <summary>
    /// Performance measurement result.
    /// </summary>
    public class ProfileResult
    {
        public int Iterations { get; set; }
        public double TotalMilliseconds { get; set; }
        public double AverageMilliseconds { get; set; }
        public long MemoryAllocated { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }

        public override string ToString()
        {
            return $"Iterations: {Iterations}\n" +
                   $"Total: {TotalMilliseconds:F3}ms\n" +
                   $"Average: {AverageMilliseconds:F3}ms\n" +
                   $"Memory: {MemoryAllocated:N0} bytes\n" +
                   $"GC: Gen0={Gen0Collections}, Gen1={Gen1Collections}, Gen2={Gen2Collections}";
        }
    }

    /// <summary>
    /// Comparison measurement result.
    /// </summary>
    public class ComparisonResult
    {
        public string Name1 { get; set; }
        public ProfileResult Result1 { get; set; }
        public string Name2 { get; set; }
        public ProfileResult Result2 { get; set; }

        public double SpeedupRatio => Result2.AverageMilliseconds / Result1.AverageMilliseconds;

        public override string ToString()
        {
            return $"=== {Name1} ===\n{Result1}\n\n" +
                   $"=== {Name2} ===\n{Result2}\n\n" +
                   $"Speedup: {SpeedupRatio:F2}x";
        }
    }
}
