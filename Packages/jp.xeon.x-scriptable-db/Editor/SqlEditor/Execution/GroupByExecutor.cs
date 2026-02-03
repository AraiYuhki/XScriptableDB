using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SQL GROUP BY処理を実行するクラス。
    /// </summary>
    public class GroupByExecutor
    {
        private readonly SqlFieldAccessor fieldAccessor;
        private readonly ExpressionEvaluator expressionEvaluator;
        private readonly AggregateCalculator aggregateCalculator;

        public GroupByExecutor(
            SqlFieldAccessor fieldAccessor,
            ExpressionEvaluator expressionEvaluator,
            AggregateCalculator aggregateCalculator)
        {
            this.fieldAccessor = fieldAccessor;
            this.expressionEvaluator = expressionEvaluator;
            this.aggregateCalculator = aggregateCalculator;
        }

        /// <summary>
        /// レコードをGROUP BYキーでグループ化する。
        /// </summary>
        public Dictionary<string, AggregateGroup> GroupRecordsByKey(
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
        /// HAVING句でグループをフィルタリングする。
        /// </summary>
        public List<AggregateGroup> ApplyHavingFilter(
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
        /// GROUP BY結果からResultRowリストを構築する。
        /// </summary>
        public List<ResultRow> BuildGroupByResultRows(
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
        /// GROUP BY結果のカラム名リストを構築する。
        /// </summary>
        public List<string> BuildGroupByColumnNames(List<SelectColumn> columns, List<FieldInfo> wildcardFields)
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
                    columnNames.Add(col.Alias ?? fieldAccessor.GetExpressionName(col.Expression));
                }
            }

            return columnNames;
        }

        /// <summary>
        /// ワイルドカードがある場合のフィールドリストを取得する。
        /// </summary>
        public List<FieldInfo> GetWildcardFieldsIfNeeded(List<SelectColumn> columns, Type recordType)
        {
            var hasWildcard = columns.Any(c => c.IsWildcard);
            return hasWildcard ? ReflectionUtility.GetSerializableFields(recordType).ToList() : null;
        }

        /// <summary>
        /// ResultRowリストにソートとページネーションを適用する。
        /// </summary>
        public List<ResultRow> ApplySortAndPagination(
            List<ResultRow> rows,
            List<OrderByItem> orderBy,
            int? offset,
            int? limit)
        {
            if (orderBy != null && orderBy.Count > 0)
                rows = SortResultRows(rows, orderBy);

            if (offset.HasValue && offset.Value > 0)
                rows = rows.Skip(offset.Value).ToList();

            if (limit.HasValue)
                rows = rows.Take(limit.Value).ToList();

            return rows;
        }

        #region Joined GROUP BY

        /// <summary>
        /// JoinedRecordをGROUP BYキーでグループ化する。
        /// </summary>
        public Dictionary<string, (Dictionary<string, object> keyValues, List<JoinedRecord> records)> GroupJoinedRecordsByKey(
            List<JoinedRecord> joinedRecords,
            List<GroupByItem> groupByItems,
            Func<SqlExpression, JoinedRecord, object> valueResolver)
        {
            var groups = new Dictionary<string, (Dictionary<string, object> keyValues, List<JoinedRecord> records)>();

            if (groupByItems.Count == 0)
            {
                groups[""] = (new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), joinedRecords);
                return groups;
            }

            foreach (var jr in joinedRecords)
            {
                var (key, keyValues) = BuildJoinedGroupKey(jr, groupByItems, valueResolver);

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
        /// HAVING句でJOINグループをフィルタリングする。
        /// </summary>
        public List<(Dictionary<string, object> keyValues, List<JoinedRecord> records)> ApplyJoinedHavingFilter(
            Dictionary<string, (Dictionary<string, object> keyValues, List<JoinedRecord> records)> groups,
            SqlExpression havingClause,
            Func<SqlExpression, JoinedRecord, object> valueResolver)
        {
            var filteredGroups = groups.Values.ToList();

            if (havingClause == null)
                return filteredGroups;

            return filteredGroups
                .Where(g => EvaluateJoinedHavingCondition(havingClause, g.records, valueResolver))
                .ToList();
        }

        /// <summary>
        /// JOIN GROUP BY結果からResultRowリストを構築する。
        /// </summary>
        public List<ResultRow> BuildJoinedGroupByResultRows(
            List<(Dictionary<string, object> keyValues, List<JoinedRecord> records)> groups,
            List<SelectColumn> columns,
            Func<SqlExpression, JoinedRecord, object> valueResolver)
        {
            var resultRows = new List<ResultRow>();

            foreach (var group in groups)
            {
                var row = BuildJoinedGroupByResultRow(group.keyValues, group.records, columns, valueResolver);
                resultRows.Add(row);
            }

            return resultRows;
        }

        /// <summary>
        /// JOIN GROUP BY結果のカラム名リストを構築する。
        /// </summary>
        public List<string> BuildJoinedGroupByColumnNames(List<SelectColumn> columns)
        {
            return columns
                .Where(c => !c.IsWildcard)
                .Select(c => c.Alias ?? fieldAccessor.GetExpressionName(c.Expression))
                .ToList();
        }

        #endregion

        #region Private Methods

        private (string key, Dictionary<string, object> keyValues) BuildGroupKey(
            object record,
            List<GroupByItem> groupByItems,
            Type recordType)
        {
            var keyValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var keyParts = new List<string>();

            foreach (var groupItem in groupByItems)
            {
                var value = expressionEvaluator.ResolveValue(groupItem.Expression, record, recordType);
                var columnName = fieldAccessor.GetExpressionName(groupItem.Expression);
                keyValues[columnName] = value;
                keyParts.Add(value?.ToString() ?? "NULL");
            }

            return (string.Join("|", keyParts), keyValues);
        }

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

                var colName = col.Alias ?? fieldAccessor.GetExpressionName(col.Expression);
                row.Values[colName] = ResolveGroupByColumnValue(col, group, recordType);
            }

            return row;
        }

        private void AddWildcardValuesToRow(ResultRow row, AggregateGroup group, List<FieldInfo> wildcardFields)
        {
            if (group.Records.Count == 0 || wildcardFields == null)
                return;

            var firstRecord = group.Records[0];
            foreach (var field in wildcardFields)
                row.Values[field.Name] = field.GetValue(firstRecord);
        }

        private object ResolveGroupByColumnValue(SelectColumn col, AggregateGroup group, Type recordType)
        {
            if (col.Expression is AggregateExpression aggExpr)
                return aggregateCalculator.EvaluateAggregate(aggExpr, group.Records, recordType);

            var exprName = fieldAccessor.GetExpressionName(col.Expression);
            if (group.GroupKeyValues.TryGetValue(exprName, out var keyValue))
                return keyValue;

            return group.Records.Count > 0
                ? expressionEvaluator.ResolveValue(col.Expression, group.Records[0], recordType)
                : null;
        }

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
                ComparisonOperator.Equal => fieldAccessor.AreEqual(leftValue, rightValue),
                ComparisonOperator.NotEqual => !fieldAccessor.AreEqual(leftValue, rightValue),
                ComparisonOperator.LessThan => fieldAccessor.Compare(leftValue, rightValue) < 0,
                ComparisonOperator.LessOrEqual => fieldAccessor.Compare(leftValue, rightValue) <= 0,
                ComparisonOperator.GreaterThan => fieldAccessor.Compare(leftValue, rightValue) > 0,
                ComparisonOperator.GreaterOrEqual => fieldAccessor.Compare(leftValue, rightValue) >= 0,
                _ => false
            };
        }

        private object ResolveHavingValue(SqlExpression expr, AggregateGroup group, Type recordType)
        {
            return expr switch
            {
                AggregateExpression aggExpr => aggregateCalculator.EvaluateAggregate(aggExpr, group.Records, recordType),
                LiteralExpression literal => literal.Value,
                ColumnExpression column => group.Records.Count > 0
                    ? fieldAccessor.GetFieldValue(group.Records[0], recordType, column.ColumnName)
                    : null,
                _ => null
            };
        }

        private List<ResultRow> SortResultRows(List<ResultRow> rows, List<OrderByItem> orderBy)
        {
            IOrderedEnumerable<ResultRow> ordered = null;

            for (var i = 0; i < orderBy.Count; i++)
            {
                var item = orderBy[i];
                var columnName = fieldAccessor.GetExpressionName(item.Expression);

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

        private (string key, Dictionary<string, object> keyValues) BuildJoinedGroupKey(
            JoinedRecord jr,
            List<GroupByItem> groupByItems,
            Func<SqlExpression, JoinedRecord, object> valueResolver)
        {
            var keyValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var keyParts = new List<string>();

            foreach (var groupItem in groupByItems)
            {
                var value = valueResolver(groupItem.Expression, jr);
                var columnName = fieldAccessor.GetExpressionName(groupItem.Expression);
                keyValues[columnName] = value;
                keyParts.Add(value?.ToString() ?? "NULL");
            }

            return (string.Join("|", keyParts), keyValues);
        }

        private bool EvaluateJoinedHavingCondition(
            SqlExpression expr,
            List<JoinedRecord> groupRecords,
            Func<SqlExpression, JoinedRecord, object> valueResolver)
        {
            switch (expr)
            {
                case LogicalExpression logical:
                    var leftResult = EvaluateJoinedHavingCondition(logical.Left, groupRecords, valueResolver);
                    var rightResult = EvaluateJoinedHavingCondition(logical.Right, groupRecords, valueResolver);

                    return logical.Operator switch
                    {
                        LogicalOperator.And => leftResult && rightResult,
                        LogicalOperator.Or => leftResult || rightResult,
                        _ => false
                    };

                case ComparisonExpression comparison:
                    return EvaluateJoinedHavingComparison(comparison, groupRecords, valueResolver);

                default:
                    return false;
            }
        }

        private bool EvaluateJoinedHavingComparison(
            ComparisonExpression expr,
            List<JoinedRecord> groupRecords,
            Func<SqlExpression, JoinedRecord, object> valueResolver)
        {
            var leftValue = ResolveJoinedHavingValue(expr.Left, groupRecords, valueResolver);
            var rightValue = ResolveJoinedHavingValue(expr.Right, groupRecords, valueResolver);

            return expr.Operator switch
            {
                ComparisonOperator.Equal => fieldAccessor.AreEqual(leftValue, rightValue),
                ComparisonOperator.NotEqual => !fieldAccessor.AreEqual(leftValue, rightValue),
                ComparisonOperator.LessThan => fieldAccessor.Compare(leftValue, rightValue) < 0,
                ComparisonOperator.LessOrEqual => fieldAccessor.Compare(leftValue, rightValue) <= 0,
                ComparisonOperator.GreaterThan => fieldAccessor.Compare(leftValue, rightValue) > 0,
                ComparisonOperator.GreaterOrEqual => fieldAccessor.Compare(leftValue, rightValue) >= 0,
                _ => false
            };
        }

        private object ResolveJoinedHavingValue(
            SqlExpression expr,
            List<JoinedRecord> groupRecords,
            Func<SqlExpression, JoinedRecord, object> valueResolver)
        {
            return expr switch
            {
                AggregateExpression aggExpr => aggregateCalculator.EvaluateJoinedAggregate(aggExpr, groupRecords, valueResolver),
                LiteralExpression literal => literal.Value,
                ColumnExpression => groupRecords.Count > 0
                    ? valueResolver(expr, groupRecords[0])
                    : null,
                _ => null
            };
        }

        private ResultRow BuildJoinedGroupByResultRow(
            Dictionary<string, object> groupKeyValues,
            List<JoinedRecord> groupRecords,
            List<SelectColumn> columns,
            Func<SqlExpression, JoinedRecord, object> valueResolver)
        {
            var row = new ResultRow();

            foreach (var col in columns)
            {
                if (col.IsWildcard)
                    continue;

                var colName = col.Alias ?? fieldAccessor.GetExpressionName(col.Expression);
                row.Values[colName] = ResolveJoinedGroupByColumnValue(col, groupKeyValues, groupRecords, valueResolver);
            }

            return row;
        }

        private object ResolveJoinedGroupByColumnValue(
            SelectColumn col,
            Dictionary<string, object> groupKeyValues,
            List<JoinedRecord> groupRecords,
            Func<SqlExpression, JoinedRecord, object> valueResolver)
        {
            if (col.Expression is AggregateExpression aggExpr)
                return aggregateCalculator.EvaluateJoinedAggregate(aggExpr, groupRecords, valueResolver);

            var exprName = fieldAccessor.GetExpressionName(col.Expression);
            if (groupKeyValues.TryGetValue(exprName, out var keyValue))
                return keyValue;

            return groupRecords.Count > 0
                ? valueResolver(col.Expression, groupRecords[0])
                : null;
        }

        #endregion
    }
}
