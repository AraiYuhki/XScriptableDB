using System;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// クラスに複合インデックスを定義する属性。
    /// 複数のフィールド/プロパティを組み合わせたインデックスを作成する。
    /// </summary>
    /// <remarks>
    /// クラスに対して複数の CompositeIndexAttribute を指定可能。
    /// 各複合インデックスは一意の名前を持つ必要がある。
    /// </remarks>
    /// <example>
    /// <code>
    /// [CompositeIndex("CategoryPrice", nameof(category), nameof(price))]
    /// [CompositeIndex("TypeRarity", nameof(type), nameof(rarity))]
    /// public class ItemRecord
    /// {
    ///     [SerializeField] private int category;
    ///     [SerializeField] private int price;
    ///     [SerializeField] private string type;
    ///     [SerializeField] private int rarity;
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class CompositeIndexAttribute : Attribute
    {
        /// <summary>
        /// 複合インデックスの名前。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// インデックスを構成するフィールド/プロパティの名前（順序付き）。
        /// </summary>
        public string[] MemberNames { get; }

        /// <summary>
        /// 同じキー組み合わせを持つ複数のレコードを許可するかどうか。
        /// </summary>
        public bool AllowDuplicates { get; set; } = true;

        /// <summary>
        /// CompositeIndexAttribute を作成する。
        /// </summary>
        /// <param name="name">インデックスの名前</param>
        /// <param name="memberNames">インデックスを構成するメンバー名（2つ以上）</param>
        /// <exception cref="ArgumentException">メンバー名が2つ未満の場合</exception>
        public CompositeIndexAttribute(string name, params string[] memberNames)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Composite index name cannot be null or empty", nameof(name));

            if (memberNames == null || memberNames.Length < 2)
                throw new ArgumentException("Composite index requires at least 2 member names", nameof(memberNames));

            Name = name;
            MemberNames = memberNames;
        }
    }
}
