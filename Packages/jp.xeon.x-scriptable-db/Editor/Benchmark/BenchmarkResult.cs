namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// ベンチマーク結果。
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