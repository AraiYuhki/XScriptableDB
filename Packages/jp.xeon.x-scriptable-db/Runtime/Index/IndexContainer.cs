using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// テーブルのすべてのSecondaryKeyインデックスを保持するコンテナ。
    /// </summary>
    [Serializable]
    public class IndexContainer
    {
        [SerializeField]
        private List<IndexData> indices = new();

        /// <summary>
        /// インデックスの数。
        /// </summary>
        public int Count => indices.Count;

        /// <summary>
        /// 指定された名前のインデックスを返します。
        /// </summary>
        /// <param name="name">インデックス名</param>
        /// <returns>インデックスデータ。見つからない場合はnull</returns>
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
        /// インデックスを追加または更新します。
        /// </summary>
        /// <param name="indexData">追加するインデックスデータ</param>
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
        /// 指定された名前のインデックスを削除します。
        /// </summary>
        /// <param name="name">削除するインデックスの名前</param>
        /// <returns>削除が成功した場合はtrue</returns>
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
        /// すべてのインデックスをクリアします。
        /// </summary>
        public void Clear()
        {
            indices.Clear();
        }

        /// <summary>
        /// すべてのインデックスを返します。
        /// </summary>
        public IReadOnlyList<IndexData> GetAllIndices() => indices;
    }
}