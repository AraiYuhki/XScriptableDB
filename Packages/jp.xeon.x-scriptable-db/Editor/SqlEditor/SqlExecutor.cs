using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SQLクエリの実行結果。
    /// </summary>
    public class SqlQueryResult
    {
        /// <summary>実行されたSQL文</summary>
        public SqlStatement Statement { get; set; }

        /// <summary>結果レコード（SELECT用）</summary>
        public List<object> Records { get; set; } = new();

        /// <summary>カラム名リスト（SELECT用）</summary>
        public List<string> ColumnNames { get; set; } = new();

        /// <summary>影響を受けたレコード数（UPDATE/DELETE用）</summary>
        public int AffectedCount { get; set; }

        /// <summary>エラーメッセージ</summary>
        public string ErrorMessage { get; set; }

        /// <summary>実行が成功したかどうか</summary>
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);

        /// <summary>実行時間（ミリ秒）</summary>
        public double ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// SELECT結果の行データ。
    /// </summary>
    public class ResultRow
    {
        public object SourceRecord { get; set; }
        public Dictionary<string, object> Values { get; set; } = new();

        public object this[string columnName] =>
            Values.TryGetValue(columnName, out var value) ? value : null;
    }

    /// <summary>
    /// SQLを実行するクラス。
    /// </summary>
    public class SqlExecutor
    {
        private const BindingFlags MemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly Dictionary<string, ITableAsset> tables = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// テーブルを登録する。
        /// </summary>
        /// <param name="tableName">テーブル名</param>
        /// <param name="table">テーブルアセット</param>
        public void RegisterTable(string tableName, ITableAsset table)
        {
            tables[tableName] = table;
        }

        /// <summary>
        /// 登録されているテーブルを取得する。
        /// </summary>
        /// <param name="tableName">テーブル名</param>
        /// <returns>テーブルアセット（見つからない場合はnull）</returns>
        public ITableAsset GetTable(string tableName)
        {
            return tables.TryGetValue(tableName, out var table) ? table : null;
        }

        /// <summary>
        /// 登録されているテーブル名の一覧を取得する。
        /// </summary>
        public IReadOnlyCollection<string> TableNames => tables.Keys;

        /// <summary>
        /// SQL文字列を解析して実行する。
        /// </summary>
        /// <param name="sql">SQL文字列</param>
        /// <returns>実行結果</returns>
        public SqlQueryResult Execute(string sql)
        {
            var startTime = DateTime.Now;
            var result = new SqlQueryResult();

            try
            {
                var parser = new SqlParser();
                var statement = parser.Parse(sql);
                result.Statement = statement;

                switch (statement)
                {
                    case SelectStatement select:
                        ExecuteSelect(select, result);
                        break;
                    case UpdateStatement update:
                        ExecuteUpdate(update, result);
                        break;
                    case DeleteStatement delete:
                        ExecuteDelete(delete, result);
                        break;
                    default:
                        result.ErrorMessage = $"Unsupported statement type: {statement?.GetType().Name}";
                        break;
                }
            }
            catch (SqlParseException ex)
            {
                result.ErrorMessage = $"Parse error: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Execution error: {ex.Message}";
            }

            result.ExecutionTimeMs = (DateTime.Now - startTime).TotalMilliseconds;
            return result;
        }

        /// <summary>
        /// SELECT文を実行する。
        /// </summary>
        private void ExecuteSelect(SelectStatement stmt, SqlQueryResult result)
        {
            if (!tables.TryGetValue(stmt.TableName, out var table))
            {
                result.ErrorMessage = $"Table not found: {stmt.TableName}";
                return;
            }

            var recordType = table.RecordType;
            var records = new List<object>();

            // フィルタリング
            foreach (var record in table.Records)
            {
                if (record == null) continue;

                if (stmt.WhereClause == null || EvaluateExpression(stmt.WhereClause, record, recordType))
                {
                    records.Add(record);
                }
            }

            // ソート
            if (stmt.OrderBy != null && stmt.OrderBy.Count > 0)
            {
                records = SortRecords(records, stmt.OrderBy, recordType);
            }

            // OFFSET
            if (stmt.Offset.HasValue && stmt.Offset.Value > 0)
            {
                records = records.Skip(stmt.Offset.Value).ToList();
            }

            // LIMIT
            if (stmt.Limit.HasValue)
            {
                records = records.Take(stmt.Limit.Value).ToList();
            }

            // カラム名の決定
            result.ColumnNames = GetColumnNames(stmt.Columns, recordType);
            result.Records = records;
        }

        /// <summary>
        /// UPDATE文を実行する。
        /// </summary>
        private void ExecuteUpdate(UpdateStatement stmt, SqlQueryResult result)
        {
            if (!tables.TryGetValue(stmt.TableName, out var table))
            {
                result.ErrorMessage = $"Table not found: {stmt.TableName}";
                return;
            }

            var recordType = table.RecordType;
            var affectedCount = 0;

            foreach (var record in table.Records)
            {
                if (record == null) continue;

                if (stmt.WhereClause == null || EvaluateExpression(stmt.WhereClause, record, recordType))
                {
                    foreach (var setItem in stmt.SetItems)
                    {
                        SetFieldValue(record, recordType, setItem.ColumnName, setItem.Value);
                    }
                    affectedCount++;
                }
            }

            result.AffectedCount = affectedCount;
        }

        /// <summary>
        /// DELETE文を実行する。
        /// </summary>
        private void ExecuteDelete(DeleteStatement stmt, SqlQueryResult result)
        {
            if (!tables.TryGetValue(stmt.TableName, out var table))
            {
                result.ErrorMessage = $"Table not found: {stmt.TableName}";
                return;
            }

            var recordType = table.RecordType;
            var indicesToRemove = new List<int>();
            var index = 0;

            foreach (var record in table.Records)
            {
                if (record != null)
                {
                    if (stmt.WhereClause == null || EvaluateExpression(stmt.WhereClause, record, recordType))
                    {
                        indicesToRemove.Add(index);
                    }
                }
                index++;
            }

            // 後ろから削除してインデックスがずれないようにする
            for (var i = indicesToRemove.Count - 1; i >= 0; i--)
            {
                table.RemoveRecordAt(indicesToRemove[i]);
            }

            result.AffectedCount = indicesToRemove.Count;
        }

        /// <summary>
        /// 式を評価する。
        /// </summary>
        private bool EvaluateExpression(SqlExpression expr, object record, Type recordType)
        {
            switch (expr)
            {
                case LogicalExpression logical:
                    var leftResult = EvaluateExpression(logical.Left, record, recordType);
                    var rightResult = EvaluateExpression(logical.Right, record, recordType);

                    return logical.Operator switch
                    {
                        LogicalOperator.And => leftResult && rightResult,
                        LogicalOperator.Or => leftResult || rightResult,
                        _ => false
                    };

                case ComparisonExpression comparison:
                    return EvaluateComparison(comparison, record, recordType);

                case LiteralExpression literal:
                    // 単独のリテラルはブール値として評価
                    return literal.Value != null && !Equals(literal.Value, false) && !Equals(literal.Value, 0);

                case ColumnExpression column:
                    var value = GetFieldValue(record, recordType, column.ColumnName);
                    return value != null && !Equals(value, false) && !Equals(value, 0);

                default:
                    return false;
            }
        }

        /// <summary>
        /// 比較式を評価する。
        /// </summary>
        private bool EvaluateComparison(ComparisonExpression expr, object record, Type recordType)
        {
            var leftValue = ResolveValue(expr.Left, record, recordType);

            switch (expr.Operator)
            {
                case ComparisonOperator.IsNull:
                    return leftValue == null;

                case ComparisonOperator.IsNotNull:
                    return leftValue != null;

                case ComparisonOperator.In:
                    if (expr.Right is InListExpression inList)
                    {
                        foreach (var item in inList.Values)
                        {
                            var itemValue = ResolveValue(item, record, recordType);
                            if (AreEqual(leftValue, itemValue))
                                return true;
                        }
                    }
                    return false;

                default:
                    var rightValue = ResolveValue(expr.Right, record, recordType);
                    return expr.Operator switch
                    {
                        ComparisonOperator.Equal => AreEqual(leftValue, rightValue),
                        ComparisonOperator.NotEqual => !AreEqual(leftValue, rightValue),
                        ComparisonOperator.LessThan => Compare(leftValue, rightValue) < 0,
                        ComparisonOperator.LessOrEqual => Compare(leftValue, rightValue) <= 0,
                        ComparisonOperator.GreaterThan => Compare(leftValue, rightValue) > 0,
                        ComparisonOperator.GreaterOrEqual => Compare(leftValue, rightValue) >= 0,
                        ComparisonOperator.Like => EvaluateLike(leftValue?.ToString(), rightValue?.ToString()),
                        _ => false
                    };
            }
        }

        /// <summary>
        /// 式から値を取得する。
        /// </summary>
        private object ResolveValue(SqlExpression expr, object record, Type recordType)
        {
            return expr switch
            {
                LiteralExpression literal => literal.Value,
                ColumnExpression column => GetFieldValue(record, recordType, column.ColumnName),
                _ => null
            };
        }

        /// <summary>
        /// 2つの値が等しいかチェックする。
        /// </summary>
        private bool AreEqual(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            // ブールと数値の比較（true=1, false=0）
            if (a is bool boolA && IsNumeric(b))
            {
                return boolA == (Convert.ToDouble(b) != 0);
            }
            if (b is bool boolB && IsNumeric(a))
            {
                return boolB == (Convert.ToDouble(a) != 0);
            }

            // 数値型の比較
            if (IsNumeric(a) && IsNumeric(b))
            {
                return Convert.ToDouble(a) == Convert.ToDouble(b);
            }

            // ブール同士の比較
            if (a is bool && b is bool)
            {
                return a.Equals(b);
            }

            return a.Equals(b) || a.ToString().Equals(b.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 2つの値を比較する。
        /// </summary>
        private int Compare(object a, object b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            // 数値型の比較
            if (IsNumeric(a) && IsNumeric(b))
            {
                return Convert.ToDouble(a).CompareTo(Convert.ToDouble(b));
            }

            if (a is IComparable ca)
            {
                try
                {
                    return ca.CompareTo(b);
                }
                catch
                {
                    // 型が異なる場合は文字列として比較
                }
            }

            return string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal);
        }

        /// <summary>
        /// 値が数値型かチェックする。
        /// </summary>
        private bool IsNumeric(object value)
        {
            return value is byte or sbyte or short or ushort or int or uint
                or long or ulong or float or double or decimal;
        }

        /// <summary>
        /// LIKE演算子を評価する。
        /// </summary>
        private bool EvaluateLike(string value, string pattern)
        {
            if (value == null || pattern == null) return false;

            // SQLのLIKEパターンを正規表現に変換
            // % -> .* (0文字以上の任意の文字列)
            // _ -> . (任意の1文字)
            var regexPattern = "^" +
                               Regex.Escape(pattern)
                                   .Replace("%", ".*")
                                   .Replace("_", ".")
                               + "$";

            return Regex.IsMatch(value, regexPattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// フィールドの値を取得する。
        /// </summary>
        private object GetFieldValue(object record, Type recordType, string fieldName)
        {
            // フィールドを検索
            var field = recordType.GetField(fieldName, MemberFlags);
            if (field != null)
                return field.GetValue(record);

            // 大文字小文字を無視して検索
            field = recordType.GetFields(MemberFlags)
                .FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            if (field != null)
                return field.GetValue(record);

            // プロパティを検索
            var property = recordType.GetProperty(fieldName, MemberFlags);
            if (property?.CanRead == true)
                return property.GetValue(record);

            property = recordType.GetProperties(MemberFlags)
                .FirstOrDefault(p => p.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            if (property?.CanRead == true)
                return property.GetValue(record);

            return null;
        }

        /// <summary>
        /// フィールドの値を設定する。
        /// </summary>
        private void SetFieldValue(object record, Type recordType, string fieldName, SqlExpression valueExpr)
        {
            var value = ResolveValue(valueExpr, record, recordType);

            // フィールドを検索
            var field = recordType.GetField(fieldName, MemberFlags);
            if (field == null)
            {
                field = recordType.GetFields(MemberFlags)
                    .FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            }

            if (field != null)
            {
                var convertedValue = ConvertValue(value, field.FieldType);
                field.SetValue(record, convertedValue);
                return;
            }

            // プロパティを検索
            var property = recordType.GetProperty(fieldName, MemberFlags);
            if (property == null)
            {
                property = recordType.GetProperties(MemberFlags)
                    .FirstOrDefault(p => p.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            }

            if (property?.CanWrite == true)
            {
                var convertedValue = ConvertValue(value, property.PropertyType);
                property.SetValue(record, convertedValue);
            }
        }

        /// <summary>
        /// 値を指定された型に変換する。
        /// </summary>
        private object ConvertValue(object value, Type targetType)
        {
            if (value == null)
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }

            var valueType = value.GetType();
            if (targetType.IsAssignableFrom(valueType))
            {
                return value;
            }

            // 数値型の変換
            if (IsNumeric(value) && IsNumericType(targetType))
            {
                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }

            // 文字列からの変換
            if (value is string str)
            {
                if (targetType == typeof(int) && int.TryParse(str, out var intVal))
                    return intVal;
                if (targetType == typeof(long) && long.TryParse(str, out var longVal))
                    return longVal;
                if (targetType == typeof(float) && float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatVal))
                    return floatVal;
                if (targetType == typeof(double) && double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleVal))
                    return doubleVal;
                if (targetType == typeof(bool) && bool.TryParse(str, out var boolVal))
                    return boolVal;
            }

            try
            {
                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }

        /// <summary>
        /// 型が数値型かチェックする。
        /// </summary>
        private bool IsNumericType(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }

        /// <summary>
        /// レコードをソートする。
        /// </summary>
        private List<object> SortRecords(List<object> records, List<OrderByItem> orderBy, Type recordType)
        {
            IOrderedEnumerable<object> ordered = null;

            for (var i = 0; i < orderBy.Count; i++)
            {
                var item = orderBy[i];
                var columnName = (item.Expression as ColumnExpression)?.ColumnName;
                if (string.IsNullOrEmpty(columnName)) continue;

                Func<object, object> keySelector = r => GetFieldValue(r, recordType, columnName);

                if (i == 0)
                {
                    ordered = item.Order == SortOrder.Descending
                        ? records.OrderByDescending(keySelector, new ObjectComparer())
                        : records.OrderBy(keySelector, new ObjectComparer());
                }
                else
                {
                    ordered = item.Order == SortOrder.Descending
                        ? ordered.ThenByDescending(keySelector, new ObjectComparer())
                        : ordered.ThenBy(keySelector, new ObjectComparer());
                }
            }

            return ordered?.ToList() ?? records;
        }

        /// <summary>
        /// カラム名リストを取得する。
        /// </summary>
        private List<string> GetColumnNames(List<SelectColumn> columns, Type recordType)
        {
            var names = new List<string>();

            foreach (var column in columns)
            {
                if (column.IsWildcard)
                {
                    // *の場合は全フィールドを追加
                    foreach (var field in recordType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        names.Add(field.Name);
                    }
                }
                else if (column.Expression is ColumnExpression colExpr)
                {
                    names.Add(column.Alias ?? colExpr.ColumnName);
                }
            }

            return names;
        }

        /// <summary>
        /// オブジェクト比較用のComparer。
        /// </summary>
        private class ObjectComparer : IComparer<object>
        {
            public int Compare(object x, object y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                if (x is IComparable cx)
                {
                    try
                    {
                        return cx.CompareTo(y);
                    }
                    catch
                    {
                        // 型が異なる場合
                    }
                }

                return string.Compare(x.ToString(), y.ToString(), StringComparison.Ordinal);
            }
        }
    }
}
