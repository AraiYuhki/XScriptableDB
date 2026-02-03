using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SQL式を評価するクラス。
    /// </summary>
    public class ExpressionEvaluator
    {
        private readonly SqlFieldAccessor fieldAccessor;
        private Func<SelectStatement, List<object>> subqueryExecutor;

        public ExpressionEvaluator(SqlFieldAccessor fieldAccessor)
        {
            this.fieldAccessor = fieldAccessor;
        }

        /// <summary>
        /// サブクエリ実行用のデリゲートを設定する。
        /// </summary>
        public void SetSubqueryExecutor(Func<SelectStatement, List<object>> executor)
        {
            subqueryExecutor = executor;
        }

        /// <summary>
        /// 式を評価してブール値を返す。
        /// </summary>
        public bool EvaluateExpression(SqlExpression expr, object record, Type recordType)
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
                    return literal.Value != null && !Equals(literal.Value, false) && !Equals(literal.Value, 0);

                case ColumnExpression column:
                    var value = fieldAccessor.GetFieldValue(record, recordType, column.ColumnName);
                    return value != null && !Equals(value, false) && !Equals(value, 0);

                default:
                    return false;
            }
        }

        /// <summary>
        /// 比較式を評価する。
        /// </summary>
        public bool EvaluateComparison(ComparisonExpression expr, object record, Type recordType)
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
        /// 式から値を取得する。
        /// </summary>
        public object ResolveValue(SqlExpression expr, object record, Type recordType)
        {
            switch (expr)
            {
                case LiteralExpression literal:
                    return literal.Value;

                case ColumnExpression column:
                    return fieldAccessor.GetFieldValue(record, recordType, column.ColumnName);

                case ArithmeticExpression arith:
                    return EvaluateArithmetic(arith, record, recordType);

                case CaseExpression caseExpr:
                    return EvaluateCase(caseExpr, record, recordType);

                case FunctionCallExpression funcExpr:
                    return EvaluateFunction(funcExpr, record, recordType);

                case AggregateExpression:
                    return null;

                default:
                    return null;
            }
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

        private bool EvaluateInSubquery(object leftValue, SubqueryExpression subqueryExpr)
        {
            if (subqueryExecutor == null)
                return false;

            var subqueryValues = subqueryExecutor(subqueryExpr.Subquery);

            foreach (var subValue in subqueryValues)
            {
                if (fieldAccessor.AreEqual(leftValue, subValue))
                    return true;
            }

            return false;
        }

        private bool EvaluateInList(object leftValue, InListExpression inList, object record, Type recordType)
        {
            foreach (var item in inList.Values)
            {
                var itemValue = ResolveValue(item, record, recordType);
                if (fieldAccessor.AreEqual(leftValue, itemValue))
                    return true;
            }

            return false;
        }

        private bool EvaluateStandardComparison(ComparisonOperator op, object leftValue, SqlExpression rightExpr, object record, Type recordType)
        {
            var rightValue = ResolveRightValue(rightExpr, record, recordType);

            return op switch
            {
                ComparisonOperator.Equal => fieldAccessor.AreEqual(leftValue, rightValue),
                ComparisonOperator.NotEqual => !fieldAccessor.AreEqual(leftValue, rightValue),
                ComparisonOperator.LessThan => fieldAccessor.Compare(leftValue, rightValue) < 0,
                ComparisonOperator.LessOrEqual => fieldAccessor.Compare(leftValue, rightValue) <= 0,
                ComparisonOperator.GreaterThan => fieldAccessor.Compare(leftValue, rightValue) > 0,
                ComparisonOperator.GreaterOrEqual => fieldAccessor.Compare(leftValue, rightValue) >= 0,
                ComparisonOperator.Like => EvaluateLike(leftValue?.ToString(), rightValue?.ToString()),
                _ => false
            };
        }

        private object ResolveRightValue(SqlExpression rightExpr, object record, Type recordType)
        {
            if (rightExpr is SubqueryExpression subquery && subqueryExecutor != null)
            {
                var subqueryValues = subqueryExecutor(subquery.Subquery);
                return subqueryValues.FirstOrDefault();
            }

            return ResolveValue(rightExpr, record, recordType);
        }

        /// <summary>
        /// 算術式を評価する。
        /// </summary>
        public object EvaluateArithmetic(ArithmeticExpression expr, object record, Type recordType)
        {
            var left = ResolveValue(expr.Left, record, recordType);
            var right = ResolveValue(expr.Right, record, recordType);

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

        /// <summary>
        /// CASE式を評価する。
        /// </summary>
        public object EvaluateCase(CaseExpression expr, object record, Type recordType)
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
        public object EvaluateFunction(FunctionCallExpression expr, object record, Type recordType)
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
        /// LIKE演算子を評価する。
        /// </summary>
        public bool EvaluateLike(string value, string pattern)
        {
            if (value == null || pattern == null) return false;

            var regexPattern = "^" +
                               Regex.Escape(pattern)
                                   .Replace("%", ".*")
                                   .Replace("_", ".")
                               + "$";

            return Regex.IsMatch(value, regexPattern, RegexOptions.IgnoreCase);
        }
    }
}
