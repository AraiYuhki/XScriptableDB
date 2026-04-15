using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// Container that holds all SecondaryKey indexes for a table.
    /// </summary>
    [Serializable]
    public class IndexContainer
    {
        [SerializeField]
        private List<IndexData> indices = new();

        /// <summary>
        /// Number of indexes.
        /// </summary>
        public int Count => indices.Count;

        /// <summary>
        /// Returns the index with the specified name.
        /// </summary>
        /// <param name="name">Index name</param>
        /// <returns>Index data, or null if not found</returns>
        public IndexData GetIndex(string name)
        {
            foreach (var index in indices)
            {
                if (index.IndexName == name)
                    return index;
            }
            return null;
        }

        /// <summary>
        /// Adds or updates an index.
        /// </summary>
        /// <param name="indexData">Index data to add</param>
        public void SetIndex(IndexData indexData)
        {
            for (var i = 0; i < indices.Count; i++)
            {
                if (indices[i].IndexName == indexData.IndexName)
                {
                    indices[i] = indexData;
                    return;
                }
            }
            indices.Add(indexData);
        }

        /// <summary>
        /// Removes the index with the specified name.
        /// </summary>
        /// <param name="name">Name of the index to remove</param>
        /// <returns>True if removal succeeded</returns>
        public bool RemoveIndex(string name)
        {
            for (var i = 0; i < indices.Count; i++)
            {
                if (indices[i].IndexName == name)
                {
                    indices.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Clears all indexes.
        /// </summary>
        public void Clear()
        {
            indices.Clear();
        }

        /// <summary>
        /// Returns all indexes.
        /// </summary>
        public IReadOnlyList<IndexData> GetAllIndices() => indices;
    }
}