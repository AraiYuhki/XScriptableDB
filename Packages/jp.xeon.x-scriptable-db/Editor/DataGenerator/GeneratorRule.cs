namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// データ生成ルール。
    /// </summary>
    public enum GeneratorRule
    {
        Sequential,     // 連番
        Random,         // ランダム値
        RandomRange,    // 指定範囲内のランダム値
        RandomChoice,   // 選択肢リストからのランダム選択
        Pattern,        // パターン文字列
        Fixed           // 固定値
    }
}
