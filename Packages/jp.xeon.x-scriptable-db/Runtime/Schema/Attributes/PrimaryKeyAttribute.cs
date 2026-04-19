using System;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// フィールドまたはプロパティをPrimaryKeyとしてマークする属性。
    /// XTableAssetはこの属性を使用してレコードのソートとルックアップを行います。
    /// </summary>
    /// <remarks>
    /// レコードクラスごとに指定できるPrimaryKeyは1つだけです。
    /// 複数が指定されている場合は、最初に見つかったものが使用されます。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PrimaryKeyAttribute : Attribute
    {
        /// <summary>
        /// PrimaryKeyの順序（将来の複合キーのサポートのために予約されています）。
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// PrimaryKeyAttributeを作成します。
        /// </summary>
        public PrimaryKeyAttribute() { }

        /// <summary>
        /// 指定された順序でPrimaryKeyAttributeを作成します。
        /// </summary>
        /// <param name="order">複合キー内の順序</param>
        public PrimaryKeyAttribute(int order)
        {
            Order = order;
        }
    }
}
