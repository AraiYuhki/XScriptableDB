using System;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SQL parse error.
    /// </summary>
    public class SqlParseException : Exception
    {
        public int Position { get; }

        public SqlParseException(string message, int position)
            : base($"{message} at position {position}")
        {
            Position = position;
        }
    }
}
