using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Xeon.XScriptableDB.Editor
{
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
            return tables.GetValueOrDefault(tableName, null);
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
            if (!TryGetMainTable(stmt, result, out var mainTable, out var mainAlias))
                return;

            if (stmt.Joins.Count > 0)
            {
                ExecuteSelectWithJoin(stmt, mainTable, mainAlias, result);
                return;
            }

            if (stmt.GroupBy.Count > 0 || HasAggregateFunction(stmt.Columns))
            {
                ExecuteSelectWithGroupBy(stmt, mainTable, result);
                return;
            }

            ExecuteSimpleSelect(stmt, mainTable, result);
        }

        /// <summary>
        /// メインテーブルを取得する。
        /// </summary>
        private bool TryGetMainTable(
            SelectStatement stmt,
            SqlQueryResult result,
            out ITableAsset mainTable,
            out string mainAlias)
        {
            var mainTableName = stmt.FromTable?.TableName ?? stmt.TableName;
            mainAlias = stmt.FromTable?.Alias ?? mainTableName;

            if (tables.TryGetValue(mainTableName, out mainTable))
                return true;

            result.ErrorMessage = $"Table not found: {mainTableName}";
            return false;
        }

        /// <summary>
        /// 通常のSELECT（JOIN/GROUP BYなし）を実行する。
        /// </summary>
        private void ExecuteSimpleSelect(SelectStatement stmt, ITableAsset mainTable, SqlQueryResult result)
        {
            var recordType = mainTable.RecordType;
            var records = FilterRecords(mainTable.Records.Cast<object>(), stmt.WhereClause, recordType);

            if (stmt.OrderBy != null && stmt.OrderBy.Count > 0)
                records = SortRecords(records, stmt.OrderBy, recordType);

            if (stmt.IsDistinct && TryApplyDistinctWithResultRows(stmt, records, recordType, result))
                return;

            if (stmt.IsDistinct)
                records = ApplyDistinct(records, stmt.Columns, recordType);

            records = ApplyOffsetAndLimit(records, stmt.Offset, stmt.Limit);

            result.ColumnNames = GetColumnNames(stmt.Columns, recordType);
            result.Records = BuildResultRecords(records, stmt.Columns, recordType);
        }

        /// <summary>
        /// レコードをWHERE句でフィルタリングする。
        /// </summary>
        private List<object> FilterRecords(IEnumerable<object> records, SqlExpression whereClause, Type recordType)
        {
            var result = new List<object>();

            foreach (var record in records)
            {
                if (record == null)
                    continue;

                if (whereClause == null || EvaluateExpression(whereClause, record, recordType))
                    result.Add(record);
            }

            return result;
        }

        /// <summary>
        /// 特定カラムのDISTINCTをResultRowとして適用する。
        /// </summary>
        private bool TryApplyDistinctWithResultRows(
            SelectStatement stmt,
            List<object> records,
            Type recordType,
            SqlQueryResult result)
        {
            var hasSpecificColumns = stmt.Columns.Any(c => !c.IsWildcard);
            if (!hasSpecificColumns)
                return false;

            var distinctRows = ApplyDistinctAsResultRows(records, stmt.Columns, recordType);

            if (stmt.OrderBy != null && stmt.OrderBy.Count > 0)
                distinctRows = SortResultRows(distinctRows, stmt.OrderBy);

            distinctRows = ApplyOffsetAndLimitToResultRows(distinctRows, stmt.Offset, stmt.Limit);

            result.ColumnNames = GetColumnNames(stmt.Columns, recordType);
            result.Records = distinctRows.Cast<object>().ToList();
            return true;
        }

        /// <summary>
        /// OFFSETとLIMITを適用する。
        /// </summary>
        private List<object> ApplyOffsetAndLimit(List<object> records, int? offset, int? limit)
        {
            if (offset.HasValue && offset.Value > 0)
                records = records.Skip(offset.Value).ToList();

            if (limit.HasValue)
                records = records.Take(limit.Value).ToList();

            return records;
        }

        /// <summary>
        /// ResultRowリストにOFFSETとLIMITを適用する。
        /// </summary>
        private List<ResultRow> ApplyOffsetAndLimitToResultRows(List<ResultRow> rows, int? offset, int? limit)
        {
            if (offset.HasValue && offset.Value > 0)
                rows = rows.Skip(offset.Value).ToList();

            if (limit.HasValue)
                rows = rows.Take(limit.Value).ToList();

            return rows;
        }

        /// <summary>
        /// 結果レコードを構築する。
        /// </summary>
        private List<object> BuildResultRecords(List<object> records, List<SelectColumn> columns, Type recordType)
        {
            if (!NeedsResultRowProjection(columns))
                return records;

            var resultRows = new List<ResultRow>();
            foreach (var record in records)
            {
                var row = BuildResultRow(record, columns, recordType);
                resultRows.Add(row);
            }
            return resultRows.Cast<object>().ToList();
        }

        /// <summary>
        /// 単一レコードからResultRowを構築する。
        /// </summary>
        private ResultRow BuildResultRow(object record, List<SelectColumn> columns, Type recordType)
        {
            var row = new ResultRow { SourceRecord = record };

            foreach (var col in columns)
            {
                if (col.IsWildcard)
                {
                    AddWildcardFieldsToRow(row, record, recordType);
                }
                else
                {
                    var colName = col.Alias ?? GetExpressionName(col.Expression);
                    row.Values[colName] = ResolveValue(col.Expression, record, recordType);
                }
            }

            return row;
        }

        /// <summary>
        /// ワイルドカードの全フィールドをResultRowに追加する。
        /// </summary>
        private void AddWildcardFieldsToRow(ResultRow row, object record, Type recordType)
        {
            foreach (var field in ReflectionUtility.GetSerializableFields(recordType))
                row.Values[field.Name] = field.GetValue(record);
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
            if (!TryGetJoinTables(stmt.Joins, result, out var joinTables))
                return;

            var joinedRecords = ExecuteJoins(mainTable, mainAlias, joinTables);
            ProcessRightJoins(joinedRecords, mainTable, mainAlias, joinTables);

            joinedRecords = ApplyWhereToJoinedRecords(joinedRecords, stmt.WhereClause);

            if (stmt.GroupBy.Count > 0 || HasAggregateFunction(stmt.Columns))
            {
                ExecuteJoinedGroupBy(stmt, joinedRecords, result);
                return;
            }

            joinedRecords = ApplySortAndPaginationToJoinedRecords(joinedRecords, stmt);

            var columnNames = BuildJoinedColumnNames(stmt.Columns, mainTable, mainAlias, joinTables);
            var resultRows = ConvertJoinedRecordsToResultRows(joinedRecords, columnNames);

            result.ColumnNames = columnNames;
            result.Records = resultRows.Cast<object>().ToList();
        }

        /// <summary>
        /// JOIN対象のテーブルを取得する。
        /// </summary>
        private bool TryGetJoinTables(
            List<JoinClause> joins,
            SqlQueryResult result,
            out List<(ITableAsset table, string alias, JoinClause clause)> joinTables)
        {
            joinTables = new List<(ITableAsset, string, JoinClause)>();

            foreach (var join in joins)
            {
                if (!tables.TryGetValue(join.TableName, out var joinTable))
                {
                    result.ErrorMessage = $"Table not found: {join.TableName}";
                    return false;
                }
                var alias = join.Alias ?? join.TableName;
                joinTables.Add((joinTable, alias, join));
            }

            return true;
        }

        /// <summary>
        /// JOINを実行する（INNER/LEFT/CROSS）。
        /// </summary>
        private List<JoinedRecord> ExecuteJoins(
            ITableAsset mainTable,
            string mainAlias,
            List<(ITableAsset table, string alias, JoinClause clause)> joinTables)
        {
            var joinedRecords = new List<JoinedRecord>();

            foreach (var mainRecord in mainTable.Records)
            {
                if (mainRecord == null)
                    continue;

                var currentResults = CreateInitialJoinedRecords(mainRecord, mainTable.RecordType, mainAlias);

                foreach (var (joinTable, joinAlias, joinClause) in joinTables)
                    currentResults = ProcessSingleJoin(currentResults, joinTable, joinAlias, joinClause);

                joinedRecords.AddRange(currentResults);
            }

            return joinedRecords;
        }

        /// <summary>
        /// 初期のJoinedRecordリストを作成する。
        /// </summary>
        private List<JoinedRecord> CreateInitialJoinedRecords(object mainRecord, Type mainRecordType, string mainAlias)
        {
            return new List<JoinedRecord>
            {
                new JoinedRecord
                {
                    TableRecords = { [mainAlias] = mainRecord },
                    TableTypes = { [mainAlias] = mainRecordType }
                }
            };
        }

        /// <summary>
        /// 単一のJOINを処理する。
        /// </summary>
        private List<JoinedRecord> ProcessSingleJoin(
            List<JoinedRecord> currentResults,
            ITableAsset joinTable,
            string joinAlias,
            JoinClause joinClause)
        {
            var nextResults = new List<JoinedRecord>();

            foreach (var currentRecord in currentResults)
            {
                var matched = ProcessJoinMatches(currentRecord, joinTable, joinAlias, joinClause, nextResults);

                if (!matched && joinClause.JoinType == JoinType.Left)
                    nextResults.Add(CreateNullJoinedRecord(currentRecord, joinTable.RecordType, joinAlias));
            }

            return nextResults;
        }

        /// <summary>
        /// JOINのマッチングを処理する。
        /// </summary>
        private bool ProcessJoinMatches(
            JoinedRecord currentRecord,
            ITableAsset joinTable,
            string joinAlias,
            JoinClause joinClause,
            List<JoinedRecord> results)
        {
            var matched = false;

            foreach (var joinRecord in joinTable.Records)
            {
                if (joinRecord == null)
                    continue;

                var testRecord = CreateTestJoinedRecord(currentRecord, joinRecord, joinTable.RecordType, joinAlias);

                if (joinClause.JoinType == JoinType.Cross || EvaluateJoinCondition(joinClause.OnCondition, testRecord))
                {
                    results.Add(testRecord);
                    matched = true;
                }
            }

            return matched;
        }

        /// <summary>
        /// テスト用のJoinedRecordを作成する。
        /// </summary>
        private JoinedRecord CreateTestJoinedRecord(
            JoinedRecord currentRecord,
            object joinRecord,
            Type joinRecordType,
            string joinAlias)
        {
            return new JoinedRecord
            {
                TableRecords = new Dictionary<string, object>(currentRecord.TableRecords, StringComparer.OrdinalIgnoreCase)
                {
                    [joinAlias] = joinRecord
                },
                TableTypes = new Dictionary<string, Type>(currentRecord.TableTypes, StringComparer.OrdinalIgnoreCase)
                {
                    [joinAlias] = joinRecordType
                }
            };
        }

        /// <summary>
        /// NULL値を持つJoinedRecordを作成する（LEFT JOIN用）。
        /// </summary>
        private JoinedRecord CreateNullJoinedRecord(JoinedRecord currentRecord, Type joinRecordType, string joinAlias)
        {
            return new JoinedRecord
            {
                TableRecords = new Dictionary<string, object>(currentRecord.TableRecords, StringComparer.OrdinalIgnoreCase)
                {
                    [joinAlias] = null
                },
                TableTypes = new Dictionary<string, Type>(currentRecord.TableTypes, StringComparer.OrdinalIgnoreCase)
                {
                    [joinAlias] = joinRecordType
                }
            };
        }

        /// <summary>
        /// RIGHT JOINを処理する。
        /// </summary>
        private void ProcessRightJoins(
            List<JoinedRecord> joinedRecords,
            ITableAsset mainTable,
            string mainAlias,
            List<(ITableAsset table, string alias, JoinClause clause)> joinTables)
        {
            foreach (var (joinTable, joinAlias, joinClause) in joinTables)
            {
                if (joinClause.JoinType != JoinType.Right)
                    continue;

                ProcessSingleRightJoin(joinedRecords, mainTable, mainAlias, joinTable, joinAlias);
            }
        }

        /// <summary>
        /// 単一のRIGHT JOINを処理する。
        /// </summary>
        private void ProcessSingleRightJoin(
            List<JoinedRecord> joinedRecords,
            ITableAsset mainTable,
            string mainAlias,
            ITableAsset joinTable,
            string joinAlias)
        {
            foreach (var joinRecord in joinTable.Records)
            {
                if (joinRecord == null)
                    continue;

                var hasMatch = joinedRecords.Any(jr =>
                    jr.TableRecords.TryGetValue(joinAlias, out var rec) && rec != null &&
                    ReferenceEquals(rec, joinRecord));

                if (hasMatch)
                    continue;

                var nullRecord = new JoinedRecord
                {
                    TableRecords = { [mainAlias] = null, [joinAlias] = joinRecord },
                    TableTypes = { [mainAlias] = mainTable.RecordType, [joinAlias] = joinTable.RecordType }
                };
                joinedRecords.Add(nullRecord);
            }
        }

        /// <summary>
        /// JoinedRecordsにWHERE句を適用する。
        /// </summary>
        private List<JoinedRecord> ApplyWhereToJoinedRecords(List<JoinedRecord> records, SqlExpression whereClause)
        {
            if (whereClause == null)
                return records;

            return records.Where(jr => EvaluateJoinCondition(whereClause, jr)).ToList();
        }

        /// <summary>
        /// JoinedRecordsにソートとページネーションを適用する。
        /// </summary>
        private List<JoinedRecord> ApplySortAndPaginationToJoinedRecords(List<JoinedRecord> records, SelectStatement stmt)
        {
            if (stmt.OrderBy != null && stmt.OrderBy.Count > 0)
                records = SortJoinedRecords(records, stmt.OrderBy);

            if (stmt.Offset.HasValue && stmt.Offset.Value > 0)
                records = records.Skip(stmt.Offset.Value).ToList();

            if (stmt.Limit.HasValue)
                records = records.Take(stmt.Limit.Value).ToList();

            return records;
        }

        /// <summary>
        /// JOIN結果のカラム名リストを構築する。
        /// </summary>
        private List<string> BuildJoinedColumnNames(
            List<SelectColumn> columns,
            ITableAsset mainTable,
            string mainAlias,
            List<(ITableAsset table, string alias, JoinClause clause)> joinTables)
        {
            var columnNames = new List<string>();
            var hasWildcard = columns.Any(c => c.IsWildcard);

            if (hasWildcard)
            {
                AddWildcardColumnNames(columnNames, mainTable.RecordType, mainAlias);
                foreach (var (table, alias, _) in joinTables)
                    AddWildcardColumnNames(columnNames, table.RecordType, alias);
            }

            foreach (var col in columns)
            {
                if (col.IsWildcard)
                    continue;

                var colName = GetJoinedColumnName(col);
                columnNames.Add(col.Alias ?? colName);
            }

            return columnNames;
        }

        /// <summary>
        /// ワイルドカードのカラム名を追加する。
        /// </summary>
        private void AddWildcardColumnNames(List<string> columnNames, Type recordType, string alias)
        {
            foreach (var field in ReflectionUtility.GetSerializableFields(recordType))
                columnNames.Add($"{alias}.{field.Name}");
        }

        /// <summary>
        /// JoinedRecordsをResultRowsに変換する。
        /// </summary>
        private List<ResultRow> ConvertJoinedRecordsToResultRows(List<JoinedRecord> joinedRecords, List<string> columnNames)
        {
            var resultRows = new List<ResultRow>();

            foreach (var jr in joinedRecords)
            {
                var row = new ResultRow();

                foreach (var colName in columnNames)
                    row.Values[colName] = ResolveColumnValueFromJoinedRecord(jr, colName);

                resultRows.Add(row);
            }

            return resultRows;
        }

        /// <summary>
        /// JoinedRecordからカラム値を解決する。
        /// </summary>
        private object ResolveColumnValueFromJoinedRecord(JoinedRecord jr, string colName)
        {
            if (colName.Contains('.'))
                return ResolveQualifiedColumnValue(jr, colName);

            return ResolveUnqualifiedColumnValue(jr, colName);
        }

        /// <summary>
        /// テーブル修飾付きカラム値を解決する（例: "t.Id"）。
        /// </summary>
        private object ResolveQualifiedColumnValue(JoinedRecord jr, string colName)
        {
            var parts = colName.Split('.');
            var tableAlias = parts[0];
            var fieldName = parts[1];

            var record = jr.GetRecord(tableAlias);
            var recordType = jr.GetRecordType(tableAlias);

            if (record != null && recordType != null)
                return GetFieldValue(record, recordType, fieldName);

            return null;
        }

        /// <summary>
        /// テーブル修飾なしカラム値を解決する。
        /// </summary>
        private object ResolveUnqualifiedColumnValue(JoinedRecord jr, string colName)
        {
            foreach (var kvp in jr.TableRecords)
            {
                var record = kvp.Value;
                if (record == null)
                    continue;

                var recordType = jr.GetRecordType(kvp.Key);
                if (HasField(recordType, colName))
                    return GetFieldValue(record, recordType, colName);
            }

            return null;
        }

        /// <summary>
        /// JOIN結果をソートする。
        /// </summary>
        private List<JoinedRecord> SortJoinedRecords(List<JoinedRecord> records, List<OrderByItem> orderBy)
        {
            IOrderedEnumerable<JoinedRecord> ordered = null;

            for (var i = 0; i < orderBy.Count; i++)
            {
                var item = orderBy[i];
                Func<JoinedRecord, object> keySelector = r => ResolveJoinedValue(item.Expression, r);

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
        /// GROUP BYを含むSELECTを実行する。
        /// </summary>
        private void ExecuteSelectWithGroupBy(SelectStatement stmt, ITableAsset table, SqlQueryResult result)
        {
            var recordType = table.RecordType;
            var records = FilterRecords(table.Records.Cast<object>(), stmt.WhereClause, recordType);

            var groups = GroupRecordsByKey(records, stmt.GroupBy, recordType);
            var filteredGroups = ApplyHavingFilter(groups, stmt.HavingClause, recordType);

            var wildcardFields = GetWildcardFieldsIfNeeded(stmt.Columns, recordType);
            var resultRows = BuildGroupByResultRows(filteredGroups, stmt.Columns, recordType, wildcardFields);

            resultRows = ApplySortAndPaginationToResultRows(resultRows, stmt);

            result.ColumnNames = BuildGroupByColumnNames(stmt.Columns, wildcardFields);
            result.Records = resultRows.Cast<object>().ToList();
        }

        /// <summary>
        /// レコードをGROUP BYキーでグループ化する。
        /// </summary>
        private Dictionary<string, AggregateGroup> GroupRecordsByKey(
            List<object> records,
            List<GroupByItem> groupByItems,
            Type recordType)
        {
            var groups = new Dictionary<string, AggregateGroup>();

            if (groupByItems.Count == 0)
            {
                groups[""] = new AggregateGroup { Records = records };
                return groups;
            }

            foreach (var record in records)
            {
                var (key, keyValues) = BuildGroupKey(record, groupByItems, recordType);

                if (!groups.TryGetValue(key, out var group))
                {
                    group = new AggregateGroup { GroupKeyValues = keyValues };
                    groups[key] = group;
                }
                group.Records.Add(record);
            }

            return groups;
        }

        /// <summary>
        /// レコードからグループキーを構築する。
        /// </summary>
        private (string key, Dictionary<string, object> keyValues) BuildGroupKey(
            object record,
            List<GroupByItem> groupByItems,
            Type recordType)
        {
            var keyValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var keyParts = new List<string>();

            foreach (var groupItem in groupByItems)
            {
                var value = ResolveValue(groupItem.Expression, record, recordType);
                var columnName = GetExpressionName(groupItem.Expression);
                keyValues[columnName] = value;
                keyParts.Add(value?.ToString() ?? "NULL");
            }

            return (string.Join("|", keyParts), keyValues);
        }

        /// <summary>
        /// HAVING句でグループをフィルタリングする。
        /// </summary>
        private List<AggregateGroup> ApplyHavingFilter(
            Dictionary<string, AggregateGroup> groups,
            SqlExpression havingClause,
            Type recordType)
        {
            var filteredGroups = groups.Values.ToList();

            if (havingClause == null)
                return filteredGroups;

            return filteredGroups
                .Where(g => EvaluateHavingCondition(havingClause, g, recordType))
                .ToList();
        }

        /// <summary>
        /// ワイルドカードがある場合のフィールドリストを取得する。
        /// </summary>
        private List<FieldInfo> GetWildcardFieldsIfNeeded(List<SelectColumn> columns, Type recordType)
        {
            var hasWildcard = columns.Any(c => c.IsWildcard);
            return hasWildcard ? ReflectionUtility.GetSerializableFields(recordType).ToList() : null;
        }

        /// <summary>
        /// GROUP BY結果からResultRowリストを構築する。
        /// </summary>
        private List<ResultRow> BuildGroupByResultRows(
            List<AggregateGroup> groups,
            List<SelectColumn> columns,
            Type recordType,
            List<FieldInfo> wildcardFields)
        {
            var resultRows = new List<ResultRow>();

            foreach (var group in groups)
            {
                var row = BuildGroupByResultRow(group, columns, recordType, wildcardFields);
                resultRows.Add(row);
            }

            return resultRows;
        }

        /// <summary>
        /// 単一グループからResultRowを構築する。
        /// </summary>
        private ResultRow BuildGroupByResultRow(
            AggregateGroup group,
            List<SelectColumn> columns,
            Type recordType,
            List<FieldInfo> wildcardFields)
        {
            var row = new ResultRow();

            foreach (var col in columns)
            {
                if (col.IsWildcard)
                {
                    AddWildcardValuesToRow(row, group, wildcardFields);
                    continue;
                }

                var colName = col.Alias ?? GetExpressionName(col.Expression);
                row.Values[colName] = ResolveGroupByColumnValue(col, group, recordType);
            }

            return row;
        }

        /// <summary>
        /// ワイルドカードの値をResultRowに追加する。
        /// </summary>
        private void AddWildcardValuesToRow(ResultRow row, AggregateGroup group, List<FieldInfo> wildcardFields)
        {
            if (group.Records.Count == 0 || wildcardFields == null)
                return;

            var firstRecord = group.Records[0];
            foreach (var field in wildcardFields)
                row.Values[field.Name] = field.GetValue(firstRecord);
        }

        /// <summary>
        /// GROUP BYコンテキストでカラム値を解決する。
        /// </summary>
        private object ResolveGroupByColumnValue(SelectColumn col, AggregateGroup group, Type recordType)
        {
            if (col.Expression is AggregateExpression aggExpr)
                return EvaluateAggregate(aggExpr, group.Records, recordType);

            var exprName = GetExpressionName(col.Expression);
            if (group.GroupKeyValues.TryGetValue(exprName, out var keyValue))
                return keyValue;

            return group.Records.Count > 0
                ? ResolveValue(col.Expression, group.Records[0], recordType)
                : null;
        }

        /// <summary>
        /// ResultRowリストにソートとページネーションを適用する。
        /// </summary>
        private List<ResultRow> ApplySortAndPaginationToResultRows(List<ResultRow> rows, SelectStatement stmt)
        {
            if (stmt.OrderBy != null && stmt.OrderBy.Count > 0)
                rows = SortResultRows(rows, stmt.OrderBy);

            if (stmt.Offset.HasValue && stmt.Offset.Value > 0)
                rows = rows.Skip(stmt.Offset.Value).ToList();

            if (stmt.Limit.HasValue)
                rows = rows.Take(stmt.Limit.Value).ToList();

            return rows;
        }

        /// <summary>
        /// GROUP BY結果のカラム名リストを構築する。
        /// </summary>
        private List<string> BuildGroupByColumnNames(List<SelectColumn> columns, List<FieldInfo> wildcardFields)
        {
            var columnNames = new List<string>();

            foreach (var col in columns)
            {
                if (col.IsWildcard && wildcardFields != null)
                {
                    foreach (var field in wildcardFields)
                        columnNames.Add(field.Name);
                }
                else if (!col.IsWildcard)
                {
                    columnNames.Add(col.Alias ?? GetExpressionName(col.Expression));
                }
            }

            return columnNames;
        }

        /// <summary>
        /// JOIN結果に対してGROUP BYを実行する。
        /// </summary>
        private void ExecuteJoinedGroupBy(SelectStatement stmt, List<JoinedRecord> joinedRecords, SqlQueryResult result)
        {
            var groups = GroupJoinedRecordsByKey(joinedRecords, stmt.GroupBy);
            var filteredGroups = ApplyJoinedHavingFilter(groups, stmt.HavingClause);

            var resultRows = BuildJoinedGroupByResultRows(filteredGroups, stmt.Columns);
            resultRows = ApplySortAndPaginationToResultRows(resultRows, stmt);

            result.ColumnNames = BuildJoinedGroupByColumnNames(stmt.Columns);
            result.Records = resultRows.Cast<object>().ToList();
        }

        /// <summary>
        /// JoinedRecordをGROUP BYキーでグループ化する。
        /// </summary>
        private Dictionary<string, (Dictionary<string, object> keyValues, List<JoinedRecord> records)> GroupJoinedRecordsByKey(
            List<JoinedRecord> joinedRecords,
            List<GroupByItem> groupByItems)
        {
            var groups = new Dictionary<string, (Dictionary<string, object> keyValues, List<JoinedRecord> records)>();

            if (groupByItems.Count == 0)
            {
                groups[""] = (new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), joinedRecords);
                return groups;
            }

            foreach (var jr in joinedRecords)
            {
                var (key, keyValues) = BuildJoinedGroupKey(jr, groupByItems);

                if (!groups.TryGetValue(key, out var group))
                {
                    group = (keyValues, new List<JoinedRecord>());
                    groups[key] = group;
                }
                group.records.Add(jr);
            }

            return groups;
        }

        /// <summary>
        /// JoinedRecordからグループキーを構築する。
        /// </summary>
        private (string key, Dictionary<string, object> keyValues) BuildJoinedGroupKey(
            JoinedRecord jr,
            List<GroupByItem> groupByItems)
        {
            var keyValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var keyParts = new List<string>();

            foreach (var groupItem in groupByItems)
            {
                var value = ResolveJoinedValue(groupItem.Expression, jr);
                var columnName = GetExpressionName(groupItem.Expression);
                keyValues[columnName] = value;
                keyParts.Add(value?.ToString() ?? "NULL");
            }

            return (string.Join("|", keyParts), keyValues);
        }

        /// <summary>
        /// HAVING句でJOINグループをフィルタリングする。
        /// </summary>
        private List<(Dictionary<string, object> keyValues, List<JoinedRecord> records)> ApplyJoinedHavingFilter(
            Dictionary<string, (Dictionary<string, object> keyValues, List<JoinedRecord> records)> groups,
            SqlExpression havingClause)
        {
            var filteredGroups = groups.Values.ToList();

            if (havingClause == null)
                return filteredGroups;

            return filteredGroups
                .Where(g => EvaluateJoinedHavingCondition(havingClause, g.records))
                .ToList();
        }

        /// <summary>
        /// JOIN GROUP BY結果からResultRowリストを構築する。
        /// </summary>
        private List<ResultRow> BuildJoinedGroupByResultRows(
            List<(Dictionary<string, object> keyValues, List<JoinedRecord> records)> groups,
            List<SelectColumn> columns)
        {
            var resultRows = new List<ResultRow>();

            foreach (var group in groups)
            {
                var row = BuildJoinedGroupByResultRow(group.keyValues, group.records, columns);
                resultRows.Add(row);
            }

            return resultRows;
        }

        /// <summary>
        /// 単一JOINグループからResultRowを構築する。
        /// </summary>
        private ResultRow BuildJoinedGroupByResultRow(
            Dictionary<string, object> groupKeyValues,
            List<JoinedRecord> groupRecords,
            List<SelectColumn> columns)
        {
            var row = new ResultRow();

            foreach (var col in columns)
            {
                if (col.IsWildcard)
                    continue;

                var colName = col.Alias ?? GetExpressionName(col.Expression);
                row.Values[colName] = ResolveJoinedGroupByColumnValue(col, groupKeyValues, groupRecords);
            }

            return row;
        }

        /// <summary>
        /// JOIN GROUP BYコンテキストでカラム値を解決する。
        /// </summary>
        private object ResolveJoinedGroupByColumnValue(
            SelectColumn col,
            Dictionary<string, object> groupKeyValues,
            List<JoinedRecord> groupRecords)
        {
            if (col.Expression is AggregateExpression aggExpr)
                return EvaluateJoinedAggregate(aggExpr, groupRecords);

            var exprName = GetExpressionName(col.Expression);
            if (groupKeyValues.TryGetValue(exprName, out var keyValue))
                return keyValue;

            return groupRecords.Count > 0
                ? ResolveJoinedValue(col.Expression, groupRecords[0])
                : null;
        }

        /// <summary>
        /// JOIN GROUP BY結果のカラム名リストを構築する。
        /// </summary>
        private List<string> BuildJoinedGroupByColumnNames(List<SelectColumn> columns)
        {
            return columns
                .Where(c => !c.IsWildcard)
                .Select(c => c.Alias ?? GetExpressionName(c.Expression))
                .ToList();
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
            return expr switch
            {
                LiteralExpression literal => literal.Value,
                ColumnExpression column => ResolveJoinedColumnValue(column, joinedRecord),
                ArithmeticExpression arith => EvaluateJoinedArithmetic(arith, joinedRecord),
                AggregateExpression or CaseExpression or FunctionCallExpression => null,
                _ => null
            };
        }

        /// <summary>
        /// JOIN結果からカラム値を取得する。
        /// </summary>
        private object ResolveJoinedColumnValue(ColumnExpression column, JoinedRecord joinedRecord)
        {
            if (!string.IsNullOrEmpty(column.TableAlias))
                return ResolveQualifiedJoinedColumnValue(column, joinedRecord);

            return ResolveUnqualifiedJoinedColumnValue(column.ColumnName, joinedRecord);
        }

        /// <summary>
        /// テーブル修飾付きのJOINカラム値を取得する。
        /// </summary>
        private object ResolveQualifiedJoinedColumnValue(ColumnExpression column, JoinedRecord joinedRecord)
        {
            var record = joinedRecord.GetRecord(column.TableAlias);
            var recordType = joinedRecord.GetRecordType(column.TableAlias);

            if (record != null && recordType != null)
                return GetFieldValue(record, recordType, column.ColumnName);

            return null;
        }

        /// <summary>
        /// テーブル修飾なしのJOINカラム値を取得する（全テーブルから検索）。
        /// </summary>
        private object ResolveUnqualifiedJoinedColumnValue(string columnName, JoinedRecord joinedRecord)
        {
            foreach (var kvp in joinedRecord.TableRecords)
            {
                var record = kvp.Value;
                if (record == null)
                    continue;

                var recordType = joinedRecord.GetRecordType(kvp.Key);
                if (HasField(recordType, columnName))
                    return GetFieldValue(record, recordType, columnName);
            }

            return null;
        }

        /// <summary>
        /// 指定した型にフィールドまたはプロパティが存在するかチェックする。
        /// </summary>
        private bool HasField(Type recordType, string fieldName)
        {
            // フィールドを検索
            var field = recordType.GetField(fieldName, MemberFlags);
            if (field != null)
                return true;

            // 大文字小文字を無視して検索
            field = recordType.GetFields(MemberFlags)
                .FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            if (field != null)
                return true;

            // プロパティを検索
            var property = recordType.GetProperty(fieldName, MemberFlags);
            if (property?.CanRead == true)
                return true;

            property = recordType.GetProperties(MemberFlags)
                .FirstOrDefault(p => p.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            if (property?.CanRead == true)
                return true;

            return false;
        }

        /// <summary>
        /// JOIN結果に対して算術式を評価する。
        /// </summary>
        private object EvaluateJoinedArithmetic(ArithmeticExpression expr, JoinedRecord joinedRecord)
        {
            var left = ResolveJoinedValue(expr.Left, joinedRecord);
            var right = ResolveJoinedValue(expr.Right, joinedRecord);

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
        /// DISTINCTを適用する。
        /// </summary>
        private List<object> ApplyDistinct(List<object> records, List<SelectColumn> columns, Type recordType)
        {
            var seen = new HashSet<string>();
            var result = new List<object>();

            foreach (var record in records)
            {
                var key = BuildDistinctKey(record, columns, recordType);
                if (seen.Add(key))
                    result.Add(record);
            }

            return result;
        }

        /// <summary>
        /// レコードからDISTINCT判定用のキー文字列を構築する。
        /// </summary>
        private string BuildDistinctKey(object record, List<SelectColumn> columns, Type recordType)
        {
            var keyParts = new List<string>();

            foreach (var col in columns)
                AddDistinctKeyParts(keyParts, col, record, recordType);

            return string.Join("|", keyParts);
        }

        /// <summary>
        /// カラムの値をキーパーツに追加する。
        /// </summary>
        private void AddDistinctKeyParts(List<string> keyParts, SelectColumn col, object record, Type recordType)
        {
            if (col.IsWildcard)
            {
                foreach (var field in ReflectionUtility.GetSerializableFields(recordType))
                {
                    var value = field.GetValue(record);
                    keyParts.Add(value?.ToString() ?? "NULL");
                }
                return;
            }

            if (col.Expression is ColumnExpression colExpr)
            {
                var value = GetFieldValue(record, recordType, colExpr.ColumnName);
                keyParts.Add(value?.ToString() ?? "NULL");
            }
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
                var (row, key) = BuildDistinctRowAndKey(record, columns, recordType);
                if (seen.Add(key))
                    result.Add(row);
            }

            return result;
        }

        /// <summary>
        /// レコードからResultRowとDISTINCT判定用のキー文字列を構築する。
        /// </summary>
        private (ResultRow row, string key) BuildDistinctRowAndKey(object record, List<SelectColumn> columns, Type recordType)
        {
            var row = new ResultRow { SourceRecord = record };
            var keyParts = new List<string>();

            foreach (var col in columns)
                AddDistinctRowAndKeyParts(row, keyParts, col, record, recordType);

            return (row, string.Join("|", keyParts));
        }

        /// <summary>
        /// カラムの値をResultRowとキーパーツに追加する。
        /// </summary>
        private void AddDistinctRowAndKeyParts(ResultRow row, List<string> keyParts, SelectColumn col, object record, Type recordType)
        {
            if (col.IsWildcard)
            {
                foreach (var field in ReflectionUtility.GetSerializableFields(recordType))
                {
                    var value = field.GetValue(record);
                    row.Values[field.Name] = value;
                    keyParts.Add(value?.ToString() ?? "NULL");
                }
                return;
            }

            var colName = col.Alias ?? GetExpressionName(col.Expression);
            var colValue = ResolveValue(col.Expression, record, recordType);
            row.Values[colName] = colValue;
            keyParts.Add(colValue?.ToString() ?? "NULL");
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

            return expr.Operator switch
            {
                ComparisonOperator.IsNull => leftValue == null,
                ComparisonOperator.IsNotNull => leftValue != null,
                ComparisonOperator.In => EvaluateInOperator(leftValue, expr.Right, record, recordType),
                _ => EvaluateStandardComparison(expr.Operator, leftValue, expr.Right, record, recordType)
            };
        }

        /// <summary>
        /// IN演算子を評価する。
        /// </summary>
        private bool EvaluateInOperator(object leftValue, SqlExpression rightExpr, object record, Type recordType)
        {
            if (rightExpr is SubqueryExpression subqueryExpr)
                return EvaluateInSubquery(leftValue, subqueryExpr);

            if (rightExpr is InListExpression inList)
                return EvaluateInList(leftValue, inList, record, recordType);

            return false;
        }

        /// <summary>
        /// INサブクエリを評価する。
        /// </summary>
        private bool EvaluateInSubquery(object leftValue, SubqueryExpression subqueryExpr)
        {
            var subqueryValues = ExecuteSubquery(subqueryExpr.Subquery);

            foreach (var subValue in subqueryValues)
            {
                if (AreEqual(leftValue, subValue))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// INリストを評価する。
        /// </summary>
        private bool EvaluateInList(object leftValue, InListExpression inList, object record, Type recordType)
        {
            foreach (var item in inList.Values)
            {
                var itemValue = ResolveValue(item, record, recordType);
                if (AreEqual(leftValue, itemValue))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 標準的な比較演算子を評価する。
        /// </summary>
        private bool EvaluateStandardComparison(ComparisonOperator op, object leftValue, SqlExpression rightExpr, object record, Type recordType)
        {
            var rightValue = ResolveRightValue(rightExpr, record, recordType);

            return op switch
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

        /// <summary>
        /// 右辺の値を解決する（サブクエリまたは通常の式）。
        /// </summary>
        private object ResolveRightValue(SqlExpression rightExpr, object record, Type recordType)
        {
            if (rightExpr is SubqueryExpression subquery)
            {
                var subqueryValues = ExecuteSubquery(subquery.Subquery);
                return subqueryValues.FirstOrDefault();
            }

            return ResolveValue(rightExpr, record, recordType);
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
                    continue;
                }
                // 通常レコードの場合、選択されたカラムの値を取得
                if (subquery.Columns.Count > 0 && !subquery.Columns[0].IsWildcard)
                {
                    var colExpr = subquery.Columns[0].Expression;
                    var value = ResolveValue(colExpr, record, record.GetType());
                    values.Add(value);
                    continue;
                }
                // ワイルドカードの場合、レコード全体を追加
                values.Add(record);
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
                    continue;
                }
                ordered = item.Order == SortOrder.Descending
                    ? ordered.ThenByDescending(keySelector, new ObjectComparer())
                    : ordered.ThenBy(keySelector, new ObjectComparer());
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
                else
                {
                    // エイリアスがあればそれを使用、なければ式の名前を取得
                    var colName = column.Alias ?? GetExpressionName(column.Expression);
                    names.Add(colName);
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
