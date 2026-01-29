using System;
using System.Collections;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// XTableAssetの非ジェネリックインターフェース。
    /// Editor等で型を知らずにテーブルを操作する場合に使用する。
    /// </summary>
    public interface IXTableAsset
    {
        /// <summary>
        /// 全レコードを取得する。
        /// </summary>
        IEnumerable Records { get; }

        /// <summary>
        /// レコード数。
        /// </summary>
        int Count { get; }

        /// <summary>
        /// レコードの型。
        /// </summary>
        Type RecordType { get; }

        /// <summary>
        /// PrimaryKeyの型。
        /// </summary>
        Type KeyType { get; }

#if UNITY_EDITOR
        /// <summary>
        /// 新しい空のレコードを作成する。
        /// </summary>
        object CreateNewRecord();

        /// <summary>
        /// レコードを追加する。
        /// </summary>
        void AddRecordObject(object record);

        /// <summary>
        /// 指定インデックスのレコードを削除する。
        /// </summary>
        void RemoveRecordAt(int index);

        /// <summary>
        /// PrimaryKeyの重複をチェックする。
        /// </summary>
        /// <returns>重複しているキーのリスト（object型）</returns>
        IList FindDuplicateKeysAsObjects();
#endif
    }
}
