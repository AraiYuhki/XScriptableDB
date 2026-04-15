using System;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// Attribute to mark a field or property as a SecondaryKey.
    /// SecondaryKey is used to build a hash index for O(1) lookups.
    /// </summary>
    /// <remarks>
    /// Multiple SecondaryKeys may be specified on a single record class.
    /// Each SecondaryKey must have a unique name.
    /// Multiple fields with the same name are grouped as a composite index.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class SecondaryKeyAttribute : Attribute
    {
        /// <summary>
        /// Name of the index. If not specified, the member name is used.
        /// Multiple fields sharing the same name are grouped as a composite index.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Field order within a composite index.
        /// Ignored for single-field indexes.
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// Whether to allow multiple records with the same key value.
        /// When true, multiple records can be mapped to a single key.
        /// </summary>
        public bool AllowDuplicates { get; set; } = true;

        /// <summary>
        /// Creates a SecondaryKeyAttribute.
        /// </summary>
        public SecondaryKeyAttribute()
        {
            Name = null;
        }

        /// <summary>
        /// Creates a SecondaryKeyAttribute with a specified name.
        /// </summary>
        /// <param name="name">Name of the index</param>
        public SecondaryKeyAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Creates a SecondaryKeyAttribute with a specified name and order.
        /// </summary>
        /// <param name="name">Name of the index</param>
        /// <param name="order">Order within the composite index</param>
        public SecondaryKeyAttribute(string name, int order)
        {
            Name = name;
            Order = order;
        }
    }
}
