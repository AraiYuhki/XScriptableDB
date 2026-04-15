using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Xeon.XScriptableDB.Performance
{
    /// <summary>
    /// Memory profiler.
    /// Estimates memory usage for tables.
    /// </summary>
    public static class MemoryProfiler
    {
        /// <summary>
        /// Estimates the memory usage of a table.
        /// </summary>
        /// <param name="tableAsset">Table asset</param>
        /// <returns>Memory information</returns>
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
        /// Estimates the memory usage of multiple tables.
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
        /// Estimates the size of a type.
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Estimated size in bytes</returns>
        public static long EstimateTypeSize(Type type)
        {
            if (type == null) return 0;

            // Primitive types
            if (type.IsPrimitive)
            {
                return GetPrimitiveSize(type);
            }

            // String (assume an average size)
            if (type == typeof(string))
            {
                return 40; // Object header + average 20 characters
            }

            // Enum
            if (type.IsEnum)
            {
                return 4;
            }

            // Array
            if (type.IsArray)
            {
                return 24 + EstimateTypeSize(type.GetElementType()) * 10; // Assumed array size
            }

            // Class/struct
            long size = type.IsValueType ? 0 : 16; // Object header

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                size += EstimateFieldSize(field);
            }

            return size;
        }

        /// <summary>
        /// Estimates the size of a field.
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
                return 8; // Reference only (string body is counted separately)
            }

            if (type.IsEnum)
            {
                return 4;
            }

            if (type.IsValueType)
            {
                return EstimateTypeSize(type);
            }

            // Reference types: size of the reference
            return 8;
        }

        /// <summary>
        /// Gets the size of a primitive type.
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
        /// Estimates the overhead of a table.
        /// </summary>
        private static long EstimateTableOverhead()
        {
            // ScriptableObject + list + indexes, etc.
            return 256;
        }

        /// <summary>
        /// Gets the current GC memory usage.
        /// </summary>
        public static long GetTotalMemory()
        {
            return GC.GetTotalMemory(false);
        }

        /// <summary>
        /// Runs GC and returns the memory usage afterward.
        /// </summary>
        public static long GetTotalMemoryAfterGC()
        {
            return GC.GetTotalMemory(true);
        }

        /// <summary>
        /// Takes a snapshot of the current memory usage.
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
}
