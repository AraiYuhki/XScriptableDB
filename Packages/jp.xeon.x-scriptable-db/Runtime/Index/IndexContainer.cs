using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// テーブルの全SecondaryKeyインデックスを保持するコンテナ。
    /// </summary>
    [Serializable]
    public class IndexContainer
    {
        [SerializeField]
        private List<IndexData> indices = new();

        /// <summary>
        /// インデックス数。
        /// </summary>
        public int Count => indices.Count;

        /// <summary>
        /// 名前でインデックスを取得する。
        /// </summary>
        /// <param name="name">インデックス名</param>
        /// <returns>インデックスデータ、見つからない場合はnull</returns>
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
        /// インデックスを追加または更新する。
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
        /// 指定した名前のインデックスを削除する。
        /// </summary>
        /// <param name="name">削除するインデックス名</param>
        /// <returns>削除に成功した場合はtrue</returns>
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
        /// 全インデックスをクリアする。
        /// </summary>
        public void Clear()
        {
            indices.Clear();
        }

        /// <summary>
        /// 全インデックスを取得する。
        /// </summary>
        public IReadOnlyList<IndexData> GetAllIndices() => indices;
    }
}