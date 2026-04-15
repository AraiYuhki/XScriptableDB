using System;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// Attribute to mark a field or property as a PrimaryKey.
    /// XTableAsset uses this attribute to sort and look up records.
    /// </summary>
    /// <remarks>
    /// Only one PrimaryKey may be specified per record class.
    /// If multiple are specified, the first one found is used.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PrimaryKeyAttribute : Attribute
    {
        /// <summary>
        /// Order of the PrimaryKey (reserved for future composite key support).
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// Creates a PrimaryKeyAttribute.
        /// </summary>
        public PrimaryKeyAttribute() { }

        /// <summary>
        /// Creates a PrimaryKeyAttribute with a specified order.
        /// </summary>
        /// <param name="order">Order within a composite key</param>
        public PrimaryKeyAttribute(int order)
        {
            Order = order;
        }
    }
}
