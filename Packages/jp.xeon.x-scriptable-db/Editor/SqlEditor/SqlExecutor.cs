using System;
using System.Collections.Generic;
using System.Linq;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Class that executes SQL.
    /// </summary>
    public class SqlExecutor
    {
        private readonly Dictionary<string, ITableAsset> tables = new(StringComparer.OrdinalIgnoreCase);

        private readonly SqlFieldAccessor fieldAccessor;
        private readonly ExpressionEvaluator expressionEvaluator;
        private readonly AggregateCalculator aggregateCalculator;
        private readonly JoinExecutor joinExecutor;
        private readonly GroupByExecutor groupByExecutor;

        public SqlExecutor()
        {
            fieldAccessor = new SqlFieldAccessor();
            expressionEvaluator = new ExpressionEvaluator(fieldAccessor);
            aggregateCalculator = new AggregateCalculator(fieldAccessor, expressionEvaluator);
            joinExecutor = new JoinExecutor(fieldAccessor);
            groupByExecutor = new GroupByExecutor(fieldAccessor, expressionEvaluator, aggregateCalculator);

            expressionEvaluator.SetSubqueryExecutor(ExecuteSubquery);
        }

        /// <summary>
        /// Registers a table.
        /// </summary>
        public void RegisterTable(string tableName, ITableAsset table)
        {
            tables[tableName] = table;
        }

        /// <summary>
        /// Gets the registered table.
        /// </summary>
        public ITableAsset GetTable(string tableName)
        {
            return tables.GetValueOrDefault(tableName);
        }

        /// <summary>
        /// Gets the list of registered table names.
        /// </summary>
        public IReadOnlyCollection<string> TableNames => tables.Keys;

        /// <summary>
        /// Parses and executes a SQL string.
        /// </summary>
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

        #region SELECT Execution

        private void ExecuteSelect(SelectStatement stmt, SqlQueryResult result)
        {
            if (!TryGetMainTable(stmt, result, out var mainTable, out var mainAlias))
                return;

            if (stmt.Joins.Count > 0)
            {
                ExecuteSelectWithJoin(stmt, mainTable, mainAlias, result);
                return;
            }

            if (stmt.GroupBy.Count > 0 || aggregateCalculator.HasAggregateFunction(stmt.Columns))
            {
                ExecuteSelectWithGroupBy(stmt, mainTable, result);
                return;
            }

            ExecuteSimpleSelect(stmt, mainTable, result);
        }

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

        private void ExecuteSelectWithJoin(SelectStatement stmt, ITableAsset mainTable, string mainAlias, SqlQueryResult result)
        {
            if (!TryGetJoinTables(stmt.Joins, result, out var joinTables))
                return;

            var joinedRecords = joinExecutor.ExecuteJoins(mainTable, mainAlias, joinTables);
            joinExecutor.ProcessRightJoins(joinedRecords, mainTable, mainAlias, joinTables);

            joinedRecords = joinExecutor.ApplyWhereToJoinedRecords(
                joinedRecords,
                stmt.WhereClause,
                joinExecutor.EvaluateJoinCondition);

            if (stmt.GroupBy.Count > 0 || aggregateCalculator.HasAggregateFunction(stmt.Columns))
            {
                ExecuteJoinedGroupBy(stmt, joinedRecords, result);
                return;
            }

            joinedRecords = joinExecutor.ApplySortAndPagination(
                joinedRecords,
                stmt.OrderBy,
                stmt.Offset,
                stmt.Limit,
                joinExecutor.ResolveJoinedValue);

            var columnNames = joinExecutor.BuildJoinedColumnNames(stmt.Columns, mainTable, mainAlias, joinTables);
            var resultRows = joinExecutor.ConvertToResultRows(
                joinedRecords,
                columnNames,
                joinExecutor.ResolveColumnValueFromJoinedRecord);

            result.ColumnNames = columnNames;
            result.Records = resultRows.Cast<object>().ToList();
        }

        private void ExecuteSelectWithGroupBy(SelectStatement stmt, ITableAsset table, SqlQueryResult result)
        {
            var recordType = table.RecordType;
            var records = FilterRecords(table.Records.Cast<object>(), stmt.WhereClause, recordType);

            var groups = groupByExecutor.GroupRecordsByKey(records, stmt.GroupBy, recordType);
            var filteredGroups = groupByExecutor.ApplyHavingFilter(groups, stmt.HavingClause, recordType);

            var wildcardFields = groupByExecutor.GetWildcardFieldsIfNeeded(stmt.Columns, recordType);
            var resultRows = groupByExecutor.BuildGroupByResultRows(filteredGroups, stmt.Columns, recordType, wildcardFields);

            resultRows = groupByExecutor.ApplySortAndPagination(resultRows, stmt.OrderBy, stmt.Offset, stmt.Limit);

            result.ColumnNames = groupByExecutor.BuildGroupByColumnNames(stmt.Columns, wildcardFields);
            result.Records = resultRows.Cast<object>().ToList();
        }

        private void ExecuteJoinedGroupBy(SelectStatement stmt, List<JoinedRecord> joinedRecords, SqlQueryResult result)
        {
            var groups = groupByExecutor.GroupJoinedRecordsByKey(
                joinedRecords,
                stmt.GroupBy,
                joinExecutor.ResolveJoinedValue);

            var filteredGroups = groupByExecutor.ApplyJoinedHavingFilter(
                groups,
                stmt.HavingClause,
                joinExecutor.ResolveJoinedValue);

            var resultRows = groupByExecutor.BuildJoinedGroupByResultRows(
                filteredGroups,
                stmt.Columns,
                joinExecutor.ResolveJoinedValue);

            resultRows = groupByExecutor.ApplySortAndPagination(resultRows, stmt.OrderBy, stmt.Offset, stmt.Limit);

            result.ColumnNames = groupByExecutor.BuildJoinedGroupByColumnNames(stmt.Columns);
            result.Records = resultRows.Cast<object>().ToList();
        }

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

        #endregion

        #region UPDATE/DELETE Execution

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
                if (record == null)
                    continue;

                if (stmt.WhereClause != null && !expressionEvaluator.EvaluateExpression(stmt.WhereClause, record, recordType))
                    continue;

                foreach (var setItem in stmt.SetItems)
                {
                    var value = expressionEvaluator.ResolveValue(setItem.Value, record, recordType);
                    fieldAccessor.SetFieldValue(record, recordType, setItem.ColumnName, value);
                }
                affectedCount++;
            }

            result.AffectedCount = affectedCount;
        }

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
                    if (stmt.WhereClause == null || expressionEvaluator.EvaluateExpression(stmt.WhereClause, record, recordType))
                        indicesToRemove.Add(index);
                }
                index++;
            }

            for (var i = indicesToRemove.Count - 1; i >= 0; i--)
                table.RemoveRecordAt(indicesToRemove[i]);

            result.AffectedCount = indicesToRemove.Count;
        }

        #endregion

        #region Helper Methods

        private List<object> FilterRecords(IEnumerable<object> records, SqlExpression whereClause, Type recordType)
        {
            var result = new List<object>();

            foreach (var record in records)
            {
                if (record == null)
                    continue;

                if (whereClause == null || expressionEvaluator.EvaluateExpression(whereClause, record, recordType))
                    result.Add(record);
            }

            return result;
        }

        private List<object> ApplyOffsetAndLimit(List<object> records, int? offset, int? limit)
        {
            if (offset.HasValue && offset.Value > 0)
                records = records.Skip(offset.Value).ToList();

            if (limit.HasValue)
                records = records.Take(limit.Value).ToList();

            return records;
        }

        private List<object> SortRecords(List<object> records, List<OrderByItem> orderBy, Type recordType)
        {
            IOrderedEnumerable<object> ordered = null;

            for (var i = 0; i < orderBy.Count; i++)
            {
                var item = orderBy[i];
                var columnName = (item.Expression as ColumnExpression)?.ColumnName;
                if (string.IsNullOrEmpty(columnName))
                    continue;

                Func<object, object> keySelector = r => fieldAccessor.GetFieldValue(r, recordType, columnName);

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

        private List<string> GetColumnNames(List<SelectColumn> columns, Type recordType)
        {
            var names = new List<string>();

            foreach (var column in columns)
            {
                if (column.IsWildcard)
                {
                    foreach (var field in ReflectionUtility.GetSerializableFields(recordType))
                        names.Add(field.Name);
                }
                else
                {
                    var colName = column.Alias ?? fieldAccessor.GetExpressionName(column.Expression);
                    names.Add(colName);
                }
            }

            return names;
        }

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

        private ResultRow BuildResultRow(object record, List<SelectColumn> columns, Type recordType)
        {
            var row = new ResultRow { SourceRecord = record };

            foreach (var col in columns)
            {
                if (col.IsWildcard)
                {
                    foreach (var field in ReflectionUtility.GetSerializableFields(recordType))
                        row.Values[field.Name] = field.GetValue(record);
                }
                else
                {
                    var colName = col.Alias ?? fieldAccessor.GetExpressionName(col.Expression);
                    row.Values[colName] = expressionEvaluator.ResolveValue(col.Expression, record, recordType);
                }
            }

            return row;
        }

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

                if (!string.IsNullOrEmpty(col.Alias))
                    return true;
            }
            return false;
        }

        #endregion

        #region DISTINCT

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

        private string BuildDistinctKey(object record, List<SelectColumn> columns, Type recordType)
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
                    var value = fieldAccessor.GetFieldValue(record, recordType, colExpr.ColumnName);
                    keyParts.Add(value?.ToString() ?? "NULL");
                }
            }

            return string.Join("|", keyParts);
        }

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
                distinctRows = groupByExecutor.ApplySortAndPagination(distinctRows, stmt.OrderBy, null, null);

            if (stmt.Offset.HasValue && stmt.Offset.Value > 0)
                distinctRows = distinctRows.Skip(stmt.Offset.Value).ToList();

            if (stmt.Limit.HasValue)
                distinctRows = distinctRows.Take(stmt.Limit.Value).ToList();

            result.ColumnNames = GetColumnNames(stmt.Columns, recordType);
            result.Records = distinctRows.Cast<object>().ToList();
            return true;
        }

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
                        var colName = col.Alias ?? fieldAccessor.GetExpressionName(col.Expression);
                        var colValue = expressionEvaluator.ResolveValue(col.Expression, record, recordType);
                        row.Values[colName] = colValue;
                        keyParts.Add(colValue?.ToString() ?? "NULL");
                    }
                }

                var key = string.Join("|", keyParts);
                if (seen.Add(key))
                    result.Add(row);
            }

            return result;
        }

        #endregion

        #region Subquery

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
                    var firstValue = row.Values.Values.FirstOrDefault();
                    values.Add(firstValue);
                    continue;
                }

                if (subquery.Columns.Count > 0 && !subquery.Columns[0].IsWildcard)
                {
                    var colExpr = subquery.Columns[0].Expression;
                    var value = expressionEvaluator.ResolveValue(colExpr, record, record.GetType());
                    values.Add(value);
                    continue;
                }

                values.Add(record);
            }

            return values;
        }

        #endregion
    }
}
