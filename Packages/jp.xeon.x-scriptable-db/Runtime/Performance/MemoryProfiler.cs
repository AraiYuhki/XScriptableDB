using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Xeon.XScriptableDB.Performance
{
    /// <summary>
    /// テーブルのメモリ使用量情報。
    /// </summary>
    public struct TableMemoryInfo
    {
        public string TableName;
        public Type TableType;
        public Type RecordType;
        public int RecordCount;
        public long EstimatedRecordSize;
        public long EstimatedTotalSize;

        public override string ToString()
        {
            return $"{TableName}: {RecordCount} records, ~{FormatBytes(EstimatedTotalSize)}";
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }
    }

    /// <summary>
    /// メモリプロファイラー。
    /// テーブルのメモリ使用量を推定する。
    /// </summary>
    public static class MemoryProfiler
    {
        /// <summary>
        /// テーブルのメモリ使用量を推定する。
        /// </summary>
        /// <param name="tableAsset">テーブルアセット</param>
        /// <returns>メモリ情報</returns>
        public static TableMemoryInfo EstimateMemoryUsage(ITableAsset tableAsset)
        {
            if (tableAsset == null)
            {
                return new TableMemoryInfo();
            }

            var recordType = tableAsset.RecordType;
            var recordSize = EstimateTypeSize(recordType);

            return new TableMemoryInfo
            {
                TableName = tableAsset.GetType().Name,
                TableType = tableAsset.GetType(),
                RecordType = recordType,
                RecordCount = tableAsset.Count,
                EstimatedRecordSize = recordSize,
                EstimatedTotalSize = recordSize * tableAsset.Count + EstimateTableOverhead()
            };
        }

        /// <summary>
        /// 複数のテーブルのメモリ使用量を推定する。
        /// </summary>
        public static IReadOnlyList<TableMemoryInfo> EstimateMemoryUsage(IEnumerable<ITableAsset> tables)
        {
            var result = new List<TableMemoryInfo>();

            foreach (var table in tables)
            {
                result.Add(EstimateMemoryUsage(table));
            }

            return result;
        }

        /// <summary>
        /// 型のサイズを推定する。
        /// </summary>
        /// <param name="type">型</param>
        /// <returns>推定サイズ（バイト）</returns>
        public static long EstimateTypeSize(Type type)
        {
            if (type == null) return 0;

            // プリミティブ型
            if (type.IsPrimitive)
            {
                return GetPrimitiveSize(type);
            }

            // 文字列（平均的なサイズを仮定）
            if (type == typeof(string))
            {
                return 40; // オブジェクトヘッダー + 平均20文字
            }

            // 列挙型
            if (type.IsEnum)
            {
                return 4;
            }

            // 配列
            if (type.IsArray)
            {
                return 24 + EstimateTypeSize(type.GetElementType()) * 10; // 仮の配列サイズ
            }

            // クラス/構造体
            long size = type.IsValueType ? 0 : 16; // オブジェクトヘッダー

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                size += EstimateFieldSize(field);
            }

            return size;
        }

        /// <summary>
        /// フィールドのサイズを推定する。
        /// </summary>
        private static long EstimateFieldSize(FieldInfo field)
        {
            var type = field.FieldType;

            if (type.IsPrimitive)
            {
                return GetPrimitiveSize(type);
            }

            if (type == typeof(string))
            {
                return 8; // 参照のみ（文字列本体は別途）
            }

            if (type.IsEnum)
            {
                return 4;
            }

            if (type.IsValueType)
            {
                return EstimateTypeSize(type);
            }

            // 参照型は参照のサイズ
            return 8;
        }

        /// <summary>
        /// プリミティブ型のサイズを取得する。
        /// </summary>
        private static int GetPrimitiveSize(Type type)
        {
            if (type == typeof(bool)) return 1;
            if (type == typeof(byte) || type == typeof(sbyte)) return 1;
            if (type == typeof(short) || type == typeof(ushort)) return 2;
            if (type == typeof(int) || type == typeof(uint)) return 4;
            if (type == typeof(long) || type == typeof(ulong)) return 8;
            if (type == typeof(float)) return 4;
            if (type == typeof(double)) return 8;
            if (type == typeof(char)) return 2;
            if (type == typeof(decimal)) return 16;

            return Marshal.SizeOf(type);
        }

        /// <summary>
        /// テーブルのオーバーヘッドを推定する。
        /// </summary>
        private static long EstimateTableOverhead()
        {
            // ScriptableObject + リスト + インデックス等
            return 256;
        }

        /// <summary>
        /// 現在のGCメモリ使用量を取得する。
        /// </summary>
        public static long GetTotalMemory()
        {
            return GC.GetTotalMemory(false);
        }

        /// <summary>
        /// GCを実行してメモリ使用量を取得する。
        /// </summary>
        public static long GetTotalMemoryAfterGC()
        {
            return GC.GetTotalMemory(true);
        }

        /// <summary>
        /// メモリ使用量のスナップショットを取得する。
        /// </summary>
        public static MemorySnapshot TakeSnapshot()
        {
            return new MemorySnapshot
            {
                Timestamp = DateTime.Now,
                TotalMemory = GetTotalMemory(),
                GCCollectionCount0 = GC.CollectionCount(0),
                GCCollectionCount1 = GC.CollectionCount(1),
                GCCollectionCount2 = GC.CollectionCount(2)
            };
        }
    }

    /// <summary>
    /// メモリスナップショット。
    /// </summary>
    public struct MemorySnapshot
    {
        public DateTime Timestamp;
        public long TotalMemory;
        public int GCCollectionCount0;
        public int GCCollectionCount1;
        public int GCCollectionCount2;

        public override string ToString()
        {
            return $"Memory: {TotalMemory / 1024.0 / 1024.0:F2} MB, " +
                   $"GC: [{GCCollectionCount0}, {GCCollectionCount1}, {GCCollectionCount2}]";
        }
    }
}
