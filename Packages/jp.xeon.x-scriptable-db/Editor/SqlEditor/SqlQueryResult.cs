using System.Collections.Generic;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SQLクエリの実行結果。
    /// </summary>
    public class SqlQueryResult
    {
        /// <summary>実行されたSQLステートメント</summary>
        public SqlStatement Statement { get; set; }

        /// <summary>結果レコード（SELECT用）</summary>
        public List<object> Records { get; set; } = new();

        /// <summary>列名リスト（SELECT用）</summary>
        public List<string> ColumnNames { get; set; } = new();

        /// <summary>影響を受けるレコードの数（UPDATE/DELETE用）</summary>
        public int AffectedCount { get; set; }

        /// <summary>エラーメッセージ</summary>
        public string ErrorMessage { get; set; }

        /// <summary>実行が成功したかどうか</summary>
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);

        /// <summary>実行時間（ミリ秒）</summary>
        public double ExecutionTimeMs { get; set; }
    }
}