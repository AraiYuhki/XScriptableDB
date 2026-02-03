using System;
using System.Collections.Generic;
using System.Linq;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SQL集計関数を計算するクラス。
    /// </summary>
    public class AggregateCalculator
    {
        private readonly SqlFieldAccessor fieldAccessor;
        private readonly ExpressionEvaluator expressionEvaluator;

        public AggregateCalculator(SqlFieldAccessor fieldAccessor, ExpressionEvaluator expressionEvaluator)
        {
            this.fieldAccessor = fieldAccessor;
            this.expressionEvaluator = expressionEvaluator;
        }

        /// <summary>
        /// 集計関数があるかチェックする。
        /// </summary>
        public bool HasAggregateFunction(List<SelectColumn> columns)
        {
            return columns.Any(c => c.Expression is AggregateExpression);
        }

        /// <summary>
        /// 集計関数を評価する。
        /// </summary>
        public object EvaluateAggregate(AggregateExpression aggExpr, List<object> records, Type recordType)
        {
            if (records.Count == 0)
                return aggExpr.Function == AggregateFunction.Count ? 0 : null;

            if (aggExpr.Argument == null)
                return records.Count;

            var values = new List<object>();

            foreach (var record in records)
            {
                var value = expressionEvaluator.ResolveValue(aggExpr.Argument, record, recordType);
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
        public object EvaluateJoinedAggregate(
            AggregateExpression aggExpr,
            List<JoinedRecord> records,
            Func<SqlExpression, JoinedRecord, object> valueResolver)
        {
            if (records.Count == 0)
                return aggExpr.Function == AggregateFunction.Count ? 0 : null;

            if (aggExpr.Argument == null)
                return records.Count;

            var values = new List<object>();

            foreach (var record in records)
            {
                var value = valueResolver(aggExpr.Argument, record);
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
        /// SUM関数を計算する。
        /// </summary>
        public object CalculateSum(List<object> values)
        {
            if (values.Count == 0)
                return null;

            double sum = 0;
            foreach (var value in values)
            {
                if (fieldAccessor.IsNumeric(value))
                    sum += Convert.ToDouble(value);
            }
            return sum;
        }

        /// <summary>
        /// AVG関数を計算する。
        /// </summary>
        public object CalculateAvg(List<object> values)
        {
            if (values.Count == 0)
                return null;

            var numericValues = values.Where(v => fieldAccessor.IsNumeric(v)).ToList();
            if (numericValues.Count == 0)
                return null;

            return numericValues.Average(v => Convert.ToDouble(v));
        }

        /// <summary>
        /// MIN関数を計算する。
        /// </summary>
        public object CalculateMin(List<object> values)
        {
            if (values.Count == 0)
                return null;

            object min = null;
            foreach (var value in values)
            {
                if (min == null || fieldAccessor.Compare(value, min) < 0)
                    min = value;
            }
            return min;
        }

        /// <summary>
        /// MAX関数を計算する。
        /// </summary>
        public object CalculateMax(List<object> values)
        {
            if (values.Count == 0)
                return null;

            object max = null;
            foreach (var value in values)
            {
                if (max == null || fieldAccessor.Compare(value, max) > 0)
                    max = value;
            }
            return max;
        }
    }
}
