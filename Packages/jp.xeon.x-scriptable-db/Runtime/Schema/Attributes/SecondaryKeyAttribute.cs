using System;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// フィールドまたはプロパティをSecondaryKeyとしてマークする属性。
    /// SecondaryKeyは、O(1)でルックアップするためのハッシュインデックスの構築に使用されます。
    /// </summary>
    /// <remarks>
    /// 単一のレコードクラスに複数のSecondaryKeyを指定できます。
    /// 各SecondaryKeyは一意の名前を持つ必要があります。
    /// 同じ名前を持つ複数のフィールドは、複合インデックスとしてグループ化されます。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class SecondaryKeyAttribute : Attribute
    {
        /// <summary>
        /// インデックスの名前。指定されていない場合は、メンバー名が使用されます。
        /// 同じ名前を共有する複数のフィールドは、複合インデックスとしてグループ化されます。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 複合インデックス内のフィールドの順序。
        /// 単一フィールドインデックスの場合は無視されます。
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// 同じキー値を持つ複数のレコードを許可するかどうか。
        /// trueの場合、複数のレコードを単一のキーにマッピングできます。
        /// </summary>
        public bool AllowDuplicates { get; set; } = true;

        /// <summary>
        /// SecondaryKeyAttributeを作成します。
        /// </summary>
        public SecondaryKeyAttribute()
        {
            Name = null;
        }

        /// <summary>
        /// 指定された名前でSecondaryKeyAttributeを作成します。
        /// </summary>
        /// <param name="name">インデックスの名前</param>
        public SecondaryKeyAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 指定された名前と順序でSecondaryKeyAttributeを作成します。
        /// </summary>
        /// <param name="name">インデックスの名前</param>
        /// <param name="order">複合インデックス内の順序</param>
        public SecondaryKeyAttribute(string name, int order)
        {
            Name = name;
            Order = order;
        }
    }
}
