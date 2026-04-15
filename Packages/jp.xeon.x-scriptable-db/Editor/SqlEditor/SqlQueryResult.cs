using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// The execution result of a SQL query.
    /// </summary>
    public class SqlQueryResult
    {
        /// <summary>The executed SQL statement</summary>
        public SqlStatement Statement { get; set; }

        /// <summary>Result records (for SELECT)</summary>
        public List<object> Records { get; set; } = new();

        /// <summary>Column name list (for SELECT)</summary>
        public List<string> ColumnNames { get; set; } = new();

        /// <summary>Number of affected records (for UPDATE/DELETE)</summary>
        public int AffectedCount { get; set; }

        /// <summary>Error message</summary>
        public string ErrorMessage { get; set; }

        /// <summary>Whether execution was successful</summary>
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);

        /// <summary>Execution time (milliseconds)</summary>
        public double ExecutionTimeMs { get; set; }
    }
}