using System;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// Index entry.
    /// </summary>
    [Serializable]
    public class IndexEntry
    {
        /// <summary>
        /// Hash value of the key.
        /// </summary>
        public int keyHash;

        /// <summary>
        /// String representation of the key (for debugging and string-based lookup).
        /// </summary>
        public string keyString;

        /// <summary>
        /// Array of record indices corresponding to this key.
        /// </summary>
        public int[] recordIndices;
    }
}