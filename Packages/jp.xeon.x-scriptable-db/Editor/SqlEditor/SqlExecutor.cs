using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xeon.XScriptableDB;

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
    /// JOIN結果のレコード。
    /// </summary>
    public class JoinedRecord
    {
        /// <summary>テーブル名/エイリアス → レコードのマップ</summary>
        public Dictionary<string, object> TableRecords { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>テーブル名/エイリアス → レコード型のマップ</summary>
        public Dictionary<string, Type> TableTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public object GetRecord(string tableAlias) =>
            TableRecords.TryGetValue(tableAlias, out var record) ? record : null;

        public Type GetRecordType(string tableAlias) =>
            TableTypes.TryGetValue(tableAlias, out var type) ? type : null;
    }

    /// <summary>
    /// 集計結果のグループ。
    /// </summary>
    public class AggregateGroup
    {
        public List<object> GroupKeyValues { get; set; } = new();
        public List<object> Records { get; set; } = new();
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
            // メインテーブルの取得
            var mainTableName = stmt.FromTable?.TableName ?? stmt.TableName;
            if (!tables.TryGetValue(mainTableName, out var mainTable))
            {
                result.ErrorMessage = $"Table not found: {mainTableName}";
                return;
            }

            var mainAlias = stmt.FromTable?.Alias ?? mainTableName;

            // JOINがある場合
            if (stmt.Joins.Count > 0)
            {
                ExecuteSelectWithJoin(stmt, mainTable, mainAlias, result);
                return;
            }

            // GROUP BY がある場合
            if (stmt.GroupBy.Count > 0 || HasAggregateFunction(stmt.Columns))
            {
                ExecuteSelectWithGroupBy(stmt, mainTable, result);
                return;
            }

            // 通常のSELECT
            var recordType = mainTable.RecordType;
            var records = new List<object>();

            // フィルタリング
            foreach (var record in mainTable.Records)
            {
                if (record == null)
                    continue;

                if (stmt.WhereClause == null || EvaluateExpression(stmt.WhereClause, record, recordType))
                    records.Add(record);
            }

            // ソート
            if (stmt.OrderBy != null && stmt.OrderBy.Count > 0)
                records = SortRecords(records, stmt.OrderBy, recordType);

            // DISTINCT
            if (stmt.IsDistinct)
            {
                // 特定カラムのみのDISTINCTの場合、ResultRowを返す
                var hasSpecificColumns = stmt.Columns.Any(c => !c.IsWildcard);
                if (hasSpecificColumns)
                {
                    var distinctRows = ApplyDistinctAsResultRows(records, stmt.Columns, recordType);

                    // ソート
                    if (stmt.OrderBy != null && stmt.OrderBy.Count > 0)
                        distinctRows = SortResultRows(distinctRows, stmt.OrderBy);

                    // OFFSET
                    if (stmt.Offset.HasValue && stmt.Offset.Value > 0)
                        distinctRows = distinctRows.Skip(stmt.Offset.Value).ToList();

                    // LIMIT
                    if (stmt.Limit.HasValue)
                        distinctRows = distinctRows.Take(stmt.Limit.Value).ToList();

                    result.ColumnNames = GetColumnNames(stmt.Columns, recordType);
                    result.Records = distinctRows.Cast<object>().ToList();
                    return;
                }
                records = ApplyDistinct(records, stmt.Columns, recordType);
            }

            // OFFSET
            if (stmt.Offset.HasValue && stmt.Offset.Value > 0)
                records = records.Skip(stmt.Offset.Value).ToList();

            // LIMIT
            if (stmt.Limit.HasValue)
                records = records.Take(stmt.Limit.Value).ToList();

            // カラム名の決定
            result.ColumnNames = GetColumnNames(stmt.Columns, recordType);

            // 算術演算や関数呼び出しがある場合はResultRowを構築する
            if (NeedsResultRowProjection(stmt.Columns))
            {
                var resultRows = new List<ResultRow>();
                foreach (var record in records)
                {
                    var row = new ResultRow { SourceRecord = record };
                    foreach (var col in stmt.Columns)
                    {
                        if (col.IsWildcard)
                        {
                            // ワイルドカードの場合は全フィールドを追加
                            foreach (var field in ReflectionUtility.GetSerializableFields(recordType))
                            {
                                row.Values[field.Name] = field.GetValue(record);
                            }
                        }
                        else
                        {
                            var colName = col.Alias ?? GetExpressionName(col.Expression);
                            row.Values[colName] = ResolveValue(col.Expression, record, recordType);
                        }
                    }
                    resultRows.Add(row);
                }
                result.Records = resultRows.Cast<object>().ToList();
            }
            else
            {
                result.Records = records;
            }
        }

        /// <summary>
        /// カラムリストにResultRowへの射影が必要な式が含まれているかチェックする。
        /// </summary>
        private bool NeedsResultRowProjection(List<SelectColumn> columns)
        {
            foreach (var col in columns)
            {
                if (col.IsWildcard)
                    continue;

                switch (col.Expression)
                {
                    case ArithmeticExpression:
                    case CaseExpression:
                    case FunctionCallExpression:
                        return true;
                }

                // エイリアスがある場合もResultRowが必要
                if (!string.IsNullOrEmpty(col.Alias))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// JOINを含むSELECTを実行する。
        /// </summary>
        private void ExecuteSelectWithJoin(SelectStatement stmt, ITableAsset mainTable, string mainAlias, SqlQueryResult result)
        {
            // JOIN対象のテーブルを取得
            var joinTables = new List<(ITableAsset table, string alias, JoinClause clause)>();
            foreach (var join in stmt.Joins)
            {
                if (!tables.TryGetValue(join.TableName, out var joinTable))
                {
                    result.ErrorMessage = $"Table not found: {join.TableName}";
                    return;
                }
                var alias = join.Alias ?? join.TableName;
                joinTables.Add((joinTable, alias, join));
            }

            // JOINの実行
            var joinedRecords = new List<JoinedRecord>();

            foreach (var mainRecord in mainTable.Records)
            {
                if (mainRecord == null)
                    continue;

                var currentResults = new List<JoinedRecord>
                {
                    new JoinedRecord
                    {
                        TableRecords = { [mainAlias] = mainRecord },
                        TableTypes = { [mainAlias] = mainTable.RecordType }
                    }
                };

                foreach (var (joinTable, joinAlias, joinClause) in joinTables)
                {
                    var nextResults = new List<JoinedRecord>();

                    foreach (var currentRecord in currentResults)
                    {
                        var matched = false;

                        foreach (var joinRecord in joinTable.Records)
                        {
                            if (joinRecord == null)
                                continue;

                            // ON条件の評価
                            var testRecord = new JoinedRecord
                            {
                                TableRecords = new Dictionary<string, object>(currentRecord.TableRecords, StringComparer.OrdinalIgnoreCase)
                                {
                                    [joinAlias] = joinRecord
                                },
                                TableTypes = new Dictionary<string, Type>(currentRecord.TableTypes, StringComparer.OrdinalIgnoreCase)
                                {
                                    [joinAlias] = joinTable.RecordType
                                }
                            };

                            if (joinClause.JoinType == JoinType.Cross ||
                                EvaluateJoinCondition(joinClause.OnCondition, testRecord))
                            {
                                nextResults.Add(testRecord);
                                matched = true;
                            }
                        }

                        // LEFT JOINでマッチしなかった場合
                        if (!matched && joinClause.JoinType == JoinType.Left)
                        {
                            var nullRecord = new JoinedRecord
                            {
                                TableRecords = new Dictionary<string, object>(currentRecord.TableRecords, StringComparer.OrdinalIgnoreCase)
                                {
                                    [joinAlias] = null
                                },
                                TableTypes = new Dictionary<string, Type>(currentRecord.TableTypes, StringComparer.OrdinalIgnoreCase)
                                {
                                    [joinAlias] = joinTable.RecordType
                                }
                            };
                            nextResults.Add(nullRecord);
                        }
                    }

                    currentResults = nextResults;
                }

                joinedRecords.AddRange(currentResults);
            }

            // RIGHT JOINの処理
            foreach (var (joinTable, joinAlias, joinClause) in joinTables)
            {
                if (joinClause.JoinType != JoinType.Right)
                    continue;

                foreach (var joinRecord in joinTable.Records)
                {
                    if (joinRecord == null)
                        continue;

                    var hasMatch = joinedRecords.Any(jr =>
                        jr.TableRecords.TryGetValue(joinAlias, out var rec) && rec != null &&
                        ReferenceEquals(rec, joinRecord));

                    if (!hasMatch)
                    {
                        var nullRecord = new JoinedRecord
                        {
                            TableRecords = { [mainAlias] = null, [joinAlias] = joinRecord },
                            TableTypes = { [mainAlias] = mainTable.RecordType, [joinAlias] = joinTable.RecordType }
                        };
                        joinedRecords.Add(nullRecord);
                    }
                }
            }

            // WHERE句の適用
            if (stmt.WhereClause != null)
            {
                joinedRecords = joinedRecords
                    .Where(jr => EvaluateJoinCondition(stmt.WhereClause, jr))
                    .ToList();
            }

            // GROUP BY がある場合
            if (stmt.GroupBy.Count > 0 || HasAggregateFunction(stmt.Columns))
            {
                ExecuteJoinedGroupBy(stmt, joinedRecords, result);
                return;
            }

            // 結果の構築
            var resultRecords = new List<object>();
            var columnNames = new List<string>();

            // カラム名の決定
            foreach (var col in stmt.Columns)
            {
                if (col.IsWildcard)
                {
                    columnNames.Add(mainAlias + ".*");
                    foreach (var (_, alias, _) in joinTables)
                        columnNames.Add(alias + ".*");
                }
                else
                {
                    var colName = GetJoinedColumnName(col);
                    columnNames.Add(col.Alias ?? colName);
                }
            }

            result.ColumnNames = columnNames;
            result.Records = joinedRecords.Cast<object>().ToList();
        }

        /// <summary>
        /// GROUP BYを含むSELECTを実行する。
        /// </summary>
        private void ExecuteSelectWithGroupBy(SelectStatement stmt, ITableAsset table, SqlQueryResult result)
        {
            var recordType = table.RecordType;
            var records = new List<object>();

            // WHEREフィルタリング
            foreach (var record in table.Records)
            {
                if (record == null)
                    continue;

                if (stmt.WhereClause == null || EvaluateExpression(stmt.WhereClause, record, recordType))
                    records.Add(record);
            }

            // グルーピング
            var groups = new Dictionary<string, AggregateGroup>();

            foreach (var record in records)
            {
                var keyValues = new List<object>();
                foreach (var groupItem in stmt.GroupBy)
                {
                    var value = ResolveValue(groupItem.Expression, record, recordType);
                    keyValues.Add(value);
                }

                var key = string.Join("|", keyValues.Select(v => v?.ToString() ?? "NULL"));

                if (!groups.TryGetValue(key, out var group))
                {
                    group = new AggregateGroup { GroupKeyValues = keyValues };
                    groups[key] = group;
                }
                group.Records.Add(record);
            }

            // GROUP BY がない場合は全レコードを1グループに
            if (stmt.GroupBy.Count == 0)
                groups[""] = new AggregateGroup { Records = records };

            // HAVING フィルタリング
            var filteredGroups = groups.Values.ToList();
            if (stmt.HavingClause != null)
            {
                filteredGroups = filteredGroups
                    .Where(g => EvaluateHavingCondition(stmt.HavingClause, g, recordType))
                    .ToList();
            }

            // 結果の構築
            var resultRows = new List<ResultRow>();

            foreach (var group in filteredGroups)
            {
                var row = new ResultRow();
                var keyIndex = 0;

                foreach (var col in stmt.Columns)
                {
                    if (col.IsWildcard)
                        continue;

                    var colName = col.Alias ?? GetExpressionName(col.Expression);
                    object value;

                    if (col.Expression is AggregateExpression aggExpr)
                    {
                        value = EvaluateAggregate(aggExpr, group.Records, recordType);
                    }
                    else if (col.Expression is ColumnExpression && keyIndex < group.GroupKeyValues.Count)
                    {
                        value = group.GroupKeyValues[keyIndex++];
                    }
                    else
                    {
                        value = group.Records.Count > 0
                            ? ResolveValue(col.Expression, group.Records[0], recordType)
                            : null;
                    }

                    row.Values[colName] = value;
                }

                resultRows.Add(row);
            }

            // ORDER BY
            if (stmt.OrderBy != null && stmt.OrderBy.Count > 0)
                resultRows = SortResultRows(resultRows, stmt.OrderBy);

            // OFFSET/LIMIT
            if (stmt.Offset.HasValue && stmt.Offset.Value > 0)
                resultRows = resultRows.Skip(stmt.Offset.Value).ToList();

            if (stmt.Limit.HasValue)
                resultRows = resultRows.Take(stmt.Limit.Value).ToList();

            result.ColumnNames = stmt.Columns
                .Where(c => !c.IsWildcard)
                .Select(c => c.Alias ?? GetExpressionName(c.Expression))
                .ToList();
            result.Records = resultRows.Cast<object>().ToList();
        }

        /// <summary>
        /// JOIN結果に対してGROUP BYを実行する。
        /// </summary>
        private void ExecuteJoinedGroupBy(SelectStatement stmt, List<JoinedRecord> joinedRecords, SqlQueryResult result)
        {
            var groups = new Dictionary<string, List<JoinedRecord>>();

            foreach (var jr in joinedRecords)
            {
                var keyValues = new List<object>();
                foreach (var groupItem in stmt.GroupBy)
                {
                    var value = ResolveJoinedValue(groupItem.Expression, jr);
                    keyValues.Add(value);
                }

                var key = string.Join("|", keyValues.Select(v => v?.ToString() ?? "NULL"));

                if (!groups.TryGetValue(key, out var groupList))
                {
                    groupList = new List<JoinedRecord>();
                    groups[key] = groupList;
                }
                groupList.Add(jr);
            }

            // GROUP BY がない場合は全レコードを1グループに
            if (stmt.GroupBy.Count == 0)
                groups[""] = joinedRecords;

            // HAVINGフィルタリング
            var filteredGroups = groups.ToList();
            if (stmt.HavingClause != null)
            {
                filteredGroups = filteredGroups
                    .Where(kvp => EvaluateJoinedHavingCondition(stmt.HavingClause, kvp.Value))
                    .ToList();
            }

            // 結果の構築
            var resultRows = new List<ResultRow>();

            foreach (var kvp in filteredGroups)
            {
                var groupRecords = kvp.Value;
                var row = new ResultRow();

                foreach (var col in stmt.Columns)
                {
                    if (col.IsWildcard)
                        continue;

                    var colName = col.Alias ?? GetExpressionName(col.Expression);
                    object value;

                    if (col.Expression is AggregateExpression aggExpr)
                    {
                        value = EvaluateJoinedAggregate(aggExpr, groupRecords);
                    }
                    else
                    {
                        value = groupRecords.Count > 0
                            ? ResolveJoinedValue(col.Expression, groupRecords[0])
                            : null;
                    }

                    row.Values[colName] = value;
                }

                resultRows.Add(row);
            }

            result.ColumnNames = stmt.Columns
                .Where(c => !c.IsWildcard)
                .Select(c => c.Alias ?? GetExpressionName(c.Expression))
                .ToList();
            result.Records = resultRows.Cast<object>().ToList();
        }

        /// <summary>
        /// JOIN結果グループに対するHAVING条件を評価する。
        /// </summary>
        private bool EvaluateJoinedHavingCondition(SqlExpression expr, List<JoinedRecord> groupRecords)
        {
            switch (expr)
            {
                case LogicalExpression logical:
                    var leftResult = EvaluateJoinedHavingCondition(logical.Left, groupRecords);
                    var rightResult = EvaluateJoinedHavingCondition(logical.Right, groupRecords);

                    return logical.Operator switch
                    {
                        LogicalOperator.And => leftResult && rightResult,
                        LogicalOperator.Or => leftResult || rightResult,
                        _ => false
                    };

                case ComparisonExpression comparison:
                    return EvaluateJoinedHavingComparison(comparison, groupRecords);

                default:
                    return false;
            }
        }

        private bool EvaluateJoinedHavingComparison(ComparisonExpression expr, List<JoinedRecord> groupRecords)
        {
            var leftValue = ResolveJoinedHavingValue(expr.Left, groupRecords);
            var rightValue = ResolveJoinedHavingValue(expr.Right, groupRecords);

            return expr.Operator switch
            {
                ComparisonOperator.Equal => AreEqual(leftValue, rightValue),
                ComparisonOperator.NotEqual => !AreEqual(leftValue, rightValue),
                ComparisonOperator.LessThan => Compare(leftValue, rightValue) < 0,
                ComparisonOperator.LessOrEqual => Compare(leftValue, rightValue) <= 0,
                ComparisonOperator.GreaterThan => Compare(leftValue, rightValue) > 0,
                ComparisonOperator.GreaterOrEqual => Compare(leftValue, rightValue) >= 0,
                _ => false
            };
        }

        private object ResolveJoinedHavingValue(SqlExpression expr, List<JoinedRecord> groupRecords)
        {
            return expr switch
            {
                AggregateExpression aggExpr => EvaluateJoinedAggregate(aggExpr, groupRecords),
                LiteralExpression literal => literal.Value,
                ColumnExpression column => groupRecords.Count > 0
                    ? ResolveJoinedValue(column, groupRecords[0])
                    : null,
                _ => null
            };
        }

        /// <summary>
        /// 集計関数があるかチェックする。
        /// </summary>
        private bool HasAggregateFunction(List<SelectColumn> columns)
        {
            return columns.Any(c => c.Expression is AggregateExpression);
        }

        /// <summary>
        /// 集計関数を評価する。
        /// </summary>
        private object EvaluateAggregate(AggregateExpression aggExpr, List<object> records, Type recordType)
        {
            if (records.Count == 0)
            {
                return aggExpr.Function == AggregateFunction.Count ? 0 : null;
            }

            var values = new List<object>();

            if (aggExpr.Argument == null)
            {
                // COUNT(*)
                return records.Count;
            }

            foreach (var record in records)
            {
                var value = ResolveValue(aggExpr.Argument, record, recordType);
                if (value != null)
                    values.Add(value);
            }

            if (aggExpr.IsDistinct)
                values = values.Distinct().ToList();

            return aggExpr.Function switch
            {
                AggregateFunction.Count => values.Count,
                AggregateFunction.Sum => CalculateSum(values),
                AggregateFunction.Avg => CalculateAvg(values),
                AggregateFunction.Min => CalculateMin(values),
                AggregateFunction.Max => CalculateMax(values),
                _ => null
            };
        }

        /// <summary>
        /// JOIN結果に対して集計関数を評価する。
        /// </summary>
        private object EvaluateJoinedAggregate(AggregateExpression aggExpr, List<JoinedRecord> records)
        {
            if (records.Count == 0)
            {
                return aggExpr.Function == AggregateFunction.Count ? 0 : null;
            }

            var values = new List<object>();

            if (aggExpr.Argument == null)
            {
                // COUNT(*)
                return records.Count;
            }

            foreach (var record in records)
            {
                var value = ResolveJoinedValue(aggExpr.Argument, record);
                if (value != null)
                    values.Add(value);
            }

            if (aggExpr.IsDistinct)
                values = values.Distinct().ToList();

            return aggExpr.Function switch
            {
                AggregateFunction.Count => values.Count,
                AggregateFunction.Sum => CalculateSum(values),
                AggregateFunction.Avg => CalculateAvg(values),
                AggregateFunction.Min => CalculateMin(values),
                AggregateFunction.Max => CalculateMax(values),
                _ => null
            };
        }

        private object CalculateSum(List<object> values)
        {
            if (values.Count == 0)
                return null;

            double sum = 0;
            foreach (var value in values)
            {
                if (IsNumeric(value))
                    sum += Convert.ToDouble(value);
            }
            return sum;
        }

        private object CalculateAvg(List<object> values)
        {
            if (values.Count == 0)
                return null;

            var numericValues = values.Where(IsNumeric).ToList();
            if (numericValues.Count == 0)
                return null;

            return numericValues.Average(v => Convert.ToDouble(v));
        }

        private object CalculateMin(List<object> values)
        {
            if (values.Count == 0)
                return null;

            object min = null;
            foreach (var value in values)
            {
                if (min == null || Compare(value, min) < 0)
                    min = value;
            }
            return min;
        }

        private object CalculateMax(List<object> values)
        {
            if (values.Count == 0)
                return null;

            object max = null;
            foreach (var value in values)
            {
                if (max == null || Compare(value, max) > 0)
                    max = value;
            }
            return max;
        }

        /// <summary>
        /// HAVING条件を評価する。
        /// </summary>
        private bool EvaluateHavingCondition(SqlExpression expr, AggregateGroup group, Type recordType)
        {
            switch (expr)
            {
                case LogicalExpression logical:
                    var leftResult = EvaluateHavingCondition(logical.Left, group, recordType);
                    var rightResult = EvaluateHavingCondition(logical.Right, group, recordType);

                    return logical.Operator switch
                    {
                        LogicalOperator.And => leftResult && rightResult,
                        LogicalOperator.Or => leftResult || rightResult,
                        _ => false
                    };

                case ComparisonExpression comparison:
                    return EvaluateHavingComparison(comparison, group, recordType);

                default:
                    return false;
            }
        }

        private bool EvaluateHavingComparison(ComparisonExpression expr, AggregateGroup group, Type recordType)
        {
            var leftValue = ResolveHavingValue(expr.Left, group, recordType);
            var rightValue = ResolveHavingValue(expr.Right, group, recordType);

            return expr.Operator switch
            {
                ComparisonOperator.Equal => AreEqual(leftValue, rightValue),
                ComparisonOperator.NotEqual => !AreEqual(leftValue, rightValue),
                ComparisonOperator.LessThan => Compare(leftValue, rightValue) < 0,
                ComparisonOperator.LessOrEqual => Compare(leftValue, rightValue) <= 0,
                ComparisonOperator.GreaterThan => Compare(leftValue, rightValue) > 0,
                ComparisonOperator.GreaterOrEqual => Compare(leftValue, rightValue) >= 0,
                _ => false
            };
        }

        private object ResolveHavingValue(SqlExpression expr, AggregateGroup group, Type recordType)
        {
            return expr switch
            {
                AggregateExpression aggExpr => EvaluateAggregate(aggExpr, group.Records, recordType),
                LiteralExpression literal => literal.Value,
                ColumnExpression column => group.Records.Count > 0
                    ? GetFieldValue(group.Records[0], recordType, column.ColumnName)
                    : null,
                _ => null
            };
        }

        /// <summary>
        /// JOIN条件を評価する。
        /// </summary>
        private bool EvaluateJoinCondition(SqlExpression expr, JoinedRecord joinedRecord)
        {
            switch (expr)
            {
                case LogicalExpression logical:
                    var leftResult = EvaluateJoinCondition(logical.Left, joinedRecord);
                    var rightResult = EvaluateJoinCondition(logical.Right, joinedRecord);

                    return logical.Operator switch
                    {
                        LogicalOperator.And => leftResult && rightResult,
                        LogicalOperator.Or => leftResult || rightResult,
                        _ => false
                    };

                case ComparisonExpression comparison:
                    return EvaluateJoinComparison(comparison, joinedRecord);

                default:
                    return false;
            }
        }

        private bool EvaluateJoinComparison(ComparisonExpression expr, JoinedRecord joinedRecord)
        {
            var leftValue = ResolveJoinedValue(expr.Left, joinedRecord);

            switch (expr.Operator)
            {
                case ComparisonOperator.IsNull:
                    return leftValue == null;
                case ComparisonOperator.IsNotNull:
                    return leftValue != null;
                default:
                    var rightValue = ResolveJoinedValue(expr.Right, joinedRecord);
                    return expr.Operator switch
                    {
                        ComparisonOperator.Equal => AreEqual(leftValue, rightValue),
                        ComparisonOperator.NotEqual => !AreEqual(leftValue, rightValue),
                        ComparisonOperator.LessThan => Compare(leftValue, rightValue) < 0,
                        ComparisonOperator.LessOrEqual => Compare(leftValue, rightValue) <= 0,
                        ComparisonOperator.GreaterThan => Compare(leftValue, rightValue) > 0,
                        ComparisonOperator.GreaterOrEqual => Compare(leftValue, rightValue) >= 0,
                        _ => false
                    };
            }
        }

        /// <summary>
        /// JOIN結果から値を取得する。
        /// </summary>
        private object ResolveJoinedValue(SqlExpression expr, JoinedRecord joinedRecord)
        {
            switch (expr)
            {
                case LiteralExpression literal:
                    return literal.Value;

                case ColumnExpression column:
                    // テーブルエイリアスが指定されている場合
                    if (!string.IsNullOrEmpty(column.TableAlias))
                    {
                        var record = joinedRecord.GetRecord(column.TableAlias);
                        var recordType = joinedRecord.GetRecordType(column.TableAlias);
                        if (record != null && recordType != null)
                            return GetFieldValue(record, recordType, column.ColumnName);
                        return null;
                    }

                    // テーブルエイリアスがない場合は全テーブルから検索
                    foreach (var kvp in joinedRecord.TableRecords)
                    {
                        var record = kvp.Value;
                        if (record == null)
                            continue;

                        var recordType = joinedRecord.GetRecordType(kvp.Key);
                        var value = GetFieldValue(record, recordType, column.ColumnName);
                        if (value != null)
                            return value;
                    }
                    return null;

                case AggregateExpression:
                case ArithmeticExpression:
                case CaseExpression:
                case FunctionCallExpression:
                    // これらは別途処理が必要
                    return null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// DISTINCTを適用する。
        /// </summary>
        private List<object> ApplyDistinct(List<object> records, List<SelectColumn> columns, Type recordType)
        {
            var seen = new HashSet<string>();
            var result = new List<object>();

            foreach (var record in records)
            {
                var keyParts = new List<string>();

                foreach (var col in columns)
                {
                    if (col.IsWildcard)
                    {
                        foreach (var field in ReflectionUtility.GetSerializableFields(recordType))
                        {
                            var value = field.GetValue(record);
                            keyParts.Add(value?.ToString() ?? "NULL");
                        }
                    }
                    else if (col.Expression is ColumnExpression colExpr)
                    {
                        var value = GetFieldValue(record, recordType, colExpr.ColumnName);
                        keyParts.Add(value?.ToString() ?? "NULL");
                    }
                }

                var key = string.Join("|", keyParts);
                if (seen.Add(key))
                    result.Add(record);
            }

            return result;
        }

        /// <summary>
        /// DISTINCTを適用し、ResultRowとして返す。
        /// </summary>
        private List<ResultRow> ApplyDistinctAsResultRows(List<object> records, List<SelectColumn> columns, Type recordType)
        {
            var seen = new HashSet<string>();
            var result = new List<ResultRow>();

            foreach (var record in records)
            {
                var row = new ResultRow { SourceRecord = record };
                var keyParts = new List<string>();

                foreach (var col in columns)
                {
                    if (col.IsWildcard)
                    {
                        foreach (var field in ReflectionUtility.GetSerializableFields(recordType))
                        {
                            var value = field.GetValue(record);
                            row.Values[field.Name] = value;
                            keyParts.Add(value?.ToString() ?? "NULL");
                        }
                    }
                    else
                    {
                        var colName = col.Alias ?? GetExpressionName(col.Expression);
                        var value = ResolveValue(col.Expression, record, recordType);
                        row.Values[colName] = value;
                        keyParts.Add(value?.ToString() ?? "NULL");
                    }
                }

                var key = string.Join("|", keyParts);
                if (seen.Add(key))
                    result.Add(row);
            }

            return result;
        }

        /// <summary>
        /// 式の名前を取得する。
        /// </summary>
        private string GetExpressionName(SqlExpression expr)
        {
            return expr switch
            {
                ColumnExpression col => col.ToString(),
                AggregateExpression agg => agg.ToString(),
                FunctionCallExpression func => func.ToString(),
                _ => expr.ToString()
            };
        }

        /// <summary>
        /// JOINカラム名を取得する。
        /// </summary>
        private string GetJoinedColumnName(SelectColumn col)
        {
            if (col.Expression is ColumnExpression colExpr)
                return colExpr.ToString();
            return col.Expression.ToString();
        }

        /// <summary>
        /// ResultRowのソート。
        /// </summary>
        private List<ResultRow> SortResultRows(List<ResultRow> rows, List<OrderByItem> orderBy)
        {
            IOrderedEnumerable<ResultRow> ordered = null;

            for (var i = 0; i < orderBy.Count; i++)
            {
                var item = orderBy[i];
                var columnName = GetExpressionName(item.Expression);

                Func<ResultRow, object> keySelector = r => r[columnName];

                if (i == 0)
                {
                    ordered = item.Order == SortOrder.Descending
                        ? rows.OrderByDescending(keySelector, new ObjectComparer())
                        : rows.OrderBy(keySelector, new ObjectComparer());
                }
                else
                {
                    ordered = item.Order == SortOrder.Descending
                        ? ordered.ThenByDescending(keySelector, new ObjectComparer())
                        : ordered.ThenBy(keySelector, new ObjectComparer());
                }
            }

            return ordered?.ToList() ?? rows;
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
                    // サブクエリの場合
                    if (expr.Right is SubqueryExpression subqueryExpr)
                    {
                        var subqueryValues = ExecuteSubquery(subqueryExpr.Subquery);
                        foreach (var subValue in subqueryValues)
                        {
                            if (AreEqual(leftValue, subValue))
                                return true;
                        }
                        return false;
                    }

                    // 通常のINリスト
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
                    // サブクエリの場合
                    object rightValue;
                    if (expr.Right is SubqueryExpression subquery)
                    {
                        var subqueryValues = ExecuteSubquery(subquery.Subquery);
                        rightValue = subqueryValues.FirstOrDefault();
                    }
                    else
                    {
                        rightValue = ResolveValue(expr.Right, record, recordType);
                    }

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
        /// サブクエリを実行して結果の値リストを返す。
        /// </summary>
        private List<object> ExecuteSubquery(SelectStatement subquery)
        {
            var result = new SqlQueryResult();
            ExecuteSelect(subquery, result);

            if (!result.IsSuccess || result.Records.Count == 0)
                return new List<object>();

            var values = new List<object>();

            foreach (var record in result.Records)
            {
                if (record is ResultRow row)
                {
                    // ResultRowの場合、最初のカラムの値を取得
                    var firstValue = row.Values.Values.FirstOrDefault();
                    values.Add(firstValue);
                }
                else
                {
                    // 通常レコードの場合、選択されたカラムの値を取得
                    var recordType = record.GetType();
                    if (subquery.Columns.Count > 0 && !subquery.Columns[0].IsWildcard)
                    {
                        var colExpr = subquery.Columns[0].Expression;
                        var value = ResolveValue(colExpr, record, recordType);
                        values.Add(value);
                    }
                    else
                    {
                        // ワイルドカードの場合、レコード全体を追加
                        values.Add(record);
                    }
                }
            }

            return values;
        }

        /// <summary>
        /// 式から値を取得する。
        /// </summary>
        private object ResolveValue(SqlExpression expr, object record, Type recordType)
        {
            switch (expr)
            {
                case LiteralExpression literal:
                    return literal.Value;

                case ColumnExpression column:
                    return GetFieldValue(record, recordType, column.ColumnName);

                case ArithmeticExpression arith:
                    return EvaluateArithmetic(arith, record, recordType);

                case CaseExpression caseExpr:
                    return EvaluateCase(caseExpr, record, recordType);

                case FunctionCallExpression funcExpr:
                    return EvaluateFunction(funcExpr, record, recordType);

                case AggregateExpression:
                    // 集計関数は単一レコードでは評価できない
                    return null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// 算術式を評価する。
        /// </summary>
        private object EvaluateArithmetic(ArithmeticExpression expr, object record, Type recordType)
        {
            var left = ResolveValue(expr.Left, record, recordType);
            var right = ResolveValue(expr.Right, record, recordType);

            if (!IsNumeric(left) || !IsNumeric(right))
                return null;

            var leftVal = Convert.ToDouble(left);
            var rightVal = Convert.ToDouble(right);

            return expr.Operator switch
            {
                ArithmeticOperator.Add => leftVal + rightVal,
                ArithmeticOperator.Subtract => leftVal - rightVal,
                ArithmeticOperator.Multiply => leftVal * rightVal,
                ArithmeticOperator.Divide => rightVal != 0 ? leftVal / rightVal : null,
                ArithmeticOperator.Modulo => rightVal != 0 ? leftVal % rightVal : null,
                _ => null
            };
        }

        /// <summary>
        /// CASE式を評価する。
        /// </summary>
        private object EvaluateCase(CaseExpression expr, object record, Type recordType)
        {
            foreach (var when in expr.WhenClauses)
            {
                if (EvaluateExpression(when.Condition, record, recordType))
                    return ResolveValue(when.Result, record, recordType);
            }

            if (expr.ElseExpression != null)
                return ResolveValue(expr.ElseExpression, record, recordType);

            return null;
        }

        /// <summary>
        /// 関数を評価する。
        /// </summary>
        private object EvaluateFunction(FunctionCallExpression expr, object record, Type recordType)
        {
            var args = expr.Arguments.Select(a => ResolveValue(a, record, recordType)).ToList();

            return expr.FunctionName.ToUpper() switch
            {
                "UPPER" => args.FirstOrDefault()?.ToString()?.ToUpper(),
                "LOWER" => args.FirstOrDefault()?.ToString()?.ToLower(),
                "TRIM" => args.FirstOrDefault()?.ToString()?.Trim(),
                "LENGTH" => args.FirstOrDefault()?.ToString()?.Length ?? 0,
                "CONCAT" => string.Concat(args.Select(a => a?.ToString() ?? "")),
                "SUBSTRING" => EvaluateSubstring(args),
                _ => null
            };
        }

        private object EvaluateSubstring(List<object> args)
        {
            if (args.Count < 2)
                return null;

            var str = args[0]?.ToString();
            if (string.IsNullOrEmpty(str))
                return null;

            if (!int.TryParse(args[1]?.ToString(), out var start))
                return null;

            // SQLのSUBSTRINGは1始まり
            start = Math.Max(0, start - 1);

            if (args.Count >= 3 && int.TryParse(args[2]?.ToString(), out var length))
            {
                if (start >= str.Length)
                    return "";
                return str.Substring(start, Math.Min(length, str.Length - start));
            }

            if (start >= str.Length)
                return "";
            return str.Substring(start);
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
                    foreach (var field in ReflectionUtility.GetSerializableFields(recordType))
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
