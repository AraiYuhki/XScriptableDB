using System;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// バリデーション属性の基底クラス。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public abstract class ValidationAttribute : Attribute
    {
        /// <summary>エラーメッセージ</summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 値を検証する。
        /// </summary>
        /// <param name="value">検証する値</param>
        /// <param name="fieldName">フィールド名</param>
        /// <returns>検証結果</returns>
        public abstract ValidationResult Validate(object value, string fieldName);

        /// <summary>
        /// デフォルトのエラーメッセージを取得する。
        /// </summary>
        protected abstract string GetDefaultErrorMessage(string fieldName);

        /// <summary>
        /// エラーメッセージを取得する。
        /// </summary>
        protected string GetErrorMessage(string fieldName)
        {
            return string.IsNullOrEmpty(ErrorMessage)
                ? GetDefaultErrorMessage(fieldName)
                : ErrorMessage;
        }
    }

    /// <summary>
    /// 必須フィールドを指定する属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class RequiredAttribute : ValidationAttribute
    {
        /// <summary>空文字列を許可するか（デフォルト: false）</summary>
        public bool AllowEmptyString { get; set; } = false;

        public override ValidationResult Validate(object value, string fieldName)
        {
            if (value == null)
            {
                return ValidationResult.Error(fieldName, GetErrorMessage(fieldName));
            }

            if (!AllowEmptyString && value is string str && string.IsNullOrEmpty(str))
            {
                return ValidationResult.Error(fieldName, GetErrorMessage(fieldName));
            }

            return ValidationResult.Success;
        }

        protected override string GetDefaultErrorMessage(string fieldName)
        {
            return $"{fieldName} は必須です。";
        }
    }

    /// <summary>
    /// 数値の範囲を指定する属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class RangeAttribute : ValidationAttribute
    {
        /// <summary>最小値</summary>
        public double Minimum { get; }

        /// <summary>最大値</summary>
        public double Maximum { get; }

        public RangeAttribute(double minimum, double maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public RangeAttribute(int minimum, int maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public override ValidationResult Validate(object value, string fieldName)
        {
            if (value == null)
            {
                return ValidationResult.Success; // nullは別のバリデーションで処理
            }

            double numValue;
            try
            {
                numValue = Convert.ToDouble(value);
            }
            catch
            {
                return ValidationResult.Error(fieldName, $"{fieldName} は数値である必要があります。");
            }

            if (numValue < Minimum || numValue > Maximum)
            {
                return ValidationResult.Error(fieldName, GetErrorMessage(fieldName));
            }

            return ValidationResult.Success;
        }

        protected override string GetDefaultErrorMessage(string fieldName)
        {
            return $"{fieldName} は {Minimum} から {Maximum} の範囲である必要があります。";
        }
    }

    /// <summary>
    /// 文字列の長さを指定する属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class StringLengthAttribute : ValidationAttribute
    {
        /// <summary>最小長</summary>
        public int MinimumLength { get; set; } = 0;

        /// <summary>最大長</summary>
        public int MaximumLength { get; }

        public StringLengthAttribute(int maximumLength)
        {
            MaximumLength = maximumLength;
        }

        public override ValidationResult Validate(object value, string fieldName)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            var str = value.ToString();
            var length = str.Length;

            if (length < MinimumLength || length > MaximumLength)
            {
                return ValidationResult.Error(fieldName, GetErrorMessage(fieldName));
            }

            return ValidationResult.Success;
        }

        protected override string GetDefaultErrorMessage(string fieldName)
        {
            if (MinimumLength > 0)
            {
                return $"{fieldName} は {MinimumLength} 文字以上 {MaximumLength} 文字以下である必要があります。";
            }
            return $"{fieldName} は {MaximumLength} 文字以下である必要があります。";
        }
    }

    /// <summary>
    /// 正規表現パターンを指定する属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class RegularExpressionAttribute : ValidationAttribute
    {
        /// <summary>正規表現パターン</summary>
        public string Pattern { get; }

        public RegularExpressionAttribute(string pattern)
        {
            Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        }

        public override ValidationResult Validate(object value, string fieldName)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            var str = value.ToString();
            if (!System.Text.RegularExpressions.Regex.IsMatch(str, Pattern))
            {
                return ValidationResult.Error(fieldName, GetErrorMessage(fieldName));
            }

            return ValidationResult.Success;
        }

        protected override string GetDefaultErrorMessage(string fieldName)
        {
            return $"{fieldName} の形式が正しくありません。";
        }
    }

    /// <summary>
    /// 一意性制約を指定する属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class UniqueAttribute : ValidationAttribute
    {
        public override ValidationResult Validate(object value, string fieldName)
        {
            // 一意性の検証はレコード単体では行えないため、
            // RecordValidatorで全レコードを対象に検証する
            return ValidationResult.Success;
        }

        protected override string GetDefaultErrorMessage(string fieldName)
        {
            return $"{fieldName} の値が重複しています。";
        }
    }

    /// <summary>
    /// 外部キー制約を指定する属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ForeignKeyAttribute : ValidationAttribute
    {
        /// <summary>参照先テーブルの型</summary>
        public Type ReferenceTableType { get; }

        /// <summary>参照先のキーフィールド名（省略時はPrimaryKey）</summary>
        public string ReferenceKeyField { get; set; }

        public ForeignKeyAttribute(Type referenceTableType)
        {
            ReferenceTableType = referenceTableType ?? throw new ArgumentNullException(nameof(referenceTableType));
        }

        public override ValidationResult Validate(object value, string fieldName)
        {
            // 外部キーの検証は参照先テーブルが必要なため、
            // RecordValidatorで検証する
            return ValidationResult.Success;
        }

        protected override string GetDefaultErrorMessage(string fieldName)
        {
            return $"{fieldName} の参照先が存在しません。";
        }
    }

    /// <summary>
    /// 比較バリデーションの種類。
    /// </summary>
    public enum CompareOperator
    {
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual
    }

    /// <summary>
    /// 他のフィールドとの比較を指定する属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class CompareAttribute : ValidationAttribute
    {
        /// <summary>比較対象のフィールド名</summary>
        public string OtherField { get; }

        /// <summary>比較演算子</summary>
        public CompareOperator Operator { get; set; } = CompareOperator.Equal;

        public CompareAttribute(string otherField)
        {
            OtherField = otherField ?? throw new ArgumentNullException(nameof(otherField));
        }

        public override ValidationResult Validate(object value, string fieldName)
        {
            // フィールド間の比較はレコード単体では行えないため、
            // RecordValidatorで検証する
            return ValidationResult.Success;
        }

        protected override string GetDefaultErrorMessage(string fieldName)
        {
            var opStr = Operator switch
            {
                CompareOperator.Equal => "等しい",
                CompareOperator.NotEqual => "異なる",
                CompareOperator.LessThan => "より小さい",
                CompareOperator.LessThanOrEqual => "以下",
                CompareOperator.GreaterThan => "より大きい",
                CompareOperator.GreaterThanOrEqual => "以上",
                _ => "?"
            };
            return $"{fieldName} は {OtherField} と{opStr}必要があります。";
        }
    }
}
