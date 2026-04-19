namespace Xeon.XScriptableDB
{
    /// <summary>
    /// TableAssetで管理されるレコードの基本インターフェース。
    /// [PrimaryKey]属性と組み合わせることで、自動的なキー管理が利用可能になります。
    /// </summary>
    public interface IRecord
    {
        /// <summary>
        /// レコードのPrimaryKey値をオブジェクトとして返します。
        /// リフレクションを使用しない高速なアクセスが必要な場合に実装してください。
        /// 実装されていない場合、[PrimaryKey]属性を使用してリフレクション経由で値にアクセスします。
        /// </summary>
        object GetPrimaryKeyValue() => null;
    }

    /// <summary>
    /// 型安全なPrimaryKeyアクセスを提供するレコードインターフェース。
    /// IPrimaryKey&lt;TKey&gt;と同等ですが、IRecordを継承します。
    /// </summary>
    /// <typeparam name="TKey">PrimaryKeyの型</typeparam>
    public interface IRecord<TKey> : IRecord
    {
        /// <summary>
        /// レコードのPrimaryKey値。
        /// </summary>
        TKey PrimaryKey { get; }

        object IRecord.GetPrimaryKeyValue() => PrimaryKey;
    }
}
