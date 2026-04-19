namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SQLトークンの種類。
    /// </summary>
    public enum TokenType
    {
        // キーワード
        Select, From, Where, And, Or, Not,
        Order, By, Asc, Desc, Limit, Offset,
        Update, Set, Delete, Insert, Into, Values,
        In, Like, Is, Null, Between,

        // JOINキーワード
        Join, Inner, Left, Right, Cross, Outer, On,

        // 集約キーワード
        Group, Having, Distinct,
        Count, Sum, Avg, Min, Max,

        // セット操作
        Union, Intersect, Except, All,

        // CASE式
        Case, When, Then, Else, End,

        // 文字列関数
        Upper, Lower, Concat, Substring, Trim, Length,

        // 記号
        Star, Comma, Dot, LeftParen, RightParen,
        Equal, NotEqual, LessThan, LessOrEqual, GreaterThan, GreaterOrEqual,
        Plus, Minus, Slash, Percent,

        // リテラル
        Identifier, StringLiteral, NumberLiteral,

        // 特殊
        Eof, Unknown
    }
}
