namespace Xeon.XScriptableDB
{
    /// <summary>
    /// TableAssetで管理されるレコードの基本インターフェース。
    /// [PrimaryKey]属性と組み合わせて使用することで、自動的なキー管理が可能になる。
    /// </summary>
    public interface IRecord
    {
        /// <summary>
        /// レコードのPrimaryKey値をobjectとして取得する。
        /// リフレクションを使用しない高速アクセスが必要な場合に実装する。
        /// 実装しない場合は[PrimaryKey]属性を使用してリフレクション経由でアクセスされる。
        /// </summary>
        object GetPrimaryKeyValue() => null;
    }

    /// <summary>
    /// 型安全なPrimaryKeyアクセスを提供するレコードインターフェース。
    /// IPrimaryKey&lt;TKey&gt;と同等だが、IRecordを継承している。
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
