namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Benchmark result.
    /// </summary>
    public class BenchmarkResult
    {
        public string Name { get; set; }
        public int DataSize { get; set; }
        public double ElapsedMs { get; set; }
        public double PerOperationUs { get; set; }
        public long MemoryBytes { get; set; }
        public int Iterations { get; set; }
        public bool Success { get; set; } = true;
        public string Error { get; set; }
    }
}