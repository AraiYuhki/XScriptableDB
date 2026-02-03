namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SQLトークンの種類。
    /// </summary>
    public enum TokenType
    {
        // Keywords
        Select, From, Where, And, Or, Not,
        Order, By, Asc, Desc, Limit, Offset,
        Update, Set, Delete, Insert, Into, Values,
        In, Like, Is, Null, Between,

        // JOIN keywords
        Join, Inner, Left, Right, Cross, Outer, On,

        // Aggregate keywords
        Group, Having, Distinct,
        Count, Sum, Avg, Min, Max,

        // Set operations
        Union, Intersect, Except, All,

        // CASE expression
        Case, When, Then, Else, End,

        // String functions
        Upper, Lower, Concat, Substring, Trim, Length,

        // Symbols
        Star, Comma, Dot, LeftParen, RightParen,
        Equal, NotEqual, LessThan, LessOrEqual, GreaterThan, GreaterOrEqual,
        Plus, Minus, Slash, Percent,

        // Literals
        Identifier, StringLiteral, NumberLiteral,

        // Special
        Eof, Unknown
    }
}
