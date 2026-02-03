using System;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// フィールドまたはプロパティをSecondaryKeyとしてマークする属性。
    /// SecondaryKeyはO(1)検索用のハッシュインデックスを構築するために使用される。
    /// </summary>
    /// <remarks>
    /// 1つのレコードクラスに複数のSecondaryKeyを指定可能。
    /// 各SecondaryKeyには一意の名前を付ける必要がある。
    /// 同じ名前を持つ複数のフィールドは複合インデックスとしてグループ化される。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class SecondaryKeyAttribute : Attribute
    {
        /// <summary>
        /// インデックスの名前。指定しない場合はメンバー名が使用される。
        /// 同じ名前を持つ複数のフィールドは複合インデックスとしてグループ化される。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 複合インデックス内でのフィールド順序。
        /// 単一フィールドインデックスの場合は無視される。
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// 同じキー値を持つ複数のレコードを許可するかどうか。
        /// trueの場合、1つのキーに対して複数のレコードがマッピングされる。
        /// </summary>
        public bool AllowDuplicates { get; set; } = true;

        /// <summary>
        /// SecondaryKeyAttribute を作成する。
        /// </summary>
        public SecondaryKeyAttribute()
        {
            Name = null;
        }

        /// <summary>
        /// 名前を指定して SecondaryKeyAttribute を作成する。
        /// </summary>
        /// <param name="name">インデックスの名前</param>
        public SecondaryKeyAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 名前と順序を指定して SecondaryKeyAttribute を作成する。
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
