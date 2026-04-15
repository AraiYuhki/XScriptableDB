using System;
using System.Collections.Generic;
using System.Linq;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Class that executes SQL JOIN processing.
    /// </summary>
    public class JoinExecutor
    {
        private readonly SqlFieldAccessor fieldAccessor;

        public JoinExecutor(SqlFieldAccessor fieldAccessor)
        {
            this.fieldAccessor = fieldAccessor;
        }

        /// <summary>
        /// Executes a JOIN (INNER/LEFT/CROSS).
        /// </summary>
        public List<JoinedRecord> ExecuteJoins(
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
        /// Processes a RIGHT JOIN.
        /// </summary>
        public void ProcessRightJoins(
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
        /// Applies a WHERE clause to JoinedRecords.
        /// </summary>
        public List<JoinedRecord> ApplyWhereToJoinedRecords(
            List<JoinedRecord> records,
            SqlExpression whereClause,
            Func<SqlExpression, JoinedRecord, bool> evaluator)
        {
            if (whereClause == null)
                return records;

            return records.Where(jr => evaluator(whereClause, jr)).ToList();
        }

        /// <summary>
        /// Applies sorting and pagination to JoinedRecords.
        /// </summary>
        public List<JoinedRecord> ApplySortAndPagination(
            List<JoinedRecord> records,
            List<OrderByItem> orderBy,
            int? offset,
            int? limit,
            Func<SqlExpression, JoinedRecord, object> valueResolver)
        {
            if (orderBy != null && orderBy.Count > 0)
                records = SortJoinedRecords(records, orderBy, valueResolver);

            if (offset.HasValue && offset.Value > 0)
                records = records.Skip(offset.Value).ToList();

            if (limit.HasValue)
                records = records.Take(limit.Value).ToList();

            return records;
        }

        /// <summary>
        /// Builds the column name list for the JOIN result.
        /// </summary>
        public List<string> BuildJoinedColumnNames(
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
        /// Converts JoinedRecords to ResultRows.
        /// </summary>
        public List<ResultRow> ConvertToResultRows(
            List<JoinedRecord> joinedRecords,
            List<string> columnNames,
            Func<JoinedRecord, string, object> valueResolver)
        {
            var resultRows = new List<ResultRow>();

            foreach (var jr in joinedRecords)
            {
                var row = new ResultRow();

                foreach (var colName in columnNames)
                    row.Values[colName] = valueResolver(jr, colName);

                resultRows.Add(row);
            }

            return resultRows;
        }

        /// <summary>
        /// Resolves a value from the JOIN result.
        /// </summary>
        public object ResolveJoinedValue(SqlExpression expr, JoinedRecord joinedRecord)
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
        /// Evaluates a JOIN condition.
        /// </summary>
        public bool EvaluateJoinCondition(SqlExpression expr, JoinedRecord joinedRecord)
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

        /// <summary>
        /// Resolves a column value from a JoinedRecord.
        /// </summary>
        public object ResolveColumnValueFromJoinedRecord(JoinedRecord jr, string colName)
        {
            if (colName.Contains('.'))
                return ResolveQualifiedColumnValue(jr, colName);

            return ResolveUnqualifiedColumnValue(jr, colName);
        }

        #region Private Methods

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

        private List<JoinedRecord> SortJoinedRecords(
            List<JoinedRecord> records,
            List<OrderByItem> orderBy,
            Func<SqlExpression, JoinedRecord, object> valueResolver)
        {
            IOrderedEnumerable<JoinedRecord> ordered = null;

            for (var i = 0; i < orderBy.Count; i++)
            {
                var item = orderBy[i];
                Func<JoinedRecord, object> keySelector = r => valueResolver(item.Expression, r);

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

        private void AddWildcardColumnNames(List<string> columnNames, Type recordType, string alias)
        {
            foreach (var field in ReflectionUtility.GetSerializableFields(recordType))
                columnNames.Add($"{alias}.{field.Name}");
        }

        private string GetJoinedColumnName(SelectColumn col)
        {
            if (col.Expression is ColumnExpression colExpr)
                return colExpr.ToString();
            return col.Expression.ToString();
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
                        ComparisonOperator.Equal => fieldAccessor.AreEqual(leftValue, rightValue),
                        ComparisonOperator.NotEqual => !fieldAccessor.AreEqual(leftValue, rightValue),
                        ComparisonOperator.LessThan => fieldAccessor.Compare(leftValue, rightValue) < 0,
                        ComparisonOperator.LessOrEqual => fieldAccessor.Compare(leftValue, rightValue) <= 0,
                        ComparisonOperator.GreaterThan => fieldAccessor.Compare(leftValue, rightValue) > 0,
                        ComparisonOperator.GreaterOrEqual => fieldAccessor.Compare(leftValue, rightValue) >= 0,
                        _ => false
                    };
            }
        }

        private object ResolveJoinedColumnValue(ColumnExpression column, JoinedRecord joinedRecord)
        {
            if (!string.IsNullOrEmpty(column.TableAlias))
                return ResolveQualifiedJoinedColumnValue(column, joinedRecord);

            return ResolveUnqualifiedJoinedColumnValue(column.ColumnName, joinedRecord);
        }

        private object ResolveQualifiedJoinedColumnValue(ColumnExpression column, JoinedRecord joinedRecord)
        {
            var record = joinedRecord.GetRecord(column.TableAlias);
            var recordType = joinedRecord.GetRecordType(column.TableAlias);

            if (record != null && recordType != null)
                return fieldAccessor.GetFieldValue(record, recordType, column.ColumnName);

            return null;
        }

        private object ResolveUnqualifiedJoinedColumnValue(string columnName, JoinedRecord joinedRecord)
        {
            foreach (var kvp in joinedRecord.TableRecords)
            {
                var record = kvp.Value;
                if (record == null)
                    continue;

                var recordType = joinedRecord.GetRecordType(kvp.Key);
                if (fieldAccessor.HasField(recordType, columnName))
                    return fieldAccessor.GetFieldValue(record, recordType, columnName);
            }

            return null;
        }

        private object ResolveQualifiedColumnValue(JoinedRecord jr, string colName)
        {
            var parts = colName.Split('.');
            var tableAlias = parts[0];
            var fieldName = parts[1];

            var record = jr.GetRecord(tableAlias);
            var recordType = jr.GetRecordType(tableAlias);

            if (record != null && recordType != null)
                return fieldAccessor.GetFieldValue(record, recordType, fieldName);

            return null;
        }

        private object ResolveUnqualifiedColumnValue(JoinedRecord jr, string colName)
        {
            foreach (var kvp in jr.TableRecords)
            {
                var record = kvp.Value;
                if (record == null)
                    continue;

                var recordType = jr.GetRecordType(kvp.Key);
                if (fieldAccessor.HasField(recordType, colName))
                    return fieldAccessor.GetFieldValue(record, recordType, colName);
            }

            return null;
        }

        private object EvaluateJoinedArithmetic(ArithmeticExpression expr, JoinedRecord joinedRecord)
        {
            var left = ResolveJoinedValue(expr.Left, joinedRecord);
            var right = ResolveJoinedValue(expr.Right, joinedRecord);

            if (!fieldAccessor.IsNumeric(left) || !fieldAccessor.IsNumeric(right))
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

        #endregion
    }
}
