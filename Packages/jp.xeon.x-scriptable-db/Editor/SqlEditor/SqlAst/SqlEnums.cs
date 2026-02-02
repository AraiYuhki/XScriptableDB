namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SQL文の種類。
    /// </summary>
    public enum SqlStatementType
    {
        Select,
        Update,
        Delete
    }

    /// <summary>
    /// JOIN の種類。
    /// </summary>
    public enum JoinType
    {
        Inner,
        Left,
        Right,
        Cross
    }

    /// <summary>
    /// 集計関数の種類。
    /// </summary>
    public enum AggregateFunction
    {
        Count,
        Sum,
        Avg,
        Min,
        Max
    }

    /// <summary>
    /// 算術演算子。
    /// </summary>
    public enum ArithmeticOperator
    {
        Add,        // +
        Subtract,   // -
        Multiply,   // *
        Divide,     // /
        Modulo      // %
    }

    /// <summary>
    /// 比較演算子。
    /// </summary>
    public enum ComparisonOperator
    {
        Equal,          // =
        NotEqual,       // != or <>
        LessThan,       // <
        LessOrEqual,    // <=
        GreaterThan,    // >
        GreaterOrEqual, // >=
        Like,           // LIKE
        In,             // IN
        IsNull,         // IS NULL
        IsNotNull       // IS NOT NULL
    }

    /// <summary>
    /// 論理演算子。
    /// </summary>
    public enum LogicalOperator
    {
        And,
        Or
    }

    /// <summary>
    /// ソート順。
    /// </summary>
    public enum SortOrder
    {
        Ascending,
        Descending
    }
}
