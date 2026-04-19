using System;
using System.Collections;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// TableAssetの非ジェネリックインターフェース。
    /// エディタコードなどで、具体的な型を知らずにテーブルを操作する場合に使用します。
    /// </summary>
    public interface ITableAsset
    {
        /// <summary>
        /// すべてのレコードを返します。
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
        /// 新しい空のレコードを作成します。
        /// </summary>
        object CreateNewRecord();

        /// <summary>
        /// レコードを追加します。
        /// </summary>
        void AddRecordObject(object record);

        /// <summary>
        /// 指定されたインデックスのレコードを削除します。
        /// </summary>
        void RemoveRecordAt(int index);

        /// <summary>
        /// 重複するPrimaryKeyをチェックします。
        /// </summary>
        /// <returns>重複キーのリスト（オブジェクト型）</returns>
        IList FindDuplicateKeysAsObjects();
#endif
    }
}
