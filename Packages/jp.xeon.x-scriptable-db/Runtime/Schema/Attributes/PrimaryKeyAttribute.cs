using System;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// フィールドまたはプロパティをPrimaryKeyとしてマークする属性。
    /// XTableAssetはこの属性を使用してレコードのソートと検索を行う。
    /// </summary>
    /// <remarks>
    /// 1つのレコードクラスには1つのPrimaryKeyのみ指定可能。
    /// 複数指定された場合は最初に見つかったものが使用される。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PrimaryKeyAttribute : Attribute
    {
        /// <summary>
        /// PrimaryKeyの順序（将来の複合キー対応用）。
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// PrimaryKeyAttribute を作成する。
        /// </summary>
        public PrimaryKeyAttribute() { }

        /// <summary>
        /// 順序を指定して PrimaryKeyAttribute を作成する。
        /// </summary>
        /// <param name="order">複合キー時の順序</param>
        public PrimaryKeyAttribute(int order)
        {
            Order = order;
        }
    }
}
