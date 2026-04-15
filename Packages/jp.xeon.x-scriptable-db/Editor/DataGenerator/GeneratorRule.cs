namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Data generation rules.
    /// </summary>
    public enum GeneratorRule
    {
        Sequential,     // Sequential numbering
        Random,         // Random value
        RandomRange,    // Random value within a specified range
        RandomChoice,   // Random choice from a list of options
        Pattern,        // Pattern string
        Fixed           // Fixed value
    }
}
