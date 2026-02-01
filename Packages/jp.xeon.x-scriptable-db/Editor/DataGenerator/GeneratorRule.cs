namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// データ生成ルール。
    /// </summary>
    public enum GeneratorRule
    {
        Sequential,     // 連番
        Random,         // ランダム
        RandomRange,    // 範囲指定ランダム
        RandomChoice,   // 選択肢からランダム
        Pattern,        // パターン文字列
        Fixed           // 固定値
    }
}
