using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// Serializable data structure for SecondaryKey indexes.
    /// Holds mappings from key values to record indexes.
    /// </summary>
    [Serializable]
    public class IndexData
    {
        [SerializeField]
        private string indexName;

        [SerializeField]
        private string keyTypeName;

        [SerializeField]
        private List<IndexEntry> entries = new();

        /// <summary>
        /// Name of the index.
        /// </summary>
        public string IndexName => indexName;

        /// <summary>
        /// Type name of the key.
        /// </summary>
        public string KeyTypeName => keyTypeName;

        /// <summary>
        /// Number of entries.
        /// </summary>
        public int Count => entries.Count;

        // Runtime hash maps (not serialized)
        [NonSerialized]
        private Dictionary<int, int[]> hashToIndices;

        [NonSerialized]
        private Dictionary<string, int[]> stringToIndices;

        [NonSerialized]
        private bool isInitialized;

        public IndexData() { }

        public IndexData(string name, Type keyType)
        {
            indexName = name;
            keyTypeName = keyType.FullName;
        }

        /// <summary>
        /// Adds an entry to the index.
        /// </summary>
        /// <param name="keyHash">Hash value of the key</param>
        /// <param name="keyString">String representation of the key</param>
        /// <param name="recordIndices">Array of record indexes</param>
        public void AddEntry(int keyHash, string keyString, int[] recordIndices)
        {
            entries.Add(new IndexEntry
            {
                keyHash = keyHash,
                keyString = keyString,
                recordIndices = recordIndices
            });
            isInitialized = false;
        }

        /// <summary>
        /// Clears the index.
        /// </summary>
        public void Clear()
        {
            entries.Clear();
            hashToIndices?.Clear();
            stringToIndices?.Clear();
            isInitialized = false;
        }

        /// <summary>
        /// Searches for record indexes by hash value.
        /// </summary>
        /// <param name="keyHash">Hash value of the key</param>
        /// <returns>Array of record indexes, or an empty array if not found</returns>
        public int[] FindByHash(int keyHash)
        {
            EnsureInitialized();
            return hashToIndices.TryGetValue(keyHash, out var indices) ? indices : Array.Empty<int>();
        }

        /// <summary>
        /// Searches for record indexes by string key.
        /// </summary>
        /// <param name="keyString">String representation of the key</param>
        /// <returns>Array of record indexes, or an empty array if not found</returns>
        public int[] FindByString(string keyString)
        {
            EnsureInitialized();
            return stringToIndices.TryGetValue(keyString, out var indices) ? indices : Array.Empty<int>();
        }

        /// <summary>
        /// Searches for record indexes by key (type-safe overload).
        /// Uses string-key-based lookup to avoid hash collisions.
        /// </summary>
        /// <typeparam name="TKey">Key type</typeparam>
        /// <param name="key">The key to search for</param>
        /// <returns>Array of record indexes, or an empty array if not found</returns>
        public int[] FindByKey<TKey>(TKey key)
        {
            if (key == null)
                return Array.Empty<int>();

            EnsureInitialized();

            // Search by string key (avoids hash collisions)
            // Convert using the same invariant culture used at index build time
            var keyString = ConvertToInvariantString(key);
            return FindByString(keyString);
        }

        /// <summary>
        /// Searches for record indexes by key (object overload).
        /// Uses string-key-based lookup to avoid hash collisions.
        /// </summary>
        /// <param name="key">The key to search for</param>
        /// <returns>Array of record indexes, or an empty array if not found</returns>
        public int[] FindByKey(object key)
        {
            if (key == null)
                return Array.Empty<int>();

            EnsureInitialized();

            // Convert using the same invariant culture used at index build time
            var keyString = ConvertToInvariantString(key);
            return FindByString(keyString);
        }

        /// <summary>
        /// Gets all entries.
        /// </summary>
        public IReadOnlyList<IndexEntry> GetAllEntries() => entries;

        private void EnsureInitialized()
        {
            if (isInitialized)
                return;

            hashToIndices = new Dictionary<int, int[]>(entries.Count);
            stringToIndices = new Dictionary<string, int[]>(entries.Count);

            foreach (var entry in entries)
            {
                hashToIndices[entry.keyHash] = entry.recordIndices;
                if (!string.IsNullOrEmpty(entry.keyString))
                    stringToIndices[entry.keyString] = entry.recordIndices;
            }

            isInitialized = true;
        }

        /// <summary>
        /// Converts a value to a culture-invariant string.
        /// Uses the same conversion logic as IndexBuilder.
        /// float/double use round-trip format to preserve precision.
        /// </summary>
        private static string ConvertToInvariantString(object value)
        {
            return value switch
            {
                float f => f.ToString("R", CultureInfo.InvariantCulture),
                double d => d.ToString("R", CultureInfo.InvariantCulture),
                DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
                SerializableDateTime sdt => sdt.Ticks.ToString(CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }
    }
}
