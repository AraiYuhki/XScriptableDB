namespace Xeon.XScriptableDB
{
    public interface IPrimaryKey<TKey>
    {
        TKey PrimaryKey { get; }
    }
}
